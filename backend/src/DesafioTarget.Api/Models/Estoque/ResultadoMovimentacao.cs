namespace DesafioTarget.Api.Models.Estoque;

public sealed record ResultadoMovimentacao(
    StatusMovimentacao Status,
    MovimentacaoEstoque? Movimentacao,
    string? Erro,
    Dictionary<string, string[]>? ErrosValidacao = null);
