using System.Text.Json;
using DesafioTarget.Api.Models.Comissoes;
using DesafioTarget.Api.Services;

namespace DesafioTarget.Api.Tests;

public sealed class CalculadoraComissaoTests
{
    private readonly CalculadoraComissao _calculadora = new();

    [Theory]
    [InlineData(99.99, 0)]
    [InlineData(100, 0.01)]
    [InlineData(499.99, 0.01)]
    [InlineData(500, 0.05)]
    public void ObterTaxa_DeveRespeitarOsLimites(decimal valor, decimal taxaEsperada)
    {
        var taxa = CalculadoraComissao.ObterTaxa(valor);

        Assert.Equal(taxaEsperada, taxa);
    }

    [Fact]
    public void Calcular_DeveRetornarOsTotaisDoJsonDoDesafio()
    {
        var caminho = Path.Combine(AppContext.BaseDirectory, "Data", "vendas.json");
        var json = File.ReadAllText(caminho);
        var request = JsonSerializer.Deserialize<CalculoComissaoRequest>(
            json,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        var resultado = _calculadora.Calcular(request!.Vendas!);

        Assert.Collection(
            resultado.Vendedores,
            resumo => Assert.Equal(495.68m, resumo.ComissaoTotal),
            resumo => Assert.Equal(465.95m, resumo.ComissaoTotal),
            resumo => Assert.Equal(379.37m, resumo.ComissaoTotal),
            resumo => Assert.Equal(404.98m, resumo.ComissaoTotal));
        Assert.Equal(1745.98m, resultado.ComissaoTotalGeral);
    }

    [Fact]
    public void Calcular_DeveAgruparNomesIgnorandoEspacosECaixa()
    {
        var vendas = new[]
        {
            new Venda("  Maria Souza ", 100m),
            new Venda("maria souza", 500m)
        };

        var resultado = _calculadora.Calcular(vendas);

        var resumo = Assert.Single(resultado.Vendedores);
        Assert.Equal("Maria Souza", resumo.Vendedor);
        Assert.Equal(2, resumo.QuantidadeVendas);
        Assert.Equal(26m, resumo.ComissaoTotal);
    }

    [Theory]
    [InlineData(100.50, 1.01)]
    [InlineData(500.10, 25.01)]
    public void Calcular_DeveArredondarComissaoDeMeioCentavoParaCima(decimal valor, decimal comissaoEsperada)
    {
        var resultado = _calculadora.Calcular([new Venda("Maria", valor)]);

        Assert.Equal(comissaoEsperada, Assert.Single(resultado.Vendedores).ComissaoTotal);
        Assert.Equal(comissaoEsperada, resultado.ComissaoTotalGeral);
    }

    [Fact]
    public void Calcular_DeveSomarComissoesDoVendedorAntesDeArredondar()
    {
        var resultado = _calculadora.Calcular([new Venda("Maria", 100.25m), new Venda("Maria", 100.25m)]);

        var resumo = Assert.Single(resultado.Vendedores);
        Assert.Equal(200.50m, resumo.ValorTotalVendas);
        Assert.Equal(2.01m, resumo.ComissaoTotal);
        Assert.Equal(2.01m, resultado.ComissaoTotalGeral);
    }
}
