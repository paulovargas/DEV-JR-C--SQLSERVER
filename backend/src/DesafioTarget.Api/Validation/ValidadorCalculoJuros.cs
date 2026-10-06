using DesafioTarget.Api.Services;

namespace DesafioTarget.Api.Validation;

public static class ValidadorCalculoJuros
{
    public static Dictionary<string, string[]> Validar(decimal valor, DateOnly dataVencimento, DateOnly dataCalculo)
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

        return erros;
    }
}
