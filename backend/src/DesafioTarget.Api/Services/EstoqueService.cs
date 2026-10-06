using System.Data;
using DesafioTarget.Api.Data;
using DesafioTarget.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DesafioTarget.Api.Services;

public interface IEstoqueService
{
    Task<IReadOnlyList<ProdutoEstoque>> ListarProdutosAsync(CancellationToken cancellationToken);
    Task<ProdutoEstoque?> ObterProdutoAsync(int codigoProduto, CancellationToken cancellationToken);
    Task<IReadOnlyList<MovimentacaoEstoque>> ListarMovimentacoesAsync(CancellationToken cancellationToken);
    Task<MovimentacaoEstoque?> ObterMovimentacaoAsync(long id, CancellationToken cancellationToken);
    Task<ResultadoMovimentacao> MovimentarAsync(MovimentacaoEstoqueRequest request, CancellationToken cancellationToken);
}

public sealed class EstoqueService(DesafioTargetDbContext contexto, TimeProvider relogio) : IEstoqueService
{
    public async Task<IReadOnlyList<ProdutoEstoque>> ListarProdutosAsync(CancellationToken cancellationToken) =>
        await contexto.Produtos.AsNoTracking().OrderBy(produto => produto.CodigoProduto)
            .Select(produto => new ProdutoEstoque(produto.CodigoProduto, produto.DescricaoProduto, produto.Estoque))
            .ToListAsync(cancellationToken);

    public async Task<ProdutoEstoque?> ObterProdutoAsync(int codigoProduto, CancellationToken cancellationToken) =>
        await contexto.Produtos.AsNoTracking().Where(produto => produto.CodigoProduto == codigoProduto)
            .Select(produto => new ProdutoEstoque(produto.CodigoProduto, produto.DescricaoProduto, produto.Estoque))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<MovimentacaoEstoque>> ListarMovimentacoesAsync(CancellationToken cancellationToken) =>
        await contexto.Movimentacoes.AsNoTracking().OrderByDescending(movimentacao => movimentacao.RealizadaEm)
            .Select(movimentacao => ParaModelo(movimentacao)).ToListAsync(cancellationToken);

    public async Task<MovimentacaoEstoque?> ObterMovimentacaoAsync(long id, CancellationToken cancellationToken)
    {
        var movimentacao = await contexto.Movimentacoes.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return movimentacao is null ? null : ParaModelo(movimentacao);
    }

    public async Task<ResultadoMovimentacao> MovimentarAsync(MovimentacaoEstoqueRequest request, CancellationToken cancellationToken)
    {
        await using var transacao = contexto.Database.IsRelational()
            ? await contexto.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;
        var produto = await contexto.Produtos.SingleOrDefaultAsync(item => item.CodigoProduto == request.CodigoProduto, cancellationToken);

        if (produto is null)
            return new ResultadoMovimentacao(StatusMovimentacao.ProdutoNaoEncontrado, null, $"Produto de código {request.CodigoProduto} não encontrado.");

        if (request.Tipo == TipoMovimentacao.Saida && request.Quantidade > produto.Estoque)
            return new ResultadoMovimentacao(StatusMovimentacao.EstoqueInsuficiente, null, $"Estoque insuficiente. Saldo atual: {produto.Estoque}.");

        int estoqueFinal;
        try
        {
            estoqueFinal = request.Tipo == TipoMovimentacao.Entrada
                ? checked(produto.Estoque + request.Quantidade)
                : produto.Estoque - request.Quantidade;
        }
        catch (OverflowException)
        {
            return new ResultadoMovimentacao(StatusMovimentacao.LimiteDeEstoqueExcedido, null,
                "A quantidade informada excede o limite suportado para o estoque.");
        }

        var movimentacao = new MovimentacaoEstoqueEntity
        {
            CodigoProduto = produto.CodigoProduto,
            DescricaoProduto = produto.DescricaoProduto,
            Tipo = request.Tipo,
            Quantidade = request.Quantidade,
            Descricao = request.Descricao!.Trim(),
            EstoqueAnterior = produto.Estoque,
            EstoqueFinal = estoqueFinal,
            RealizadaEm = relogio.GetUtcNow()
        };

        produto.Estoque = estoqueFinal;
        contexto.Movimentacoes.Add(movimentacao);
        await contexto.SaveChangesAsync(cancellationToken);
        if (transacao is not null)
            await transacao.CommitAsync(cancellationToken);

        return new ResultadoMovimentacao(StatusMovimentacao.Sucesso, ParaModelo(movimentacao), null);
    }

    private static MovimentacaoEstoque ParaModelo(MovimentacaoEstoqueEntity movimentacao) => new(
        movimentacao.Id, movimentacao.CodigoProduto, movimentacao.DescricaoProduto, movimentacao.Tipo,
        movimentacao.Quantidade, movimentacao.Descricao, movimentacao.EstoqueAnterior,
        movimentacao.EstoqueFinal, movimentacao.RealizadaEm);
}
