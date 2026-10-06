using DesafioTarget.Api.Services;

namespace DesafioTarget.Api.Tests;

public sealed class CalculadoraJurosLimitesTests
{
    private readonly CalculadoraJuros _calculadora = new();
    private readonly DateOnly _vencimento = new(2026, 10, 1);

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
        var erro = Assert.Throws<CalculoInvalidoException>(() =>
            _calculadora.Calcular(valor, _vencimento, _vencimento));

        Assert.Contains("valor", erro.Erros.Keys);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Calcular_DeveAceitarValorMaximoSemAtraso(int dias)
    {
        var resultado = _calculadora.Calcular(
            LimitesMonetarios.ValorMaximo,
            _vencimento,
            _vencimento.AddDays(dias));

        Assert.Equal(0, resultado.DiasAtraso);
        Assert.Equal(0m, resultado.ValorJuros);
        Assert.Equal(LimitesMonetarios.ValorMaximo, resultado.ValorOriginal);
        Assert.Equal(LimitesMonetarios.ValorMaximo, resultado.ValorAtualizado);
    }

    [Fact]
    public void Calcular_DeveAceitarValorAtualizadoExatamenteNoLimite()
    {
        var principal = LimitesMonetarios.ValorMaximo / 2m;

        var resultado = _calculadora.Calcular(principal, _vencimento, _vencimento.AddDays(40));

        Assert.Equal(40, resultado.DiasAtraso);
        Assert.Equal(principal, resultado.ValorJuros);
        Assert.Equal(LimitesMonetarios.ValorMaximo, resultado.ValorAtualizado);
    }

    [Fact]
    public void Calcular_DeveRejeitarValorAtualizadoAcimaDoLimite()
    {
        var principal = LimitesMonetarios.ValorMaximo / 2m + 0.01m;

        var erro = Assert.Throws<CalculoInvalidoException>(() =>
            _calculadora.Calcular(principal, _vencimento, _vencimento.AddDays(40)));

        Assert.Contains("valor", erro.Erros.Keys);
    }

    [Fact]
    public void Calcular_DeveRejeitarUmDiaDeJurosSobreValorMaximo()
    {
        var erro = Assert.Throws<CalculoInvalidoException>(() =>
            _calculadora.Calcular(LimitesMonetarios.ValorMaximo, _vencimento, _vencimento.AddDays(1)));

        Assert.Contains("valor", erro.Erros.Keys);
    }

    [Fact]
    public void Calcular_DeveTraduzirOverflowFisicoDeJurosEmErroDeCalculo()
    {
        var erro = Assert.Throws<CalculoInvalidoException>(() => _calculadora.Calcular(
            LimitesMonetarios.ValorMaximo,
            DateOnly.MinValue.AddDays(1),
            DateOnly.MaxValue));

        Assert.Contains("valor", erro.Erros.Keys);
    }

    [Fact]
    public void Calcular_DeveAceitarAtrasoMaximoQuandoOTotalCabeNoLimite()
    {
        var resultado = _calculadora.Calcular(1m, DateOnly.MinValue.AddDays(1), DateOnly.MaxValue);

        Assert.Equal(3652057, resultado.DiasAtraso);
        Assert.Equal(91301.43m, resultado.ValorJuros);
        Assert.Equal(91302.43m, resultado.ValorAtualizado);
    }

    [Theory]
    [InlineData(true, "dataVencimento")]
    [InlineData(false, "dataCalculo")]
    public void Calcular_DeveRejeitarDatasAusentesEmChamadasDiretas(bool vencimentoAusente, string campo)
    {
        var vencimento = vencimentoAusente ? default : _vencimento;
        var calculo = vencimentoAusente ? _vencimento : default;

        var erro = Assert.Throws<CalculoInvalidoException>(() =>
            _calculadora.Calcular(100m, vencimento, calculo));

        Assert.Contains(campo, erro.Erros.Keys);
    }

    [Fact]
    public void Calcular_DeveAcumularErrosDeValorEDatas()
    {
        var erro = Assert.Throws<CalculoInvalidoException>(() =>
            _calculadora.Calcular(0m, default, default));

        Assert.Equal(3, erro.Erros.Count);
        Assert.Contains("valor", erro.Erros.Keys);
        Assert.Contains("dataVencimento", erro.Erros.Keys);
        Assert.Contains("dataCalculo", erro.Erros.Keys);
    }

    [Theory]
    [InlineData(0.20, 0.01, 0.21)]
    [InlineData(0.60, 0.02, 0.62)]
    public void Calcular_DevePreservarArredondamentoDeMeioCentavo(
        decimal valor,
        decimal jurosEsperados,
        decimal atualizadoEsperado)
    {
        var resultado = _calculadora.Calcular(valor, _vencimento, _vencimento.AddDays(1));

        Assert.Equal(jurosEsperados, resultado.ValorJuros);
        Assert.Equal(atualizadoEsperado, resultado.ValorAtualizado);
    }
}
