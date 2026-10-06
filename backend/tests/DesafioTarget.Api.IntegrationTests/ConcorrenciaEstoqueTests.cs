using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DesafioTarget.Api.Data.Entities;
using DesafioTarget.Api.IntegrationTests.Infrastructure;
using DesafioTarget.Api.Models.Estoque;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DesafioTarget.Api.IntegrationTests;

[Trait("Category", "SqlServerIntegration")]
public sealed class ConcorrenciaEstoqueTests
{
    [SqlServerTheory]
    [InlineData("saida", 7, 3)]
    [InlineData("entrada", 1, 11)]
    public async Task Registrar_EmDeadlock_DeveRetornarConflitoSemPerderSaldoOuDuplicarHistorico(
        string tipo,
        int quantidade,
        int saldoEsperado)
    {
        await using var banco = await BancoSqlServerTeste.CriarAsync();
        var sincronizacao = new SincronizarGravacoesInterceptor();
        using var factory = new SqlServerApiFactory(banco, sincronizacao);
        using var cliente = factory.CreateClient();
        using var prazo = new CancellationTokenSource(TimeSpan.FromSeconds(45));

        var respostas = await Task.WhenAll(
            EnviarMovimentacaoAsync(cliente, tipo, quantidade, "Primeira movimentação", prazo.Token),
            EnviarMovimentacaoAsync(cliente, tipo, quantidade, "Segunda movimentação", prazo.Token));

        try
        {
            Assert.Equal(2, sincronizacao.ContextosSincronizados);
            Assert.Equal(1, sincronizacao.DeadlocksDetectados);
            var confirmada = Assert.Single(respostas.Where(resposta => resposta.StatusCode == HttpStatusCode.Created));
            var rejeitada = Assert.Single(respostas.Where(resposta => resposta.StatusCode == HttpStatusCode.Conflict));
            await VerificarConflitoAsync(rejeitada, prazo.Token);

            using var conteudoConfirmado = JsonDocument.Parse(await confirmada.Content.ReadAsStringAsync(prazo.Token));
            var movimentacaoConfirmada = conteudoConfirmado.RootElement;
            var idConfirmado = movimentacaoConfirmada.GetProperty("id").GetInt64();
            Assert.Equal($"/api/movimentacoes/{idConfirmado}", confirmada.Headers.Location?.OriginalString);
            Assert.Equal(10, movimentacaoConfirmada.GetProperty("estoqueAnterior").GetInt32());
            Assert.Equal(saldoEsperado, movimentacaoConfirmada.GetProperty("estoqueFinal").GetInt32());

            await using var contexto = banco.CriarContexto();
            var produto = await contexto.Produtos.AsNoTracking().SingleAsync(item => item.CodigoProduto == 101, prazo.Token);
            var historico = await contexto.Movimentacoes.AsNoTracking().ToListAsync(prazo.Token);
            var movimentacao = Assert.Single(historico);
            Assert.Equal(saldoEsperado, produto.Estoque);
            Assert.Equal(idConfirmado, movimentacao.Id);
            Assert.Equal(101, movimentacao.CodigoProduto);
            Assert.Equal(quantidade, movimentacao.Quantidade);
            Assert.Equal(tipo == "entrada" ? TipoMovimentacao.Entrada : TipoMovimentacao.Saida, movimentacao.Tipo);
            Assert.Equal(movimentacaoConfirmada.GetProperty("descricao").GetString(), movimentacao.Descricao);
            Assert.Equal(10, movimentacao.EstoqueAnterior);
            Assert.Equal(saldoEsperado, movimentacao.EstoqueFinal);
        }
        finally
        {
            foreach (var resposta in respostas)
                resposta.Dispose();
        }
    }

    [SqlServerFact]
    public async Task Registrar_SaidasConcorrentes_DevePreservarSaldoEHistoricoDasOperacoesConfirmadas()
    {
        await using var banco = await BancoSqlServerTeste.CriarAsync();
        using var factory = new SqlServerApiFactory(banco);
        using var cliente = factory.CreateClient();
        using var prazo = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        var inicio = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task<HttpResponseMessage> EnviarSaidaAsync(int numero)
        {
            await inicio.Task.WaitAsync(prazo.Token);
            return await EnviarMovimentacaoAsync(cliente, "saida", 3, $"Saída concorrente {numero}", prazo.Token);
        }

        var tarefas = Enumerable.Range(1, 8).Select(EnviarSaidaAsync).ToArray();
        inicio.SetResult();
        var respostas = await Task.WhenAll(tarefas);

        try
        {
            Assert.All(respostas, resposta => Assert.Contains(
                resposta.StatusCode, new[] { HttpStatusCode.Created, HttpStatusCode.Conflict }));
            var confirmadas = respostas.Where(resposta => resposta.StatusCode == HttpStatusCode.Created).ToArray();
            Assert.InRange(confirmadas.Length, 1, 3);
            foreach (var rejeitada in respostas.Where(resposta => resposta.StatusCode == HttpStatusCode.Conflict))
                await VerificarConflitoAsync(rejeitada, prazo.Token);

            var idsConfirmados = new List<long>();
            var descricoesConfirmadas = new List<string>();
            foreach (var resposta in confirmadas)
            {
                using var conteudo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync(prazo.Token));
                idsConfirmados.Add(conteudo.RootElement.GetProperty("id").GetInt64());
                descricoesConfirmadas.Add(conteudo.RootElement.GetProperty("descricao").GetString()!);
            }

            await using var contexto = banco.CriarContexto();
            var produto = await contexto.Produtos.AsNoTracking().SingleAsync(item => item.CodigoProduto == 101, prazo.Token);
            var historico = await contexto.Movimentacoes.AsNoTracking()
                .OrderByDescending(item => item.EstoqueAnterior).ToListAsync(prazo.Token);
            Assert.Equal(10 - 3 * confirmadas.Length, produto.Estoque);
            Assert.InRange(produto.Estoque, 0, 10);
            Assert.Equal(confirmadas.Length, historico.Count);
            Assert.Equal(idsConfirmados.Order(), historico.Select(item => item.Id).Order());
            Assert.Equal(descricoesConfirmadas.Order(), historico.Select(item => item.Descricao).Order());

            var saldo = 10;
            foreach (var movimentacao in historico)
            {
                Assert.Equal(101, movimentacao.CodigoProduto);
                Assert.Equal(TipoMovimentacao.Saida, movimentacao.Tipo);
                Assert.Equal(3, movimentacao.Quantidade);
                Assert.Equal(saldo, movimentacao.EstoqueAnterior);
                saldo -= movimentacao.Quantidade;
                Assert.Equal(saldo, movimentacao.EstoqueFinal);
                Assert.InRange(movimentacao.EstoqueFinal, 0, 10);
            }

            Assert.Equal(produto.Estoque, saldo);
        }
        finally
        {
            foreach (var resposta in respostas)
                resposta.Dispose();
        }
    }

    private static Task<HttpResponseMessage> EnviarMovimentacaoAsync(
        HttpClient cliente,
        string tipo,
        int quantidade,
        string descricao,
        CancellationToken cancellationToken) =>
        cliente.PostAsJsonAsync("/api/movimentacoes", new
        {
            codigoProduto = 101,
            tipo,
            quantidade,
            descricao
        }, cancellationToken);

    private static async Task VerificarConflitoAsync(HttpResponseMessage resposta, CancellationToken cancellationToken)
    {
        Assert.Equal("application/problem+json", resposta.Content.Headers.ContentType?.MediaType);
        var conteudo = await resposta.Content.ReadAsStringAsync(cancellationToken);
        using var problema = JsonDocument.Parse(conteudo);
        Assert.Equal(409, problema.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Conflito na operação.", problema.RootElement.GetProperty("title").GetString());
        Assert.Equal("/api/movimentacoes", problema.RootElement.GetProperty("instance").GetString());
        var detalhe = problema.RootElement.GetProperty("detail").GetString();
        Assert.False(string.IsNullOrWhiteSpace(detalhe));
        Assert.False(string.IsNullOrWhiteSpace(problema.RootElement.GetProperty("traceId").GetString()));
        Assert.False(problema.RootElement.TryGetProperty("stackTrace", out _));
        Assert.False(problema.RootElement.TryGetProperty("exception", out _));
        foreach (var detalheInterno in new[] { "SqlException", "SqlClient", "deadlock", "1205", "UPDATE", "SELECT", "stackTrace" })
            Assert.DoesNotContain(detalheInterno, detalhe!, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class SincronizarGravacoesInterceptor : SaveChangesInterceptor
    {
        private readonly TaskCompletionSource _leiturasConcluidas = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _participantes;
        private int _deadlocks;

        public int ContextosSincronizados => Volatile.Read(ref _participantes);
        public int DeadlocksDetectados => Volatile.Read(ref _deadlocks);

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context?.ChangeTracker.Entries<MovimentacaoEstoqueEntity>()
                    .Any(item => item.State == EntityState.Added) != true)
                return result;

            // As duas transações mantêm os locks de leitura até tentarem gravar o mesmo produto.
            if (Interlocked.Increment(ref _participantes) == 2)
                _leiturasConcluidas.TrySetResult();

            await _leiturasConcluidas.Task.WaitAsync(TimeSpan.FromSeconds(15), cancellationToken);
            return result;
        }

        public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
        {
            for (Exception? erro = eventData.Exception; erro is not null; erro = erro.InnerException)
            {
                if (erro is SqlException { Number: 1205 })
                    Interlocked.Increment(ref _deadlocks);
            }

            return Task.CompletedTask;
        }
    }
}
