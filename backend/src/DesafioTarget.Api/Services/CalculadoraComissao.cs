using DesafioTarget.Api.Models;

namespace DesafioTarget.Api.Services;

public interface ICalculadoraComissao
{
    CalculoComissaoResponse Calcular(IEnumerable<Venda> vendas);
}

public sealed class CalculadoraComissao : ICalculadoraComissao
{
    public CalculoComissaoResponse Calcular(IEnumerable<Venda> vendas)
    {
        var listaVendas = vendas?.ToArray();
        var erros = ValidadorVendas.Validar(listaVendas);
        if (erros.Count > 0)
            throw new CalculoInvalidoException(erros);

        var vendasCalculadas = listaVendas!.Select(venda => new
        {
            Vendedor = venda.Vendedor!.Trim(),
            venda.Valor,
            Comissao = venda.Valor * ObterTaxa(venda.Valor)
        });

        var resumos = vendasCalculadas
            .GroupBy(venda => venda.Vendedor, StringComparer.OrdinalIgnoreCase)
            .Select(grupo => new ResumoComissao(
                grupo.Key,
                grupo.Count(),
                Arredondar(grupo.Sum(venda => venda.Valor)),
                Arredondar(grupo.Sum(venda => venda.Comissao))))
            .ToArray();

        return new CalculoComissaoResponse(
            resumos,
            Arredondar(resumos.Sum(resumo => resumo.ComissaoTotal)));
    }

    public static decimal ObterTaxa(decimal valor)
    {
        if (valor < 100m)
        {
            return 0m;
        }

        return valor < 500m ? 0.01m : 0.05m;
    }

    private static decimal Arredondar(decimal valor) =>
        Math.Round(valor, 2, MidpointRounding.AwayFromZero);
}
