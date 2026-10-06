using DesafioTarget.Api.Data;
using DesafioTarget.Api.Data.Entities;
using DesafioTarget.Api.Models.Estoque;
using DesafioTarget.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace DesafioTarget.Api.Tests;

public sealed class EstoqueServiceTests
{
    [Fact]
    public async Task ConsultarProdutos_DevePreservarCamposOrdenacaoEBuscaPorCodigo()
    {
        await using var contexto = CriarContexto(150);
        contexto.Produtos.Add(new ProdutoEstoqueEntity { CodigoProduto = 99, DescricaoProduto = "Borracha", Estoque = 10 });
        await contexto.SaveChangesAsync();
        contexto.ChangeTracker.Clear();
        var estoque = new EstoqueService(contexto, TimeProvider.System);

        var produtos = await estoque.ListarProdutosAsync(CancellationToken.None);
        var produto = await estoque.ObterProdutoAsync(101, CancellationToken.None);
        var inexistente = await estoque.ObterProdutoAsync(999, CancellationToken.None);

        Assert.Equal(new[] { new ProdutoEstoque(99, "Borracha", 10), new ProdutoEstoque(101, "Caneta Azul", 150) }, produtos);
        Assert.Equal(produtos[1], produto);
        Assert.Null(inexistente);
        Assert.Empty(contexto.ChangeTracker.Entries());
    }

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

    public static IEnumerable<object[]> MovimentacoesInvalidas()
    {
        yield return [new MovimentacaoEstoqueRequest(0, TipoMovimentacao.Entrada, 1, "Compra"), "codigoProduto"];
        yield return [new MovimentacaoEstoqueRequest(-1, TipoMovimentacao.Entrada, 1, "Compra"), "codigoProduto"];
        yield return [new MovimentacaoEstoqueRequest(101, null, 1, "Compra"), "tipo"];
        yield return [new MovimentacaoEstoqueRequest(101, (TipoMovimentacao)99, 1, "Compra"), "tipo"];
        foreach (var tipo in new[] { TipoMovimentacao.Entrada, TipoMovimentacao.Saida })
        {
            foreach (var quantidade in new[] { 0, -1, int.MinValue })
                yield return [new MovimentacaoEstoqueRequest(101, tipo, quantidade, "Compra"), "quantidade"];
        }
        foreach (var descricao in new[] { null, "", "   ", new string('a', 501) })
            yield return [new MovimentacaoEstoqueRequest(101, TipoMovimentacao.Entrada, 1, descricao), "descricao"];
    }

    [Theory]
    [MemberData(nameof(MovimentacoesInvalidas))]
    public async Task MovimentarAsync_DeveRejeitarDadosInvalidosSemAlterarSaldoOuHistorico(MovimentacaoEstoqueRequest request, string campo)
    {
        await using var contexto = CriarContexto(10);
        var estoque = new EstoqueService(contexto, TimeProvider.System);

        var resultado = await estoque.MovimentarAsync(request, CancellationToken.None);

        Assert.Equal(StatusMovimentacao.DadosInvalidos, resultado.Status);
        Assert.Null(resultado.Movimentacao);
        Assert.NotNull(resultado.ErrosValidacao);
        Assert.True(resultado.ErrosValidacao.ContainsKey(campo));
        Assert.Equal(10, (await estoque.ObterProdutoAsync(101, CancellationToken.None))!.Estoque);
        Assert.Empty(await estoque.ListarMovimentacoesAsync(CancellationToken.None));
        Assert.False(contexto.ChangeTracker.HasChanges());
    }

    [Fact]
    public async Task MovimentarAsync_DeveAcumularErrosDeTodosOsCamposInvalidos()
    {
        await using var contexto = CriarContexto(10);
        var estoque = new EstoqueService(contexto, TimeProvider.System);

        var resultado = await estoque.MovimentarAsync(new MovimentacaoEstoqueRequest(0, null, 0, null), CancellationToken.None);

        Assert.Equal(StatusMovimentacao.DadosInvalidos, resultado.Status);
        Assert.NotNull(resultado.ErrosValidacao);
        Assert.Equal(4, resultado.ErrosValidacao.Count);
        Assert.Equal(10, (await estoque.ObterProdutoAsync(101, CancellationToken.None))!.Estoque);
        Assert.Empty(await estoque.ListarMovimentacoesAsync(CancellationToken.None));
    }

    [Fact]
    public async Task MovimentarAsync_DeveRejeitarProdutoInexistenteSemAlterarSaldoOuHistorico()
    {
        await using var contexto = CriarContexto(10);
        var estoque = new EstoqueService(contexto, TimeProvider.System);

        var resultado = await estoque.MovimentarAsync(new MovimentacaoEstoqueRequest(999, TipoMovimentacao.Entrada, 1, "Compra"), CancellationToken.None);

        Assert.Equal(StatusMovimentacao.ProdutoNaoEncontrado, resultado.Status);
        Assert.Null(resultado.Movimentacao);
        Assert.Equal(10, (await estoque.ObterProdutoAsync(101, CancellationToken.None))!.Estoque);
        Assert.Empty(await estoque.ListarMovimentacoesAsync(CancellationToken.None));
    }

    [Fact]
    public async Task MovimentarAsync_DevePermitirSaidaIgualAoSaldo()
    {
        await using var contexto = CriarContexto(10);
        var estoque = new EstoqueService(contexto, TimeProvider.System);

        var resultado = await estoque.MovimentarAsync(new MovimentacaoEstoqueRequest(101, TipoMovimentacao.Saida, 10, "Venda"), CancellationToken.None);

        Assert.Equal(StatusMovimentacao.Sucesso, resultado.Status);
        Assert.Equal(0, resultado.Movimentacao!.EstoqueFinal);
        Assert.Equal(0, (await estoque.ObterProdutoAsync(101, CancellationToken.None))!.Estoque);
        Assert.Equal(resultado.Movimentacao, Assert.Single(await estoque.ListarMovimentacoesAsync(CancellationToken.None)));
    }

    [Fact]
    public async Task MovimentarAsync_DeveRejeitarOverflowSemAlterarSaldoOuHistorico()
    {
        await using var contexto = CriarContexto(int.MaxValue);
        var estoque = new EstoqueService(contexto, TimeProvider.System);

        var resultado = await estoque.MovimentarAsync(new MovimentacaoEstoqueRequest(101, TipoMovimentacao.Entrada, 1, "Compra"), CancellationToken.None);

        Assert.Equal(StatusMovimentacao.LimiteDeEstoqueExcedido, resultado.Status);
        Assert.Null(resultado.Movimentacao);
        Assert.Equal(int.MaxValue, (await estoque.ObterProdutoAsync(101, CancellationToken.None))!.Estoque);
        Assert.Empty(await estoque.ListarMovimentacoesAsync(CancellationToken.None));
        Assert.False(contexto.ChangeTracker.HasChanges());
    }

    [Fact]
    public async Task MovimentarAsync_DevePermitirEntradaAteOLimiteDoEstoque()
    {
        await using var contexto = CriarContexto(int.MaxValue - 1);
        var estoque = new EstoqueService(contexto, TimeProvider.System);

        var resultado = await estoque.MovimentarAsync(new MovimentacaoEstoqueRequest(101, TipoMovimentacao.Entrada, 1, "Compra"), CancellationToken.None);

        Assert.Equal(StatusMovimentacao.Sucesso, resultado.Status);
        Assert.Equal(int.MaxValue, (await estoque.ObterProdutoAsync(101, CancellationToken.None))!.Estoque);
        Assert.Equal(resultado.Movimentacao, Assert.Single(await estoque.ListarMovimentacoesAsync(CancellationToken.None)));
    }

    [Fact]
    public async Task MovimentarAsync_DeveAceitarDescricaoNoLimiteAposRemoverEspacosExternos()
    {
        await using var contexto = CriarContexto(10);
        var estoque = new EstoqueService(contexto, TimeProvider.System);
        var descricao = new string('a', 500);

        var resultado = await estoque.MovimentarAsync(new MovimentacaoEstoqueRequest(101, TipoMovimentacao.Entrada, 1, $"  {descricao}  "), CancellationToken.None);

        Assert.Equal(StatusMovimentacao.Sucesso, resultado.Status);
        Assert.Equal(descricao, resultado.Movimentacao!.Descricao);
        Assert.Equal(11, (await estoque.ObterProdutoAsync(101, CancellationToken.None))!.Estoque);
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
