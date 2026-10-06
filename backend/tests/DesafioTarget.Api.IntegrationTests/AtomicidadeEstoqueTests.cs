using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DesafioTarget.Api.Data;
using DesafioTarget.Api.IntegrationTests.Infrastructure;
using DesafioTarget.Api.Models.Estoque;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DesafioTarget.Api.IntegrationTests;

[Trait("Category", "SqlServerIntegration")]
public sealed class AtomicidadeEstoqueTests
{
    [SqlServerFact]
    public async Task Registrar_DevePersistirSaldoEHistoricoNaMesmaOperacao()
    {
        await using var banco = await BancoSqlServerTeste.CriarAsync();
        using var factory = new SqlServerApiFactory(banco);
        using var cliente = factory.CreateClient();

        using var resposta = await cliente.PostAsJsonAsync("/api/movimentacoes", new
        {
            codigoProduto = 101,
            tipo = "entrada",
            quantidade = 5,
            descricao = "  Reposição  "
        });

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        using var conteudo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        var id = conteudo.RootElement.GetProperty("id").GetInt64();
        Assert.Equal($"/api/movimentacoes/{id}", resposta.Headers.Location?.OriginalString);
        await using var contexto = banco.CriarContexto();
        var produto = await contexto.Produtos.AsNoTracking().SingleAsync(item => item.CodigoProduto == 101);
        var movimentacao = Assert.Single(await contexto.Movimentacoes.AsNoTracking().ToListAsync());
        Assert.Equal(15, produto.Estoque);
        Assert.Equal(id, movimentacao.Id);
        Assert.Equal(101, movimentacao.CodigoProduto);
        Assert.Equal(TipoMovimentacao.Entrada, movimentacao.Tipo);
        Assert.Equal(5, movimentacao.Quantidade);
        Assert.Equal("Reposição", movimentacao.Descricao);
        Assert.Equal(10, movimentacao.EstoqueAnterior);
        Assert.Equal(produto.Estoque, movimentacao.EstoqueFinal);
        using var consulta = await cliente.GetAsync(resposta.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, consulta.StatusCode);
    }

    [SqlServerFact]
    public async Task Registrar_DeveDesfazerGravacoesSeFalharAntesDoCommit()
    {
        await using var banco = await BancoSqlServerTeste.CriarAsync();
        var falha = new FalhaDepoisDeSalvarInterceptor();
        using var factory = new SqlServerApiFactory(banco, falha);
        using var cliente = factory.CreateClient();
        falha.Ativo = true;

        using var resposta = await cliente.PostAsJsonAsync("/api/movimentacoes", new
        {
            codigoProduto = 101,
            tipo = "entrada",
            quantidade = 5,
            descricao = "Reposição"
        });

        Assert.Equal(HttpStatusCode.InternalServerError, resposta.StatusCode);
        Assert.Equal(15, falha.EstoqueAntesDaFalha);
        Assert.Equal(1, falha.MovimentacoesAntesDaFalha);
        Assert.Equal("application/problem+json", resposta.Content.Headers.ContentType?.MediaType);
        using var problema = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        Assert.Equal(500, problema.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Não foi possível concluir a operação. Tente novamente mais tarde.",
            problema.RootElement.GetProperty("detail").GetString());
        Assert.False(string.IsNullOrWhiteSpace(problema.RootElement.GetProperty("traceId").GetString()));

        await using var contexto = banco.CriarContexto();
        Assert.Equal(10, (await contexto.Produtos.AsNoTracking().SingleAsync()).Estoque);
        Assert.Empty(await contexto.Movimentacoes.AsNoTracking().ToListAsync());
    }

    [SqlServerFact]
    public async Task Registrar_DeveRejeitarSaidaSemSaldoSemPersistirAlteracoes()
    {
        await using var banco = await BancoSqlServerTeste.CriarAsync();
        using var factory = new SqlServerApiFactory(banco);
        using var cliente = factory.CreateClient();

        using var resposta = await cliente.PostAsJsonAsync("/api/movimentacoes", new
        {
            codigoProduto = 101,
            tipo = "saida",
            quantidade = 11,
            descricao = "Venda"
        });

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        await using var contexto = banco.CriarContexto();
        Assert.Equal(10, (await contexto.Produtos.AsNoTracking().SingleAsync()).Estoque);
        Assert.Empty(await contexto.Movimentacoes.AsNoTracking().ToListAsync());
    }

    [SqlServerFact]
    public async Task Registrar_FalhaSqlDiferenteDeDeadlock_DeveRetornarErroInternoSemPersistirAlteracoes()
    {
        await using var banco = await BancoSqlServerTeste.CriarAsync();
        await using (var configuracao = banco.CriarContexto())
        {
            await configuracao.Database.ExecuteSqlRawAsync("""
                ALTER TABLE dbo.MovimentacoesEstoque
                ADD CONSTRAINT CK_MovimentacoesEstoque_FalhaTeste CHECK (Quantidade < 0);
                """);
        }

        var captura = new CapturarFalhaSqlInterceptor();
        using var factory = new SqlServerApiFactory(banco, captura);
        using var cliente = factory.CreateClient();
        using var resposta = await cliente.PostAsJsonAsync("/api/movimentacoes", new
        {
            codigoProduto = 101,
            tipo = "entrada",
            quantidade = 5,
            descricao = "Reposição"
        });

        Assert.Equal(HttpStatusCode.InternalServerError, resposta.StatusCode);
        Assert.Equal(547, captura.NumeroErro);
        using var problema = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        Assert.Equal("Não foi possível concluir a operação. Tente novamente mais tarde.",
            problema.RootElement.GetProperty("detail").GetString());
        Assert.False(problema.RootElement.TryGetProperty("exception", out _));
        Assert.False(problema.RootElement.TryGetProperty("stackTrace", out _));
        await using var contexto = banco.CriarContexto();
        Assert.Equal(10, (await contexto.Produtos.AsNoTracking().SingleAsync()).Estoque);
        Assert.Empty(await contexto.Movimentacoes.AsNoTracking().ToListAsync());
    }

    private sealed class CapturarFalhaSqlInterceptor : SaveChangesInterceptor
    {
        public int? NumeroErro { get; private set; }

        public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
        {
            for (Exception? erro = eventData.Exception; erro is not null; erro = erro.InnerException)
            {
                if (erro is SqlException sql)
                {
                    NumeroErro = sql.Number;
                    break;
                }
            }

            return Task.CompletedTask;
        }
    }

    private sealed class FalhaDepoisDeSalvarInterceptor : SaveChangesInterceptor
    {
        public bool Ativo { get; set; }
        public int? EstoqueAntesDaFalha { get; private set; }
        public int? MovimentacoesAntesDaFalha { get; private set; }

        public override async ValueTask<int> SavedChangesAsync(
            SaveChangesCompletedEventData eventData,
            int result,
            CancellationToken cancellationToken = default)
        {
            if (!Ativo)
                return result;

            // Confirma as gravações na transação real antes de provocar a falha anterior ao commit.
            var contexto = (DesafioTargetDbContext)eventData.Context!;
            EstoqueAntesDaFalha = await contexto.Produtos.AsNoTracking()
                .Where(produto => produto.CodigoProduto == 101).Select(produto => produto.Estoque)
                .SingleAsync(cancellationToken);
            MovimentacoesAntesDaFalha = await contexto.Movimentacoes.CountAsync(cancellationToken);
            throw new InvalidOperationException("Falha simulada após as gravações e antes do commit.");
        }
    }
}
