namespace DesafioTarget.Api.Models.Comissoes;

public sealed record CalculoComissaoResponse(
    IReadOnlyList<ResumoComissao> Vendedores,
    decimal ComissaoTotalGeral);
