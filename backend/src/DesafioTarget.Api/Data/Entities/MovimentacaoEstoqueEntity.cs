using DesafioTarget.Api.Models.Estoque;

namespace DesafioTarget.Api.Data.Entities;

public sealed class MovimentacaoEstoqueEntity
{
    public long Id { get; set; }
    public int CodigoProduto { get; set; }
    public string DescricaoProduto { get; set; } = string.Empty;
    public TipoMovimentacao Tipo { get; set; }
    public int Quantidade { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public int EstoqueAnterior { get; set; }
    public int EstoqueFinal { get; set; }
    public DateTimeOffset RealizadaEm { get; set; }
}
