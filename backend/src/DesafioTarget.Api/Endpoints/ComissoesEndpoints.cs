using DesafioTarget.Api.Models.Comissoes;
using DesafioTarget.Api.Services.Interfaces;

namespace DesafioTarget.Api.Endpoints;

public static class ComissoesEndpoints
{
    public static IEndpointRouteBuilder MapearComissoes(this IEndpointRouteBuilder app)
    {
        var comissoes = app.MapGroup("/api/comissoes");

        comissoes.MapPost("/calcular", (
            CalculoComissaoRequest request,
            ICalculadoraComissao calculadora) => Results.Ok(calculadora.Calcular(request.Vendas!)));

        return app;
    }
}
