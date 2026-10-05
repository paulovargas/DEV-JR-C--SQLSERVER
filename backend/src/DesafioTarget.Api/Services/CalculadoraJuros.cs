using DesafioTarget.Api.Models;

namespace DesafioTarget.Api.Services;

public interface ICalculadoraJuros
{
    CalculoJurosResponse Calcular(decimal valor, DateOnly dataVencimento, DateOnly dataCalculo);
}

public sealed class CalculadoraJuros : ICalculadoraJuros
{
    private const decimal TaxaDiaria = 0.025m;

    public CalculoJurosResponse Calcular(
        decimal valor,
        DateOnly dataVencimento,
        DateOnly dataCalculo)
    {
        var diasAtraso = Math.Max(0, dataCalculo.DayNumber - dataVencimento.DayNumber);
        var juros = Arredondar(valor * TaxaDiaria * diasAtraso);

        return new CalculoJurosResponse(
            Arredondar(valor),
            dataVencimento,
            dataCalculo,
            diasAtraso,
            TaxaDiaria * 100m,
            juros,
            Arredondar(valor + juros));
    }

    private static decimal Arredondar(decimal valor) =>
        Math.Round(valor, 2, MidpointRounding.AwayFromZero);
}
