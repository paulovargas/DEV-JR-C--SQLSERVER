namespace DesafioTarget.Api.Models.Estoque;

public sealed record MovimentacaoEstoque(
    long Id,
    int CodigoProduto,
    string DescricaoProduto,
    TipoMovimentacao Tipo,
    int Quantidade,
    string Descricao,
    int EstoqueAnterior,
    int EstoqueFinal,
    DateTimeOffset RealizadaEm);
