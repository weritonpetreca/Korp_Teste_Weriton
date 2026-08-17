using Amazon.DynamoDBv2.Model;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Estoque.API.Handlers;

// Herdamos a interface oficial do .NET 8 para tratamento de exceções
// Injetamos o ILogger para garantir a Observabilidade (CloudWatch)
public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, 
        Exception exception, 
        CancellationToken cancellationToken)
    {
        // 1. OBSERVABILIDADE: Gravamos o erro original e assustador nos logs para o time de plantão
        logger.LogError(exception, "Exceção capturada globalmente: {Message}", exception.Message);

        // 2. PADRONIZAÇÃO: Definimos os status HTTP de acordo com o tipo de erro
        var (statusCode, title, detail) = exception switch
        {
            // Erro 400: Dados inválidos (Nosso FluentValidation)
            ValidationException => (StatusCodes.Status400BadRequest, "Erro de Validação", "Os dados enviados na requisição são inválidos."),

            // Erro 400: Regra de Negócio violada (Ex: Saldo insuficiente no Domínio)
            InvalidOperationException => (StatusCodes.Status400BadRequest, "Erro de Regra de Negócio", exception.Message),
            
            // Erro 404: Não encontrado (Quando o produto não existe no banco)
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Recurso não encontrado", exception.Message),
            
            // Erro 409: Conflito (Optimistic Locking - Alguém mexeu no produto ao mesmo tempo)
            ConditionalCheckFailedException => (StatusCodes.Status409Conflict, "Conflito de Concorrência", "O registro foi alterado por outro processo. Recarregue os dados e tente novamente."),
            
            // Erro 500: Erro não mapeado/inesperado (Falha grave, banco fora do ar, etc.)
            _ => (StatusCodes.Status500InternalServerError, "Erro Interno do Servidor", "Ocorreu um erro inesperado. Nossa equipe técnica já foi notificada.")
        };

        // 3. RFC 7807: Criação do objeto padronizado ProblemDetails
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path // Mostra qual rota deu o erro
        };

        // 4. BÔNUS: Se for erro de validação, anexamos a lista de erros exata para o Front-end
        if (exception is ValidationException validationException)
        {
            problemDetails.Extensions["erros"] = validationException.Errors
                .Select(e => new { Campo = e.PropertyName, Mensagem = e.ErrorMessage });
        }

        // 5. RESPOSTA: Configura o cabeçalho e escreve o JSON padronizado de volta para o cliente
        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        // Retorna 'true' para avisar ao .NET que "o erro já foi tratado, não precisa derrubar a aplicação"
        return true;
    }
}