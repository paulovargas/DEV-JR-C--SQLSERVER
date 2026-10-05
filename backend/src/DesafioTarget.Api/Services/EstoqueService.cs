using System.Text.Json;
using DesafioTarget.Api.Models;

namespace DesafioTarget.Api.Services;

public interface IEstoqueService
{
    IReadOnlyList<ProdutoEstoque> ListarProdutos();
    ProdutoEstoque? ObterProduto(int codigoProduto);
    IReadOnlyList<MovimentacaoEstoque> ListarMovimentacoes();
    MovimentacaoEstoque? ObterMovimentacao(Guid id);
    ResultadoMovimentacao Movimentar(MovimentacaoEstoqueRequest request);
}

public sealed class EstoqueService : IEstoqueService
{
    private readonly object _controleConcorrencia = new();
    private readonly Dictionary<int, ProdutoEstoque> _produtos;
    private readonly List<MovimentacaoEstoque> _movimentacoes = [];
    private readonly TimeProvider _relogio;

    public EstoqueService(IEnumerable<ProdutoEstoque> produtos, TimeProvider relogio)
    {
        _produtos = produtos.ToDictionary(produto => produto.CodigoProduto);
        _relogio = relogio;
    }

    public static EstoqueService CarregarDeArquivo(string caminho, TimeProvider relogio)
    {
        if (!File.Exists(caminho))
        {
            throw new FileNotFoundException("O arquivo de estoque inicial não foi encontrado.", caminho);
        }

        var json = File.ReadAllText(caminho);
        var opcoes = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var dados = JsonSerializer.Deserialize<EstoqueSeed>(json, opcoes);

        if (dados?.Estoque is null || dados.Estoque.Count == 0)
        {
            throw new InvalidDataException("O arquivo de estoque inicial não contém produtos.");
        }

        if (dados.Estoque.Any(produto =>
                produto.CodigoProduto <= 0 ||
                string.IsNullOrWhiteSpace(produto.DescricaoProduto) ||
                produto.Estoque < 0))
        {
            throw new InvalidDataException("O arquivo de estoque inicial contém um produto inválido.");
        }

        if (dados.Estoque.Select(produto => produto.CodigoProduto).Distinct().Count() != dados.Estoque.Count)
        {
            throw new InvalidDataException("O arquivo de estoque inicial contém códigos duplicados.");
        }

        return new EstoqueService(dados.Estoque, relogio);
    }

    public IReadOnlyList<ProdutoEstoque> ListarProdutos()
    {
        lock (_controleConcorrencia)
        {
            return _produtos.Values
                .OrderBy(produto => produto.CodigoProduto)
                .ToArray();
        }
    }

    public ProdutoEstoque? ObterProduto(int codigoProduto)
    {
        lock (_controleConcorrencia)
        {
            return _produtos.GetValueOrDefault(codigoProduto);
        }
    }

    public IReadOnlyList<MovimentacaoEstoque> ListarMovimentacoes()
    {
        lock (_controleConcorrencia)
        {
            return _movimentacoes.ToArray();
        }
    }

    public MovimentacaoEstoque? ObterMovimentacao(Guid id)
    {
        lock (_controleConcorrencia)
        {
            return _movimentacoes.FirstOrDefault(movimentacao => movimentacao.Id == id);
        }
    }

    public ResultadoMovimentacao Movimentar(MovimentacaoEstoqueRequest request)
    {
        lock (_controleConcorrencia)
        {
            if (!_produtos.TryGetValue(request.CodigoProduto, out var produto))
            {
                return new ResultadoMovimentacao(
                    StatusMovimentacao.ProdutoNaoEncontrado,
                    null,
                    $"Produto de código {request.CodigoProduto} não encontrado.");
            }

            if (request.Tipo == TipoMovimentacao.Saida && request.Quantidade > produto.Estoque)
            {
                return new ResultadoMovimentacao(
                    StatusMovimentacao.EstoqueInsuficiente,
                    null,
                    $"Estoque insuficiente. Saldo atual: {produto.Estoque}.");
            }

            int estoqueFinal;

            try
            {
                estoqueFinal = request.Tipo == TipoMovimentacao.Entrada
                    ? checked(produto.Estoque + request.Quantidade)
                    : produto.Estoque - request.Quantidade;
            }
            catch (OverflowException)
            {
                return new ResultadoMovimentacao(
                    StatusMovimentacao.LimiteDeEstoqueExcedido,
                    null,
                    "A quantidade informada excede o limite suportado para o estoque.");
            }

            var produtoAtualizado = produto with { Estoque = estoqueFinal };
            var movimentacao = new MovimentacaoEstoque(
                Guid.NewGuid(),
                produto.CodigoProduto,
                produto.DescricaoProduto,
                request.Tipo,
                request.Quantidade,
                request.Descricao!.Trim(),
                produto.Estoque,
                estoqueFinal,
                _relogio.GetUtcNow());

            _produtos[produto.CodigoProduto] = produtoAtualizado;
            _movimentacoes.Add(movimentacao);

            return new ResultadoMovimentacao(StatusMovimentacao.Sucesso, movimentacao, null);
        }
    }
}
