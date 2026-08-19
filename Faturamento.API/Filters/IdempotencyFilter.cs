using System.Text.Json;
using Faturamento.Domain.Idempotencia;
using Faturamento.Domain.Repositories;

namespace Faturamento.API.Filters;

public class IdempotencyFilter(IIdempotenciaRepository idempotenciaRepository) : IEndpointFilter
{
    private readonly IIdempotenciaRepository _repository = idempotenciaRepository;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        
        // 1. OBRIGATORIEDADE DO HEADER: Segurança na borda
        if (!httpContext.Request.Headers.TryGetValue("X-Idempotency-Key", out var headerChave) 
            || string.IsNullOrWhiteSpace(headerChave))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Cabeçalho Obrigatório Ausente",
                detail: "O cabeçalho 'X-Idempotency-Key' é obrigatório para operações de mutação de faturamento."
            );
        }

        var chaveIdempotencia = headerChave.ToString();

        // 2. CURTO-CIRCUITO: Se já processou, devolve o cache armazenado
        var registroExistente = await _repository.ObterPorChaveAsync(chaveIdempotencia);
        if (registroExistente != null)
        {
            return Results.Json(
                data: string.IsNullOrWhiteSpace(registroExistente.RespostaJson) ? null : JsonSerializer.Deserialize<object>(registroExistente.RespostaJson, _jsonOptions),
                statusCode: registroExistente.StatusCode
            );
        }

        // 3. FLUXO NORMAL: Prossegue para o Caso de Uso
        var result = await next(context);

        // 4. CACHE DE SUCESSO: Se gerou status 2xx, grava a resposta no banco
        if (result is IStatusCodeHttpResult statusCodeResult && 
            statusCodeResult.StatusCode >= 200 && statusCodeResult.StatusCode < 300)
        {
            string respostaJson = string.Empty;
            if (result is IValueHttpResult valueHttpResult && valueHttpResult.Value != null)
            {
                respostaJson = JsonSerializer.Serialize(valueHttpResult.Value, _jsonOptions);
            }

            var novoRegistro = new RegistroIdempotencia(
                chaveIdempotencia,
                statusCodeResult.StatusCode ?? 200,
                respostaJson
            );

            await _repository.SalvarAsync(novoRegistro);
        }

        return result;
    }
}