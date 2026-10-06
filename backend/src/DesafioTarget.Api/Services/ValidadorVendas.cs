using DesafioTarget.Api.Models;

namespace DesafioTarget.Api.Services;

public static class ValidadorVendas
{
    public static Dictionary<string, string[]> Validar(IReadOnlyList<Venda>? vendas)
    {
        var erros = new Dictionary<string, string[]>();

        if (vendas is null)
        {
            erros["vendas"] = ["A lista de vendas é obrigatória."];
            return erros;
        }

        if (vendas.Count == 0)
        {
            erros["vendas"] = ["A lista de vendas deve conter pelo menos uma venda."];
            return erros;
        }

        var totalVendas = 0m;
        for (var indice = 0; indice < vendas.Count; indice++)
        {
            var venda = vendas[indice];
            if (venda is null)
            {
                erros[$"vendas[{indice}]"] = ["A venda não pode ser nula."];
                continue;
            }

            if (string.IsNullOrWhiteSpace(venda.Vendedor))
                erros[$"vendas[{indice}].vendedor"] = ["O vendedor é obrigatório."];

            if (venda.Valor <= 0m)
                erros[$"vendas[{indice}].valor"] = ["O valor da venda deve ser maior que zero."];
            else if (venda.Valor > LimitesMonetarios.ValorMaximo)
                erros[$"vendas[{indice}].valor"] = [LimitesMonetarios.MensagemValorMaximo];
            else if (venda.Valor > LimitesMonetarios.ValorMaximo - totalVendas)
                erros["vendas"] = ["A soma dos valores das vendas excede o limite monetário suportado."];
            else
                totalVendas += venda.Valor;
        }

        return erros;
    }
}
