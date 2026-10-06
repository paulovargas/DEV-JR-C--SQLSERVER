using DesafioTarget.Api.Data;
using DesafioTarget.Api.Models;
using DesafioTarget.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace DesafioTarget.Api.Tests;

public sealed class EstoqueServiceTests
{
    [Fact]
    public async Task MovimentarAsync_DeveAtualizarOSaldoEmEntradasESaidas()
    {
        await using var contexto = CriarContexto(150);
        var estoque = new EstoqueService(contexto, TimeProvider.System);

        var entrada = await estoque.MovimentarAsync(new MovimentacaoEstoqueRequest(101, TipoMovimentacao.Entrada, 25, "Compra"), CancellationToken.None);
        var saida = await estoque.MovimentarAsync(new MovimentacaoEstoqueRequest(101, TipoMovimentacao.Saida, 40, "Venda"), CancellationToken.None);

        Assert.Equal(StatusMovimentacao.Sucesso, entrada.Status);
        Assert.Equal(150, entrada.Movimentacao!.EstoqueAnterior);
        Assert.Equal(175, entrada.Movimentacao.EstoqueFinal);
        Assert.Equal(StatusMovimentacao.Sucesso, saida.Status);
        Assert.Equal(135, saida.Movimentacao!.EstoqueFinal);
        Assert.NotEqual(entrada.Movimentacao.Id, saida.Movimentacao.Id);
        Assert.Equal(135, (await estoque.ObterProdutoAsync(101, CancellationToken.None))!.Estoque);
    }

    [Fact]
    public async Task MovimentarAsync_NaoDeveAlterarSaldoQuandoASaidaExcedeOEstoque()
    {
        await using var contexto = CriarContexto(10);
        var estoque = new EstoqueService(contexto, TimeProvider.System);

        var resultado = await estoque.MovimentarAsync(new MovimentacaoEstoqueRequest(101, TipoMovimentacao.Saida, 11, "Venda"), CancellationToken.None);

        Assert.Equal(StatusMovimentacao.EstoqueInsuficiente, resultado.Status);
        Assert.Equal(10, (await estoque.ObterProdutoAsync(101, CancellationToken.None))!.Estoque);
        Assert.Empty(await estoque.ListarMovimentacoesAsync(CancellationToken.None));
    }

    private static DesafioTargetDbContext CriarContexto(int quantidade)
    {
        var opcoes = new DbContextOptionsBuilder<DesafioTargetDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var contexto = new DesafioTargetDbContext(opcoes);
        contexto.Produtos.Add(new ProdutoEstoqueEntity { CodigoProduto = 101, DescricaoProduto = "Caneta Azul", Estoque = quantidade });
        contexto.SaveChanges();
        return contexto;
    }
}
