using DesafioTarget.Api.Errors;
using DesafioTarget.Api.Models;
using DesafioTarget.Api.Services;

namespace DesafioTarget.Api.Endpoints;

public static class EstoqueEndpoints
{
    public static IEndpointRouteBuilder MapearEstoque(this IEndpointRouteBuilder app)
    {
        var produtos = app.MapGroup("/api/produtos");

        produtos.MapGet("/", async (
            IEstoqueService estoque,
            CancellationToken cancellationToken) =>
            Results.Ok(await estoque.ListarProdutosAsync(cancellationToken)));

        produtos.MapGet("/{codigoProduto:int}", async (
            int codigoProduto,
            IEstoqueService estoque,
            HttpContext contexto,
            CancellationToken cancellationToken) =>
        {
            var produto = await estoque.ObterProdutoAsync(codigoProduto, cancellationToken);
            return produto is null
                ? RespostasErro.Problema(
                    contexto,
                    StatusCodes.Status404NotFound,
                    $"Produto de código {codigoProduto} não encontrado.")
                : Results.Ok(produto);
        });

        var movimentacoes = app.MapGroup("/api/movimentacoes");

        movimentacoes.MapGet("/", async (
            IEstoqueService estoque,
            CancellationToken cancellationToken) =>
            Results.Ok(await estoque.ListarMovimentacoesAsync(cancellationToken)));

        movimentacoes.MapGet("/{id:long}", async (
            long id,
            IEstoqueService estoque,
            HttpContext contexto,
            CancellationToken cancellationToken) =>
        {
            var movimentacao = await estoque.ObterMovimentacaoAsync(id, cancellationToken);
            return movimentacao is null
                ? RespostasErro.Problema(
                    contexto,
                    StatusCodes.Status404NotFound,
                    $"Movimentação {id} não encontrada.")
                : Results.Ok(movimentacao);
        });

        movimentacoes.MapPost("/", async (
            MovimentacaoEstoqueRequest request,
            IEstoqueService estoque,
            HttpContext contexto,
            CancellationToken cancellationToken) =>
        {
            var resultado = await estoque.MovimentarAsync(request, cancellationToken);

            return resultado.Status switch
            {
                StatusMovimentacao.DadosInvalidos => RespostasErro.Validacao(contexto, resultado.ErrosValidacao!),
                StatusMovimentacao.Sucesso => Results.Created(
                    $"/api/movimentacoes/{resultado.Movimentacao!.Id}",
                    resultado.Movimentacao),
                StatusMovimentacao.ProdutoNaoEncontrado => RespostasErro.Problema(
                    contexto, StatusCodes.Status404NotFound, resultado.Erro),
                StatusMovimentacao.EstoqueInsuficiente => RespostasErro.Problema(
                    contexto, StatusCodes.Status409Conflict, resultado.Erro),
                StatusMovimentacao.LimiteDeEstoqueExcedido => RespostasErro.Problema(
                    contexto, StatusCodes.Status400BadRequest, resultado.Erro),
                _ => throw new InvalidOperationException("O serviço retornou um status de movimentação desconhecido.")
            };
        });

        return app;
    }
}
