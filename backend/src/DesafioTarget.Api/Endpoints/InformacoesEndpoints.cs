namespace DesafioTarget.Api.Endpoints;

public static class InformacoesEndpoints
{
    public static IEndpointRouteBuilder MapearInformacoes(this IEndpointRouteBuilder app)
    {
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

        return app;
    }
}
