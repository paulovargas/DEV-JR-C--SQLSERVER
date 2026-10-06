namespace DesafioTarget.Api.Models;

public sealed record ProdutoEstoque(
    int CodigoProduto,
    string DescricaoProduto,
    int Estoque);

public sealed record EstoqueSeed(List<ProdutoEstoque>? Estoque);

public enum TipoMovimentacao
{
    Entrada,
    Saida
}

public sealed record MovimentacaoEstoqueRequest(
    int CodigoProduto,
    TipoMovimentacao Tipo,
    int Quantidade,
    string? Descricao);

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

public enum StatusMovimentacao
{
    Sucesso,
    ProdutoNaoEncontrado,
    EstoqueInsuficiente,
    LimiteDeEstoqueExcedido
}

public sealed record ResultadoMovimentacao(
    StatusMovimentacao Status,
    MovimentacaoEstoque? Movimentacao,
    string? Erro);
