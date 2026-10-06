namespace DesafioTarget.Api.Services;

public sealed class CalculoInvalidoException(Dictionary<string, string[]> erros)
    : Exception("Os dados informados não permitem realizar o cálculo.")
{
    public Dictionary<string, string[]> Erros { get; } = erros;
}
