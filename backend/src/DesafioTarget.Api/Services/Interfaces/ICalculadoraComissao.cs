using DesafioTarget.Api.Models.Comissoes;

namespace DesafioTarget.Api.Services.Interfaces;

public interface ICalculadoraComissao
{
    CalculoComissaoResponse Calcular(IEnumerable<Venda> vendas);
}
