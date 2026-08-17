using Estoque.Application.DTOs;
using Estoque.Domain.Repositories;
using FluentValidation;

namespace Estoque.Application.UseCases;

public class AtualizarDescricaoUseCase(
    IProdutoRepository repository,
    IValidator<AtualizarDescricaoRequest> validator)
{
    public async Task ExecutarAsync(string codigoProduto, AtualizarDescricaoRequest request)
    {
        // 1. Validação imediata na borda (Fail-Fast)
        await validator.ValidateAndThrowAsync(request);

        var produto = await repository.ObterPorCodigoAsync(codigoProduto)
                      ?? throw new KeyNotFoundException($"O produto com o código '{codigoProduto}' não foi encontrado.");

        // Altera a descrição e incrementa a versão
        produto.AtualizarDescricao(request.NovaDescricao);

        await repository.AtualizarAsync(produto);
    }
}