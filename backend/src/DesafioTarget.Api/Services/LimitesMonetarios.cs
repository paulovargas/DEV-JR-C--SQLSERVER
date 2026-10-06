namespace DesafioTarget.Api.Services;

public static class LimitesMonetarios
{
    // Parte inteira de decimal.MaxValue / 100: reserva duas casas para centavos.
    public const decimal ValorMaximo = 792281625142643375935439503m;
    public const string MensagemValorMaximo = "O valor excede o limite monetário suportado: 792281625142643375935439503.";
}
