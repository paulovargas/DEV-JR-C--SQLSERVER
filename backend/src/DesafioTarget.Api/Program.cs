using System.Text.Json;
using System.Text.Json.Serialization;
using DesafioTarget.Api.Models;
using DesafioTarget.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(opcoes =>
{
    opcoes.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    opcoes.SerializerOptions.Converters.Add(
        new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
});

builder.Services.AddSingleton<ICalculadoraComissao, CalculadoraComissao>();
builder.Services.AddSingleton<ICalculadoraJuros, CalculadoraJuros>();
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
builder.Services.AddSingleton<IEstoqueService>(provedor =>
{
    var relogio = provedor.GetRequiredService<TimeProvider>();
    var caminhoEstoque = Path.Combine(AppContext.BaseDirectory, "Data", "estoque.json");
    return EstoqueService.CarregarDeArquivo(caminhoEstoque, relogio);
});

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    aplicacao = "Desafio técnico Target Sistemas",
    endpoints = new[]
    {
        "POST /api/comissoes/calcular",
        "GET /api/produtos",
        "GET /api/produtos/{codigoProduto}",
        "POST /api/movimentacoes",
        "GET /api/movimentacoes",
        "GET /api/movimentacoes/{id}",
        "POST /api/juros/calcular"
    }
}));

var comissoes = app.MapGroup("/api/comissoes");

comissoes.MapPost("/calcular", (
    CalculoComissaoRequest request,
    ICalculadoraComissao calculadora) =>
{
    var erros = ValidarVendas(request.Vendas);

    if (erros.Count > 0)
    {
        return Results.ValidationProblem(erros);
    }

    return Results.Ok(calculadora.Calcular(request.Vendas!));
});

var produtos = app.MapGroup("/api/produtos");

produtos.MapGet("/", (IEstoqueService estoque) =>
    Results.Ok(estoque.ListarProdutos()));

produtos.MapGet("/{codigoProduto:int}", (int codigoProduto, IEstoqueService estoque) =>
{
    var produto = estoque.ObterProduto(codigoProduto);
    return produto is null
        ? Results.NotFound(new { erro = $"Produto de código {codigoProduto} não encontrado." })
        : Results.Ok(produto);
});

var movimentacoes = app.MapGroup("/api/movimentacoes");

movimentacoes.MapGet("/", (IEstoqueService estoque) =>
    Results.Ok(estoque.ListarMovimentacoes()));

movimentacoes.MapGet("/{id:guid}", (Guid id, IEstoqueService estoque) =>
{
    var movimentacao = estoque.ObterMovimentacao(id);
    return movimentacao is null
        ? Results.NotFound(new { erro = $"Movimentação {id} não encontrada." })
        : Results.Ok(movimentacao);
});

movimentacoes.MapPost("/", (MovimentacaoEstoqueRequest request, IEstoqueService estoque) =>
{
    var erros = ValidarMovimentacao(request);

    if (erros.Count > 0)
    {
        return Results.ValidationProblem(erros);
    }

    var resultado = estoque.Movimentar(request);

    return resultado.Status switch
    {
        StatusMovimentacao.Sucesso => Results.Created(
            $"/api/movimentacoes/{resultado.Movimentacao!.Id}",
            resultado.Movimentacao),
        StatusMovimentacao.ProdutoNaoEncontrado => Results.NotFound(new { erro = resultado.Erro }),
        StatusMovimentacao.EstoqueInsuficiente => Results.Conflict(new { erro = resultado.Erro }),
        StatusMovimentacao.LimiteDeEstoqueExcedido => Results.BadRequest(new { erro = resultado.Erro }),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError)
    };
});

var juros = app.MapGroup("/api/juros");

juros.MapPost("/calcular", (
    CalculoJurosRequest request,
    ICalculadoraJuros calculadora,
    TimeProvider relogio) =>
{
    var erros = ValidarCalculoJuros(request);

    if (erros.Count > 0)
    {
        return Results.ValidationProblem(erros);
    }

    var hoje = DateOnly.FromDateTime(relogio.GetLocalNow().DateTime);
    return Results.Ok(calculadora.Calcular(request.Valor, request.DataVencimento, hoje));
});

app.Run();

static Dictionary<string, string[]> ValidarVendas(List<Venda>? vendas)
{
    var erros = new Dictionary<string, string[]>();

    if (vendas is null)
    {
        erros["vendas"] = ["A lista de vendas é obrigatória."];
        return erros;
    }

    for (var indice = 0; indice < vendas.Count; indice++)
    {
        var venda = vendas[indice];

        if (string.IsNullOrWhiteSpace(venda.Vendedor))
        {
            erros[$"vendas[{indice}].vendedor"] = ["O vendedor é obrigatório."];
        }

        if (venda.Valor <= 0m)
        {
            erros[$"vendas[{indice}].valor"] = ["O valor da venda deve ser maior que zero."];
        }
    }

    return erros;
}

static Dictionary<string, string[]> ValidarMovimentacao(MovimentacaoEstoqueRequest request)
{
    var erros = new Dictionary<string, string[]>();

    if (request.CodigoProduto <= 0)
    {
        erros["codigoProduto"] = ["O código do produto deve ser maior que zero."];
    }

    if (request.Quantidade <= 0)
    {
        erros["quantidade"] = ["A quantidade deve ser maior que zero."];
    }

    if (string.IsNullOrWhiteSpace(request.Descricao))
    {
        erros["descricao"] = ["A descrição da movimentação é obrigatória."];
    }

    return erros;
}

static Dictionary<string, string[]> ValidarCalculoJuros(CalculoJurosRequest request)
{
    var erros = new Dictionary<string, string[]>();

    if (request.Valor <= 0m)
    {
        erros["valor"] = ["O valor deve ser maior que zero."];
    }

    if (request.DataVencimento == default)
    {
        erros["dataVencimento"] = ["A data de vencimento é obrigatória."];
    }

    return erros;
}
