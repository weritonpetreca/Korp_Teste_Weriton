using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Faturamento.API.Handlers;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, 
        Exception exception, 
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Exceção capturada no Faturamento: {Message}", exception.Message);

        var (statusCode, title, detail) = exception switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, "Erro de Validação", "Os dados enviados na requisição de faturamento são inválidos."),
            
            ArgumentException => (StatusCodes.Status400BadRequest, "Erro de Domínio", exception.Message),

            InvalidOperationException => (StatusCodes.Status400BadRequest, "Erro de Regra de Negócio", exception.Message),
            
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Recurso não encontrado", exception.Message),
            
            HttpRequestException => (StatusCodes.Status503ServiceUnavailable, "Serviço Indisponível", "Falha de comunicação com o microsserviço de Estoque. O sistema tentará recuperar-se em breve."),
            
            _ => (StatusCodes.Status500InternalServerError, "Erro Interno do Servidor", "Ocorreu um erro inesperado no processamento da nota fiscal.")
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        if (exception is ValidationException validationException)
        {
            problemDetails.Extensions["erros"] = validationException.Errors
                .Select(e => new { Campo = e.PropertyName, Mensagem = e.ErrorMessage });
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}