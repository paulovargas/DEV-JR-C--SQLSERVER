namespace DesafioTarget.Api.Models;

public sealed record Venda(string? Vendedor, decimal Valor);

public sealed record CalculoComissaoRequest(List<Venda>? Vendas);

public sealed record ResumoComissao(
    string Vendedor,
    int QuantidadeVendas,
    decimal ValorTotalVendas,
    decimal ComissaoTotal);

public sealed record CalculoComissaoResponse(
    IReadOnlyList<ResumoComissao> Vendedores,
    decimal ComissaoTotalGeral);
