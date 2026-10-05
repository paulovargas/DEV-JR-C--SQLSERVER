using DesafioTarget.Api.Services;

namespace DesafioTarget.Api.Tests;

public sealed class CalculadoraJurosTests
{
    private readonly CalculadoraJuros _calculadora = new();

    [Fact]
    public void Calcular_DeveAplicarJurosSimplesPorDiaDeAtraso()
    {
        var resultado = _calculadora.Calcular(
            100m,
            new DateOnly(2026, 10, 1),
            new DateOnly(2026, 10, 5));

        Assert.Equal(4, resultado.DiasAtraso);
        Assert.Equal(10m, resultado.ValorJuros);
        Assert.Equal(110m, resultado.ValorAtualizado);
    }

    [Theory]
    [InlineData(2026, 10, 5)]
    [InlineData(2026, 10, 6)]
    public void Calcular_NaoDeveCobrarJurosSemAtraso(int ano, int mes, int dia)
    {
        var resultado = _calculadora.Calcular(
            100m,
            new DateOnly(ano, mes, dia),
            new DateOnly(2026, 10, 5));

        Assert.Equal(0, resultado.DiasAtraso);
        Assert.Equal(0m, resultado.ValorJuros);
        Assert.Equal(100m, resultado.ValorAtualizado);
    }
}
