using DesafioTarget.Api.Models.Estoque;

namespace DesafioTarget.Api.Services.Interfaces;

public interface IEstoqueService
{
    Task<IReadOnlyList<ProdutoEstoque>> ListarProdutosAsync(CancellationToken cancellationToken);
    Task<ProdutoEstoque?> ObterProdutoAsync(int codigoProduto, CancellationToken cancellationToken);
    Task<IReadOnlyList<MovimentacaoEstoque>> ListarMovimentacoesAsync(CancellationToken cancellationToken);
    Task<MovimentacaoEstoque?> ObterMovimentacaoAsync(long id, CancellationToken cancellationToken);
    Task<ResultadoMovimentacao> MovimentarAsync(MovimentacaoEstoqueRequest request, CancellationToken cancellationToken);
}
