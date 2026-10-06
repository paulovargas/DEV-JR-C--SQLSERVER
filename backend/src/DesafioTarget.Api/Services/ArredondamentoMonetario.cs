namespace DesafioTarget.Api.Services;

public static class ArredondamentoMonetario
{
    public static decimal Arredondar(decimal valor) =>
        Math.Round(valor, 2, MidpointRounding.AwayFromZero);
}
