using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using DesafioTarget.Api.Models;

namespace DesafioTarget.Api.Tests;

public sealed class ComissoesEndpointTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _cliente;

    public ComissoesEndpointTests(ApiFactory factory)
    {
        _cliente = factory.CreateClient();
    }

    [Theory]
    [InlineData("{}", "vendas", "A lista de vendas é obrigatória.")]
    [InlineData("{\"vendas\":null}", "vendas", "A lista de vendas é obrigatória.")]
    [InlineData("{\"vendas\":[]}", "vendas", "A lista de vendas deve conter pelo menos uma venda.")]
    [InlineData("{\"vendas\":[null]}", "vendas[0]", "A venda não pode ser nula.")]
    [InlineData("{\"vendas\":[{\"valor\":100}]}", "vendas[0].vendedor", "O vendedor é obrigatório.")]
    [InlineData("{\"vendas\":[{\"vendedor\":\"\",\"valor\":100}]}", "vendas[0].vendedor", "O vendedor é obrigatório.")]
    [InlineData("{\"vendas\":[{\"vendedor\":\"   \",\"valor\":100}]}", "vendas[0].vendedor", "O vendedor é obrigatório.")]
    [InlineData("{\"vendas\":[{\"vendedor\":\"Maria\",\"valor\":0}]}", "vendas[0].valor", "O valor da venda deve ser maior que zero.")]
    [InlineData("{\"vendas\":[{\"vendedor\":\"Maria\",\"valor\":-1}]}", "vendas[0].valor", "O valor da venda deve ser maior que zero.")]
    public async Task Calcular_DeveRetornarValidacaoParaVendasInvalidas(string json, string campo, string mensagem)
    {
        using var conteudo = new StringContent(json, Encoding.UTF8, "application/json");
        using var resposta = await _cliente.PostAsync("/api/comissoes/calcular", conteudo);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Equal("application/problem+json", resposta.Content.Headers.ContentType?.MediaType);
        using var problema = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        Assert.Equal(400, problema.RootElement.GetProperty("status").GetInt32());
        Assert.Equal(mensagem, problema.RootElement.GetProperty("errors").GetProperty(campo)[0].GetString());
    }

    [Fact]
    public async Task Calcular_DevePreservarOsIndicesEAcumularErrosAposUmaVendaNula()
    {
        const string json = """
            {"vendas":[{"vendedor":"Maria","valor":100},null,{"vendedor":" ","valor":0}]}
            """;
        using var conteudo = new StringContent(json, Encoding.UTF8, "application/json");
        using var resposta = await _cliente.PostAsync("/api/comissoes/calcular", conteudo);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        using var problema = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        var erros = problema.RootElement.GetProperty("errors");
        Assert.Equal(3, erros.EnumerateObject().Count());
        Assert.True(erros.TryGetProperty("vendas[1]", out _));
        Assert.True(erros.TryGetProperty("vendas[2].vendedor", out _));
        Assert.True(erros.TryGetProperty("vendas[2].valor", out _));
    }

    [Fact]
    public async Task Calcular_DeveRetornarOsTotaisDoJsonDoDesafio()
    {
        var caminho = Path.Combine(AppContext.BaseDirectory, "Data", "vendas.json");
        using var conteudo = new StringContent(await File.ReadAllTextAsync(caminho), Encoding.UTF8, "application/json");
        using var resposta = await _cliente.PostAsync("/api/comissoes/calcular", conteudo);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var resultado = await resposta.Content.ReadFromJsonAsync<CalculoComissaoResponse>();
        Assert.NotNull(resultado);
        Assert.Equal(new[] { 495.68m, 465.95m, 379.37m, 404.98m }, resultado.Vendedores.Select(vendedor => vendedor.ComissaoTotal));
        Assert.Equal(1745.98m, resultado.ComissaoTotalGeral);
    }
}
