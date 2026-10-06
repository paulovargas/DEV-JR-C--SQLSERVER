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
        var erros = new Dictionary<string, string[]>();
        if (valor <= 0m)
            erros["valor"] = ["O valor deve ser maior que zero."];
        else if (valor > LimitesMonetarios.ValorMaximo)
            erros["valor"] = [LimitesMonetarios.MensagemValorMaximo];

        if (dataVencimento == default)
            erros["dataVencimento"] = ["A data de vencimento é obrigatória."];
        if (dataCalculo == default)
            erros["dataCalculo"] = ["A data de cálculo é obrigatória."];

        if (erros.Count > 0)
            throw new CalculoInvalidoException(erros);

        var diasAtraso = Math.Max(0, dataCalculo.DayNumber - dataVencimento.DayNumber);
        decimal juros;
        decimal valorAtualizado;
        try
        {
            // Calcula o fator primeiro para evitar perda de precisão em valores próximos do limite.
            juros = Arredondar(valor * (TaxaDiaria * diasAtraso));
            valorAtualizado = Arredondar(valor + juros);
        }
        catch (OverflowException)
        {
            throw CriarErroLimiteCalculo();
        }

        if (juros > LimitesMonetarios.ValorMaximo || valorAtualizado > LimitesMonetarios.ValorMaximo)
            throw CriarErroLimiteCalculo();

        return new CalculoJurosResponse(
            Arredondar(valor),
            dataVencimento,
            dataCalculo,
            diasAtraso,
            TaxaDiaria * 100m,
            juros,
            valorAtualizado);
    }

    private static CalculoInvalidoException CriarErroLimiteCalculo() => new(new Dictionary<string, string[]>
    {
        ["valor"] = ["O cálculo de juros excede o limite monetário suportado. Reduza o valor ou o período de atraso."]
    });

    private static decimal Arredondar(decimal valor) =>
        Math.Round(valor, 2, MidpointRounding.AwayFromZero);
}
