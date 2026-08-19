using Estoque.Application.DTOs;
using Estoque.Domain.Repositories;
using FluentValidation;

namespace Estoque.Application.UseCases;

public class CreditarEstoqueUseCase(
    IProdutoRepository repository,
    IValidator<CreditarEstoqueRequest> validator)
{
    public async Task ExecutarAsync(string codigoProduto, CreditarEstoqueRequest request)
    {
        // 1. Validação imediata na borda (Fail-Fast)
        await validator.ValidateAndThrowAsync(request);

        var produto = await repository.ObterPorCodigoAsync(codigoProduto)
                      ?? throw new KeyNotFoundException($"O produto com o código '{codigoProduto}' não foi encontrado.");

        // Aciona a regra de domínio (soma o saldo e incrementa a Version para o Optimistic Locking)
        produto.CreditarEstoque(request.Quantidade);

        await repository.AtualizarAsync(produto);
    }
}