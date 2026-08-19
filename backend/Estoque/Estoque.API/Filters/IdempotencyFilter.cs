using System.Text.Json;
using Estoque.Domain.Idempotencia;
using Estoque.Domain.Repositories;

namespace Estoque.API.Filters;

public class IdempotencyFilter(IIdempotenciaRepository idempotenciaRepository) : IEndpointFilter
{
    private readonly IIdempotenciaRepository _repository = idempotenciaRepository;

    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        
        // 1. OBRIGATORIEDADE: Se a chave não existir ou for vazia, barramos imediatamente.
        if (!httpContext.Request.Headers.TryGetValue("X-Idempotency-Key", out var headerChave) 
            || string.IsNullOrWhiteSpace(headerChave))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Cabeçalho Obrigatório Ausente",
                detail: "O cabeçalho 'X-Idempotency-Key' é obrigatório para operações de mutação."
            );
        }

        var chaveIdempotencia = headerChave.ToString();

        // 2. VERIFICAÇÃO NO BANCO: Procura se a chave já foi processada.
        var registroExistente = await _repository.ObterPorChaveAsync(chaveIdempotencia);
        if (registroExistente != null)
        {
            // JÁ PROCESSAMOS! Curto-circuito: devolvemos o cache sem chamar o banco de Produtos.
            return Results.Json(
                data: string.IsNullOrWhiteSpace(registroExistente.RespostaJson) ? null : JsonSerializer.Deserialize<object>(registroExistente.RespostaJson, _jsonOptions),
                statusCode: registroExistente.StatusCode
            );
        }

        // 3. EXECUÇÃO NORMAL: Passa a requisição para frente (Caso de Uso -> DynamoDB)
        var result = await next(context);

        // 4. PERSISTÊNCIA DA CHAVE: Se deu certo (Status 2xx), gravamos o resultado para o futuro.
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