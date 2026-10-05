namespace DesafioTarget.Api.Models;

public sealed record CalculoJurosRequest(
    decimal Valor,
    DateOnly DataVencimento);

public sealed record CalculoJurosResponse(
    decimal ValorOriginal,
    DateOnly DataVencimento,
    DateOnly DataCalculo,
    int DiasAtraso,
    decimal TaxaDiariaPercentual,
    decimal ValorJuros,
    decimal ValorAtualizado);
