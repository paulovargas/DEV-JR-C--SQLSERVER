namespace DesafioTarget.Api.Models.Estoque;

public enum StatusMovimentacao
{
    Sucesso,
    ProdutoNaoEncontrado,
    EstoqueInsuficiente,
    LimiteDeEstoqueExcedido,
    DadosInvalidos
}
