using DesafioTarget.Api.Models.Juros;

namespace DesafioTarget.Api.Services.Interfaces;

public interface ICalculadoraJuros
{
    CalculoJurosResponse Calcular(decimal valor, DateOnly dataVencimento, DateOnly dataCalculo);
}
