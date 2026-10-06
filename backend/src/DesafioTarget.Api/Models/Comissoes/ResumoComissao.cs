namespace DesafioTarget.Api.Models.Comissoes;

public sealed record ResumoComissao(
    string Vendedor,
    int QuantidadeVendas,
    decimal ValorTotalVendas,
    decimal ComissaoTotal);
