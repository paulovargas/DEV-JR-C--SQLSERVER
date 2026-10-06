using DesafioTarget.Api.Models;

namespace DesafioTarget.Api.Services;

public static class ValidadorMovimentacaoEstoque
{
    public const int LimiteDescricao = 500;

    public static Dictionary<string, string[]> Validar(MovimentacaoEstoqueRequest request)
    {
        var erros = new Dictionary<string, string[]>();

        if (request.Tipo is null)
            erros["tipo"] = ["O tipo da movimentação é obrigatório."];
        else if (!Enum.IsDefined(request.Tipo.Value))
            erros["tipo"] = ["O tipo da movimentação deve ser entrada ou saida."];

        if (request.CodigoProduto <= 0)
            erros["codigoProduto"] = ["O código do produto deve ser maior que zero."];

        if (request.Quantidade <= 0)
            erros["quantidade"] = ["A quantidade deve ser maior que zero."];

        if (string.IsNullOrWhiteSpace(request.Descricao))
            erros["descricao"] = ["A descrição da movimentação é obrigatória."];
        else if (request.Descricao.Trim().Length > LimiteDescricao)
            erros["descricao"] = [$"A descrição da movimentação deve ter no máximo {LimiteDescricao} caracteres."];

        return erros;
    }
}
