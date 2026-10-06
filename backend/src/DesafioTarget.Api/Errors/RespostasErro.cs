namespace DesafioTarget.Api.Errors;

public static class RespostasErro
{
    public static IResult Problema(HttpContext contexto, int status, string? detalhe = null) =>
        Results.Problem(
            statusCode: status,
            title: ObterTitulo(status),
            detail: detalhe ?? ObterDetalhe(status),
            instance: contexto.Request.Path,
            extensions: new Dictionary<string, object?> { ["traceId"] = contexto.TraceIdentifier });

    public static IResult Validacao(HttpContext contexto, IDictionary<string, string[]> erros) =>
        Results.ValidationProblem(
            erros,
            title: ObterTitulo(StatusCodes.Status400BadRequest),
            detail: "Corrija os campos indicados e tente novamente.",
            instance: contexto.Request.Path,
            extensions: new Dictionary<string, object?> { ["traceId"] = contexto.TraceIdentifier });

    private static string ObterTitulo(int status) => status switch
    {
        StatusCodes.Status400BadRequest => "Dados inválidos.",
        StatusCodes.Status404NotFound => "Recurso não encontrado.",
        StatusCodes.Status405MethodNotAllowed => "Método não permitido.",
        StatusCodes.Status409Conflict => "Conflito na operação.",
        StatusCodes.Status415UnsupportedMediaType => "Tipo de conteúdo não suportado.",
        StatusCodes.Status500InternalServerError => "Erro interno do servidor.",
        _ => "Não foi possível concluir a operação."
    };

    private static string ObterDetalhe(int status) => status switch
    {
        StatusCodes.Status400BadRequest => "O corpo ou os parâmetros da requisição são inválidos.",
        StatusCodes.Status404NotFound => "O recurso solicitado não foi encontrado.",
        StatusCodes.Status405MethodNotAllowed => "O método HTTP informado não é permitido para este recurso.",
        StatusCodes.Status409Conflict => "A operação não pode ser concluída no estado atual do recurso.",
        StatusCodes.Status415UnsupportedMediaType => "Envie o corpo da requisição como application/json.",
        _ => "Não foi possível concluir a operação. Tente novamente mais tarde."
    };
}
