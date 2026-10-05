using DesafioTarget.Api.Models;
using DesafioTarget.Api.Services;

namespace DesafioTarget.Api.Tests;

public sealed class EstoqueServiceTests
{
    [Fact]
    public void Movimentar_DeveAtualizarOSaldoEmEntradasESaidas()
    {
        var estoque = CriarEstoque(150);

        var entrada = estoque.Movimentar(
            new MovimentacaoEstoqueRequest(101, TipoMovimentacao.Entrada, 25, "Compra"));
        var saida = estoque.Movimentar(
            new MovimentacaoEstoqueRequest(101, TipoMovimentacao.Saida, 40, "Venda"));

        Assert.Equal(StatusMovimentacao.Sucesso, entrada.Status);
        Assert.Equal(150, entrada.Movimentacao!.EstoqueAnterior);
        Assert.Equal(175, entrada.Movimentacao.EstoqueFinal);
        Assert.Equal(StatusMovimentacao.Sucesso, saida.Status);
        Assert.Equal(135, saida.Movimentacao!.EstoqueFinal);
        Assert.NotEqual(entrada.Movimentacao.Id, saida.Movimentacao.Id);
        Assert.Equal(135, estoque.ObterProduto(101)!.Estoque);
    }

    [Fact]
    public void Movimentar_NaoDeveAlterarSaldoQuandoASaidaExcedeOEstoque()
    {
        var estoque = CriarEstoque(10);

        var resultado = estoque.Movimentar(
            new MovimentacaoEstoqueRequest(101, TipoMovimentacao.Saida, 11, "Venda"));

        Assert.Equal(StatusMovimentacao.EstoqueInsuficiente, resultado.Status);
        Assert.Equal(10, estoque.ObterProduto(101)!.Estoque);
        Assert.Empty(estoque.ListarMovimentacoes());
    }

    [Fact]
    public void Movimentar_DeveManterAtualizacoesConcorrentes()
    {
        var estoque = CriarEstoque(0);

        Parallel.For(0, 100, indice =>
            estoque.Movimentar(
                new MovimentacaoEstoqueRequest(101, TipoMovimentacao.Entrada, 1, $"Entrada {indice}")));

        Assert.Equal(100, estoque.ObterProduto(101)!.Estoque);
        Assert.Equal(100, estoque.ListarMovimentacoes().Count);
    }

    private static EstoqueService CriarEstoque(int quantidade) =>
        new(
            [new ProdutoEstoque(101, "Caneta Azul", quantidade)],
            TimeProvider.System);
}
