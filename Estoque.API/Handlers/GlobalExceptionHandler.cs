using Amazon.DynamoDBv2.Model;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Estoque.API.Handlers;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, 
        Exception exception, 
        CancellationToken cancellationToken)
    {
        // 1. OBSERVABILIDADE: Logamos o erro para auditoria
        logger.LogError(exception, "Exceção capturada globalmente: {Message}", exception.Message);

        // 2. PADRONIZAÇÃO: Mapeamento de exceções para status HTTP
        var (statusCode, title, detail) = exception switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, "Erro de Validação", "Os dados enviados na requisição são inválidos."),
            
            // Adicionado: ArgumentException vindo das Guard Clauses do Domínio
            ArgumentException => (StatusCodes.Status400BadRequest, "Erro de Negócio", exception.Message),

            InvalidOperationException => (StatusCodes.Status400BadRequest, "Erro de Regra de Negócio", exception.Message),
            
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Recurso não encontrado", exception.Message),
            
            ConditionalCheckFailedException => (StatusCodes.Status409Conflict, "Conflito de Concorrência", "O registro foi alterado por outro processo."),
            
            _ => (StatusCodes.Status500InternalServerError, "Erro Interno do Servidor", "Ocorreu um erro inesperado.")
        };

        // 3. RFC 7807: ProblemDetails
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        // 4. Detalhes extras para erros de validação do FluentValidation
        if (exception is ValidationException validationException)
        {
            problemDetails.Extensions["erros"] = validationException.Errors
                .Select(e => new { Campo = e.PropertyName, Mensagem = e.ErrorMessage });
        }

        // 5. RESPOSTA
        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}