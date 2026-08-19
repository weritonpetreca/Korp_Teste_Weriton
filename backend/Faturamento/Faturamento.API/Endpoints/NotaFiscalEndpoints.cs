using Faturamento.API.Filters;
using Faturamento.Application.DTOs;
using Faturamento.Application.UseCases;
using Faturamento.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Faturamento.API.Endpoints;

public static class NotaFiscalEndpoints
{
    public static void MapNotaFiscalEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/notas")
                          .WithTags("Notas Fiscais");

        // ==========================================
        // 0. LISTAR TODAS AS NOTAS FISCAIS (GET)
        // ==========================================
        group.MapGet("/", async ([FromServices] INotaFiscalRepository repository) =>
        {
            var notas = await repository.ObterTodosAsync();
            return Results.Ok(notas);
        })
        .WithName("ListarNotasFiscais")
        .Produces<IEnumerable<object>>(StatusCodes.Status200OK);

        // ==========================================
        // 1. CRIAR NOTA FISCAL (POST)
        // ==========================================
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

        // ==========================================
        // 2. IMPRIMIR / FECHAR NOTA FISCAL (POST)
        // ==========================================
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