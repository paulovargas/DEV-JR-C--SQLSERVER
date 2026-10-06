using DesafioTarget.Api.Models;
using DesafioTarget.Api.Services;

namespace DesafioTarget.Api.Tests;

public sealed class CalculadoraComissaoLimitesTests
{
    private readonly CalculadoraComissao _calculadora = new();

    public static IEnumerable<object[]> ValoresInvalidos()
    {
        yield return [0m];
        yield return [-1m];
        yield return [LimitesMonetarios.ValorMaximo + 0.01m];
        yield return [decimal.MaxValue];
    }

    [Theory]
    [MemberData(nameof(ValoresInvalidos))]
    public void Calcular_DeveRejeitarValoresInvalidosEmChamadasDiretas(decimal valor)
    {
        var vendas = new[] { new Venda("Maria", valor) };

        var erro = Assert.Throws<CalculoInvalidoException>(() => _calculadora.Calcular(vendas));

        Assert.Contains("vendas[0].valor", erro.Erros.Keys);
    }

    [Fact]
    public void Calcular_DeveAceitarVendaNoLimiteMonetario()
    {
        var vendas = new[] { new Venda("Maria", LimitesMonetarios.ValorMaximo) };

        var resultado = _calculadora.Calcular(vendas);

        var vendedor = Assert.Single(resultado.Vendedores);
        Assert.Equal(LimitesMonetarios.ValorMaximo, vendedor.ValorTotalVendas);
        Assert.Equal(39614081257132168796771975.15m, vendedor.ComissaoTotal);
        Assert.Equal(vendedor.ComissaoTotal, resultado.ComissaoTotalGeral);
    }

    [Fact]
    public void Calcular_DeveAceitarSomaExatamenteNoLimite()
    {
        var vendas = new[]
        {
            new Venda("Maria", LimitesMonetarios.ValorMaximo / 2m),
            new Venda("Maria", LimitesMonetarios.ValorMaximo / 2m)
        };

        var resultado = _calculadora.Calcular(vendas);

        var vendedor = Assert.Single(resultado.Vendedores);
        Assert.Equal(2, vendedor.QuantidadeVendas);
        Assert.Equal(LimitesMonetarios.ValorMaximo, vendedor.ValorTotalVendas);
        Assert.Equal(39614081257132168796771975.15m, vendedor.ComissaoTotal);
    }

    [Theory]
    [InlineData("Maria")]
    [InlineData("Joao")]
    public void Calcular_DeveAplicarLimiteGlobalMesmoEntreVendedoresDiferentes(string segundoVendedor)
    {
        var vendas = new[]
        {
            new Venda("Maria", LimitesMonetarios.ValorMaximo - 0.01m),
            new Venda(segundoVendedor, 0.02m)
        };

        var erro = Assert.Throws<CalculoInvalidoException>(() => _calculadora.Calcular(vendas));

        Assert.Contains("vendas", erro.Erros.Keys);
    }

    [Fact]
    public void Calcular_DeveRejeitarSomaQueExcederiaACapacidadeDeDecimal()
    {
        var vendas = Enumerable.Range(0, 101)
            .Select(indice => new Venda($"Vendedor {indice}", LimitesMonetarios.ValorMaximo));

        var erro = Assert.Throws<CalculoInvalidoException>(() => _calculadora.Calcular(vendas));

        Assert.Contains("vendas", erro.Erros.Keys);
    }

    [Fact]
    public void Calcular_DeveRejeitarListaNulaEmChamadaDireta()
    {
        var erro = Assert.Throws<CalculoInvalidoException>(() => _calculadora.Calcular(null!));

        Assert.Contains("vendas", erro.Erros.Keys);
    }

    [Fact]
    public void Calcular_DeveRejeitarListaVaziaEmChamadaDireta()
    {
        var erro = Assert.Throws<CalculoInvalidoException>(() => _calculadora.Calcular([]));

        Assert.Contains("vendas", erro.Erros.Keys);
    }

    [Fact]
    public void Calcular_DeveRejeitarVendaNulaPreservandoSeuIndice()
    {
        var vendas = new Venda[] { new("Maria", 100m), null! };

        var erro = Assert.Throws<CalculoInvalidoException>(() => _calculadora.Calcular(vendas));

        Assert.Contains("vendas[1]", erro.Erros.Keys);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Calcular_DeveRejeitarVendedorInvalidoEmChamadaDireta(string? vendedor)
    {
        var vendas = new[] { new Venda(vendedor, 100m) };

        var erro = Assert.Throws<CalculoInvalidoException>(() => _calculadora.Calcular(vendas));

        Assert.Contains("vendas[0].vendedor", erro.Erros.Keys);
    }

    [Fact]
    public void Calcular_DeveAcumularErrosEmChamadasDiretas()
    {
        var vendas = new Venda[] { null!, new("   ", 0m) };

        var erro = Assert.Throws<CalculoInvalidoException>(() => _calculadora.Calcular(vendas));

        Assert.Equal(3, erro.Erros.Count);
        Assert.Contains("vendas[0]", erro.Erros.Keys);
        Assert.Contains("vendas[1].vendedor", erro.Erros.Keys);
        Assert.Contains("vendas[1].valor", erro.Erros.Keys);
    }
}
