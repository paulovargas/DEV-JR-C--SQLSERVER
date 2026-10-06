using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using DesafioTarget.Api.Models;

namespace DesafioTarget.Api.Tests;

public sealed class MovimentacoesEndpointTests
{
    [Theory]
    [InlineData("")]
    [InlineData(",\"tipo\":null")]
    [InlineData(",\"tipo\":\"desconhecido\"")]
    [InlineData(",\"tipo\":\"\"")]
    [InlineData(",\"tipo\":0")]
    [InlineData(",\"tipo\":1")]
    [InlineData(",\"tipo\":99")]
    public async Task Registrar_DeveRejeitarTipoInvalidoSemAlterarSaldoOuHistorico(string campoTipo)
    {
        using var factory = new ApiFactory();
        using var cliente = factory.CreateClient();
        var produtoAnterior = await cliente.GetFromJsonAsync<ProdutoEstoque>("/api/produtos/101");
        var historicoAnterior = await cliente.GetFromJsonAsync<MovimentacaoEstoque[]>("/api/movimentacoes");
        Assert.NotNull(produtoAnterior);
        Assert.NotNull(historicoAnterior);
        var json = "{\"codigoProduto\":101,\"quantidade\":5,\"descricao\":\"Teste\"" + campoTipo + "}";
        using var conteudo = new StringContent(json, Encoding.UTF8, "application/json");

        using var resposta = await cliente.PostAsync("/api/movimentacoes", conteudo);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        if (campoTipo is "" or ",\"tipo\":null")
        {
            using var problema = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            Assert.Equal("O tipo da movimentação é obrigatório.",
                problema.RootElement.GetProperty("errors").GetProperty("tipo")[0].GetString());
        }

        var produtoAtual = await cliente.GetFromJsonAsync<ProdutoEstoque>("/api/produtos/101");
        var historicoAtual = await cliente.GetFromJsonAsync<MovimentacaoEstoque[]>("/api/movimentacoes");
        Assert.Equal(produtoAnterior, produtoAtual);
        Assert.Equal(historicoAnterior, historicoAtual);
    }

    [Theory]
    [InlineData("entrada", TipoMovimentacao.Entrada, 155)]
    [InlineData("saida", TipoMovimentacao.Saida, 145)]
    public async Task Registrar_DevePersistirTiposValidosEAtualizarSaldo(string tipo, TipoMovimentacao tipoEsperado, int saldoEsperado)
    {
        using var factory = new ApiFactory();
        using var cliente = factory.CreateClient();
        using var resposta = await cliente.PostAsJsonAsync("/api/movimentacoes", new
        {
            codigoProduto = 101,
            tipo,
            quantidade = 5,
            descricao = "Teste"
        });

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        Assert.NotNull(resposta.Headers.Location);
        var opcoes = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        opcoes.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        var movimentacao = await resposta.Content.ReadFromJsonAsync<MovimentacaoEstoque>(opcoes);
        Assert.NotNull(movimentacao);
        Assert.Equal(tipoEsperado, movimentacao.Tipo);
        Assert.Equal(150, movimentacao.EstoqueAnterior);
        Assert.Equal(saldoEsperado, movimentacao.EstoqueFinal);
        var produto = await cliente.GetFromJsonAsync<ProdutoEstoque>("/api/produtos/101");
        Assert.NotNull(produto);
        Assert.Equal(saldoEsperado, produto.Estoque);
        var historico = await cliente.GetFromJsonAsync<MovimentacaoEstoque[]>("/api/movimentacoes", opcoes);
        Assert.NotNull(historico);
        Assert.Equal(movimentacao, Assert.Single(historico));
    }

    [Theory]
    [InlineData(0, "Teste", "quantidade")]
    [InlineData(-1, "Teste", "quantidade")]
    [InlineData(1, null, "descricao")]
    [InlineData(1, "", "descricao")]
    [InlineData(1, "   ", "descricao")]
    public async Task Registrar_DeveTraduzirErrosDoServicoParaValidacaoHttp(int quantidade, string? descricao, string campo)
    {
        using var factory = new ApiFactory();
        using var cliente = factory.CreateClient();
        using var resposta = await cliente.PostAsJsonAsync("/api/movimentacoes", new
        {
            codigoProduto = 101,
            tipo = "entrada",
            quantidade,
            descricao
        });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Equal("application/problem+json", resposta.Content.Headers.ContentType?.MediaType);
        using var problema = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        Assert.True(problema.RootElement.GetProperty("errors").TryGetProperty(campo, out _));
        var produto = await cliente.GetFromJsonAsync<ProdutoEstoque>("/api/produtos/101");
        Assert.NotNull(produto);
        Assert.Equal(150, produto.Estoque);
        Assert.Empty((await cliente.GetFromJsonAsync<MovimentacaoEstoque[]>("/api/movimentacoes"))!);
    }
}
