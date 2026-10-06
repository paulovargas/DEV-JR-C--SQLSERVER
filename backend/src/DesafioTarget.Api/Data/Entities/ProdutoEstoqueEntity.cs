namespace DesafioTarget.Api.Data.Entities;

public sealed class ProdutoEstoqueEntity
{
    public int CodigoProduto { get; set; }
    public string DescricaoProduto { get; set; } = string.Empty;
    public int Estoque { get; set; }
}
