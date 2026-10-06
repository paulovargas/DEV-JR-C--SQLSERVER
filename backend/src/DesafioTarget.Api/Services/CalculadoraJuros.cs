using DesafioTarget.Api.Models.Juros;
using DesafioTarget.Api.Services.Interfaces;
using DesafioTarget.Api.Validation;

namespace DesafioTarget.Api.Services;

public sealed class CalculadoraJuros : ICalculadoraJuros
{
    private const decimal TaxaDiaria = 0.025m;

    public CalculoJurosResponse Calcular(
        decimal valor,
        DateOnly dataVencimento,
        DateOnly dataCalculo)
    {
        var erros = ValidadorCalculoJuros.Validar(valor, dataVencimento, dataCalculo);
        if (erros.Count > 0)
            throw new CalculoInvalidoException(erros);

        var diasAtraso = Math.Max(0, dataCalculo.DayNumber - dataVencimento.DayNumber);
        decimal juros;
        decimal valorAtualizado;
        try
        {
            // Calcula o fator primeiro para evitar perda de precisão em valores próximos do limite.
            juros = ArredondamentoMonetario.Arredondar(valor * (TaxaDiaria * diasAtraso));
            valorAtualizado = ArredondamentoMonetario.Arredondar(valor + juros);
        }
        catch (OverflowException)
        {
            throw CriarErroLimiteCalculo();
        }

        if (juros > LimitesMonetarios.ValorMaximo || valorAtualizado > LimitesMonetarios.ValorMaximo)
            throw CriarErroLimiteCalculo();

        return new CalculoJurosResponse(
            ArredondamentoMonetario.Arredondar(valor),
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
}
