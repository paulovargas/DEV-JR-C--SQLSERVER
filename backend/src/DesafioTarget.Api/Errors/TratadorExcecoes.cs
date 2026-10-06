using DesafioTarget.Api.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Data.SqlClient;

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
        else if (EhDeadlockSqlServer(excecao))
        {
            logger.LogWarning(excecao,
                "Conflito de concorrência na requisição {Metodo} {Caminho}. TraceId: {TraceId}",
                contexto.Request.Method, contexto.Request.Path, contexto.TraceIdentifier);
            resposta = RespostasErro.Problema(contexto, StatusCodes.Status409Conflict,
                "Outra operação de estoque ocorreu ao mesmo tempo. Tente novamente.");
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

    private static bool EhDeadlockSqlServer(Exception excecao)
    {
        for (Exception? erro = excecao; erro is not null; erro = erro.InnerException)
        {
            if (erro is SqlException { Number: 1205 })
                return true;
        }

        return false;
    }
}
