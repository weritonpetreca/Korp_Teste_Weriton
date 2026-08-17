using Estoque.Application.DTOs;
using Estoque.Application.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace Estoque.API.Endpoints;

/// <summary>
/// Classe responsável por expor as rotas HTTP do domínio de Produtos utilizando Minimal APIs.
/// Funciona como um "Extension Method" do IEndpointRouteBuilder para manter o Program.cs limpo.
/// </summary>
public static class ProdutoEndpoints
{
    public static void MapProdutoEndpoints(this IEndpointRouteBuilder app)
    {
        // Agrupamos todas as rotas sob o mesmo prefixo para evitar repetição
        // O WithOpenApi() já deixa as rotas preparadas para o Swagger gerar a documentação automaticamente
        var group = app.MapGroup("/api/produtos")
                       .WithTags("Gerenciamento de Estoque")
                       .WithOpenApi();

        // ==========================================
        // 1. CADASTRAR PRODUTO (POST)
        // ==========================================
        group.MapPost("/", async (
            [FromBody] CadastrarProdutoRequest request,
            [FromServices] CadastrarProdutoUseCase useCase) =>
        {
            // O Caso de Uso orquestra a validação, a regra de negócio e o salvamento
           var codigoCriado = await useCase.ExecutarAsync(request);
            
            // Padrão REST: 201 Created indica que o recurso foi criado. 
            // Informamos também a rota teórica de onde ele pode ser lido depois.
            return Results.Created($"/api/produtos/{request.Codigo}", new { 
                Mensagem = "Produto cadastrado com sucesso.",
                Codigo = codigoCriado
            });
        })
        .WithName("CadastrarProduto")
        .Produces(StatusCodes.Status201Created)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest); // Documenta que pode retornar 400 se a validação falhar

        // ==========================================
        // 2. CREDITAR ESTOQUE (PUT)
        // ==========================================
        // Usamos PUT porque a operação (com Optimistic Locking) garante consistência, 
        // e estamos alterando o estado completo do estoque daquele produto.
        group.MapPut("/{codigo}/creditar", async (
            string codigo,
            [FromBody] CreditarEstoqueRequest request,
            [FromServices] CreditarEstoqueUseCase useCase) =>
        {
            await useCase.ExecutarAsync(codigo, request);
            
            // Padrão REST: 204 No Content. Operação bem-sucedida, mas não precisamos devolver o corpo todo do produto de volta.
            return Results.NoContent(); 
        })
        .WithName("CreditarEstoque")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status409Conflict); // Documenta que o DynamoDB pode rejeitar por concorrência

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