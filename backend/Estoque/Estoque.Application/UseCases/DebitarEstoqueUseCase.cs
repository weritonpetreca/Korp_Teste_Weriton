using FluentValidation;
using Estoque.Application.DTOs;
using Estoque.Domain.Repositories;

namespace Estoque.Application.UseCases;

// Injetamos tanto o Repositório quanto o Validador do DTO via Primary Constructor
public class DebitarEstoqueUseCase(
    IProdutoRepository repository,
    IValidator<DebitarEstoqueRequest> validator)
{
    public async Task ExecutarAsync(string codigoProduto, DebitarEstoqueRequest request)
    {
        // 1. Validação imediata na borda (Fail-Fast)
        // O ValidateAndThrowAsync valida o DTO e joga uma ValidationException automaticamente se houver erro
        await validator.ValidateAndThrowAsync(request);

        // 2. Busca o produto atual no banco de dados
        var produto = await repository.ObterPorCodigoAsync(codigoProduto)
                      ?? throw new KeyNotFoundException($"O produto com o código '{codigoProduto}' não foi encontrado.");

        // 3. Executa a ação de domínio (Altera o saldo e incrementa a Versão)
        produto.DebitarEstoque(request.Quantidade);

        // 4. Persiste no DynamoDB aplicando o controle de concorrência (Optimistic Locking)
        await repository.AtualizarAsync(produto);
    }
}