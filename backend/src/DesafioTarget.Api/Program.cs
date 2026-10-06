using System.Text.Json;
using System.Text.Json.Serialization;
using DesafioTarget.Api.Data;
using DesafioTarget.Api.Endpoints;
using DesafioTarget.Api.Errors;
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

app.MapearInformacoes();
app.MapearComissoes();
app.MapearEstoque();
app.MapearJuros();

app.Run();

public partial class Program { }
