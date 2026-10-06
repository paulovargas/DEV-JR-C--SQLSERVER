using DesafioTarget.Api.Services;
using Microsoft.AspNetCore.Diagnostics;

namespace DesafioTarget.Api.Errors;

public sealed class TratadorExcecoes(ILogger<TratadorExcecoes> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext contexto, Exception excecao, CancellationToken cancellationToken)
    {
        IResult resposta;
        if (excecao is CalculoInvalidoException calculoInvalido)
        {
            resposta = RespostasErro.Validacao(contexto, calculoInvalido.Erros);
        }
        else if (excecao is BadHttpRequestException requisicaoInvalida)
        {
            resposta = RespostasErro.Problema(contexto, requisicaoInvalida.StatusCode);
        }
        else
        {
            logger.LogError(excecao,
                "Falha inesperada na requisição {Metodo} {Caminho}. TraceId: {TraceId}",
                contexto.Request.Method, contexto.Request.Path, contexto.TraceIdentifier);
            resposta = RespostasErro.Problema(contexto, StatusCodes.Status500InternalServerError);
        }

        await resposta.ExecuteAsync(contexto);
        return true;
    }
}
