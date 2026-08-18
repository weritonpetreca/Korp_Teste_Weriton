using Estoque.API.Filters;
using Estoque.Application.DTOs;
using Estoque.Application.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace Estoque.API.Endpoints;

public static class ProdutoEndpoints
{
    public static void MapProdutoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/produtos")
                       .WithTags("Gerenciamento de Estoque")
                       .WithOpenApi();

        // ==========================================
        // 1. CADASTRAR PRODUTO (POST) - Atualizado para retornar o DTO completo
        // ==========================================
        group.MapPost("/", async (
            [FromBody] CadastrarProdutoRequest request,
            [FromServices] CadastrarProdutoUseCase useCase,
            [FromServices] ObterProdutoPorCodigoUseCase obterUseCase) =>
        {
            await useCase.ExecutarAsync(request);
            
            // Busca o produto recém-criado para devolver a resposta completa com versão e datas de auditoria
            var produtoCriadoDto = await obterUseCase.ExecutarAsync(request.Codigo);
            
            return Results.Created($"/api/produtos/{request.Codigo}", produtoCriadoDto);
        })
        .WithName("CadastrarProduto")
        .AddEndpointFilter<IdempotencyFilter>()
        .Produces<ProdutoResponseDto>(StatusCodes.Status201Created)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        // ==========================================
        // 2. CREDITAR ESTOQUE (PUT)
        // ==========================================
        group.MapPut("/{codigo}/creditar", async (
            string codigo,
            [FromBody] CreditarEstoqueRequest request,
            [FromServices] CreditarEstoqueUseCase useCase) =>
        {
            await useCase.ExecutarAsync(codigo, request);
            return Results.NoContent(); 
        })
        .WithName("CreditarEstoque")
        .AddEndpointFilter<IdempotencyFilter>()
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        // ==========================================
        // 3. DEBITAR ESTOQUE (PUT)
        // ==========================================
        group.MapPut("/{codigo}/debitar", async (
            string codigo,
            [FromBody] DebitarEstoqueRequest request,
            [FromServices] DebitarEstoqueUseCase useCase) =>
        {
            await useCase.ExecutarAsync(codigo, request);
            return Results.NoContent();
        })
        .WithName("DebitarEstoque")
        .AddEndpointFilter<IdempotencyFilter>()
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        // ==========================================
        // 4. ATUALIZAR DESCRIÇÃO (PUT)
        // ==========================================
        group.MapPut("/{codigo}/descricao", async (
            string codigo,
            [FromBody] AtualizarDescricaoRequest request,
            [FromServices] AtualizarDescricaoUseCase useCase) =>
        {
            await useCase.ExecutarAsync(codigo, request);
            return Results.NoContent();
        })
        .WithName("AtualizarDescricao")
        .AddEndpointFilter<IdempotencyFilter>()
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        // ==========================================
        // 5. OBTER PRODUTO POR CÓDIGO (GET)
        // ==========================================
        group.MapGet("/{codigo}", async (
            string codigo,
            [FromServices] ObterProdutoPorCodigoUseCase useCase) =>
        {
            var produtoDto = await useCase.ExecutarAsync(codigo);
            return Results.Ok(produtoDto);
        })
        .WithName("ObterProdutoPorCodigo")
        .Produces<ProdutoResponseDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
    }
}