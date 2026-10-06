namespace DesafioTarget.Api.Models.Estoque;

public sealed record ProdutoEstoque(
    int CodigoProduto,
    string DescricaoProduto,
    int Estoque);
