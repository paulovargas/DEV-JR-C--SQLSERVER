namespace DesafioTarget.Api.Models.Juros;

public sealed record CalculoJurosRequest(
    decimal Valor,
    DateOnly DataVencimento);
