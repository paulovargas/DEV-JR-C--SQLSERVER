using System.Text.Json;
using System.Text.Json.Serialization;
using DesafioTarget.Api.Data;
using DesafioTarget.Api.Errors;
using DesafioTarget.Api.Models;
using DesafioTarget.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<TratadorExcecoes>();
builder.Services.Configure<RouteHandlerOptions>(opcoes => opcoes.ThrowOnBadRequest = true);

builder.Services.ConfigureHttpJsonOptions(opcoes =>
{
    opcoes.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    opcoes.SerializerOptions.Converters.Add(
        new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
});

builder.Services.AddSingleton<ICalculadoraComissao, CalculadoraComissao>();
builder.Services.AddSingleton<ICalculadoraJuros, CalculadoraJuros>();
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
var connectionString = builder.Configuration.GetConnectionString("SqlServer")
    ?? throw new InvalidOperationException("A string de conexão SqlServer não foi configurada.");
builder.Services.AddDbContext<DesafioTargetDbContext>(opcoes => opcoes.UseSqlServer(connectionString));
builder.Services.AddScoped<IEstoqueService, EstoqueService>();

var app = builder.Build();
await BancoDadosInicializador.InicializarAsync(app.Services);

app.UseExceptionHandler();
app.UseStatusCodePages(async contexto =>
    await RespostasErro.Problema(contexto.HttpContext, contexto.HttpContext.Response.StatusCode)
        .ExecuteAsync(contexto.HttpContext));

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
    ICalculadoraComissao calculadora) => Results.Ok(calculadora.Calcular(request.Vendas!)));

var produtos = app.MapGroup("/api/produtos");

produtos.MapGet("/", async (IEstoqueService estoque, CancellationToken cancellationToken) =>
    Results.Ok(await estoque.ListarProdutosAsync(cancellationToken)));

produtos.MapGet("/{codigoProduto:int}", async (int codigoProduto, IEstoqueService estoque, HttpContext contexto, CancellationToken cancellationToken) =>
{
    var produto = await estoque.ObterProdutoAsync(codigoProduto, cancellationToken);
    return produto is null
        ? RespostasErro.Problema(contexto, StatusCodes.Status404NotFound, $"Produto de código {codigoProduto} não encontrado.")
        : Results.Ok(produto);
});

var movimentacoes = app.MapGroup("/api/movimentacoes");

movimentacoes.MapGet("/", async (IEstoqueService estoque, CancellationToken cancellationToken) =>
    Results.Ok(await estoque.ListarMovimentacoesAsync(cancellationToken)));

movimentacoes.MapGet("/{id:long}", async (long id, IEstoqueService estoque, HttpContext contexto, CancellationToken cancellationToken) =>
{
    var movimentacao = await estoque.ObterMovimentacaoAsync(id, cancellationToken);
    return movimentacao is null
        ? RespostasErro.Problema(contexto, StatusCodes.Status404NotFound, $"Movimentação {id} não encontrada.")
        : Results.Ok(movimentacao);
});

movimentacoes.MapPost("/", async (MovimentacaoEstoqueRequest request, IEstoqueService estoque, HttpContext contexto, CancellationToken cancellationToken) =>
{
    var resultado = await estoque.MovimentarAsync(request, cancellationToken);

    return resultado.Status switch
    {
        StatusMovimentacao.DadosInvalidos => RespostasErro.Validacao(contexto, resultado.ErrosValidacao!),
        StatusMovimentacao.Sucesso => Results.Created(
            $"/api/movimentacoes/{resultado.Movimentacao!.Id}",
            resultado.Movimentacao),
        StatusMovimentacao.ProdutoNaoEncontrado => RespostasErro.Problema(contexto, StatusCodes.Status404NotFound, resultado.Erro),
        StatusMovimentacao.EstoqueInsuficiente => RespostasErro.Problema(contexto, StatusCodes.Status409Conflict, resultado.Erro),
        StatusMovimentacao.LimiteDeEstoqueExcedido => RespostasErro.Problema(contexto, StatusCodes.Status400BadRequest, resultado.Erro),
        _ => throw new InvalidOperationException("O serviço retornou um status de movimentação desconhecido.")
    };
});

var juros = app.MapGroup("/api/juros");

juros.MapPost("/calcular", (
    CalculoJurosRequest request,
    ICalculadoraJuros calculadora,
    TimeProvider relogio) =>
{
    var hoje = DateOnly.FromDateTime(relogio.GetLocalNow().DateTime);
    return Results.Ok(calculadora.Calcular(request.Valor, request.DataVencimento, hoje));
});

app.Run();

public partial class Program { }
