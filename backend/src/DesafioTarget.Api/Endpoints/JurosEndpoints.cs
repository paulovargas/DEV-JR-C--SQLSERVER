using DesafioTarget.Api.Models.Juros;
using DesafioTarget.Api.Services.Interfaces;

namespace DesafioTarget.Api.Endpoints;

public static class JurosEndpoints
{
    public static IEndpointRouteBuilder MapearJuros(this IEndpointRouteBuilder app)
    {
        var juros = app.MapGroup("/api/juros");

        juros.MapPost("/calcular", (
            CalculoJurosRequest request,
            ICalculadoraJuros calculadora,
            TimeProvider relogio) =>
        {
            var hoje = DateOnly.FromDateTime(relogio.GetLocalNow().DateTime);
            return Results.Ok(calculadora.Calcular(request.Valor, request.DataVencimento, hoje));
        });

        return app;
    }
}
