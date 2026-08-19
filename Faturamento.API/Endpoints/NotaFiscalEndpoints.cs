using Faturamento.API.Filters;
using Faturamento.Application.DTOs;
using Faturamento.Application.UseCases;

namespace Faturamento.API.Endpoints;

public static class NotaFiscalEndpoints
{
    public static void MapNotaFiscalEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/notas")
                          .WithTags("Notas Fiscais");

        // Endpoint para Criar Nota Fiscal (Status Inicial: Aberta)
        group.MapPost("/", async (CriarNotaFiscalRequest request, CriarNotaFiscalUseCase useCase) =>
        {
            var numeroNota = await useCase.ExecutarAsync(request);
            
            // Retorna 201 Created indicando o recurso criado e o número gerado
            return Results.Created($"/api/notas/{numeroNota}", new { Numero = numeroNota, Status = "Aberta" });
        })
        .WithName("CriarNotaFiscal")
        .AddEndpointFilter<IdempotencyFilter>()
        .Produces(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest);

        // Endpoint para Imprimir / Fechar Nota Fiscal (Debita o estoque)
        group.MapPost("/{numero}/imprimir", async (string numero, ImprimirNotaFiscalUseCase useCase) =>
        {
            await useCase.ExecutarAsync(numero);
            
            // Retorna 200 OK informando que a impressão e baixa de estoque concluíram com sucesso
            return Results.Ok(new { Mensagem = $"Nota fiscal {numero} impressa e fechada com sucesso." });
        })
        .WithName("ImprimirNotaFiscal")
        .AddEndpointFilter<IdempotencyFilter>()
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }
}