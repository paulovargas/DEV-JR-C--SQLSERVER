namespace DesafioTarget.Api.Models.Juros;

public sealed record CalculoJurosResponse(
    decimal ValorOriginal,
    DateOnly DataVencimento,
    DateOnly DataCalculo,
    int DiasAtraso,
    decimal TaxaDiariaPercentual,
    decimal ValorJuros,
    decimal ValorAtualizado);
