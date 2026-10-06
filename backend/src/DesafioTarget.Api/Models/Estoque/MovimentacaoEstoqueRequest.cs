namespace DesafioTarget.Api.Models.Estoque;

public sealed record MovimentacaoEstoqueRequest(
    int CodigoProduto,
    TipoMovimentacao? Tipo,
    int Quantidade,
    string? Descricao);
