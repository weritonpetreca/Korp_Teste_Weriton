using Estoque.Application.DTOs;
using Estoque.Domain.Repositories;
using FluentValidation;

namespace Estoque.Application.UseCases;

public class ObterProdutoPorCodigoUseCase(
    IProdutoRepository repository,
    IValidator<string> validator)
{
    private readonly IProdutoRepository _repository = repository;

    public async Task<ProdutoResponseDto> ExecutarAsync(string codigoProduto)
    {
        await validator.ValidateAndThrowAsync(codigoProduto);

        // 1. Busca o produto no banco usando o método que já existe no seu repositório
        var produto = await _repository.ObterPorCodigoAsync(codigoProduto)
                      ?? throw new KeyNotFoundException($"O produto com o código '{codigoProduto}' não foi encontrado.");

        // 2. Mapeia a Entidade de Domínio para o DTO de Resposta da Aplicação
        return new ProdutoResponseDto(
            produto.Codigo,
            produto.Descricao,
            produto.Saldo,
            produto.Version,
            produto.DataCriacao,
            produto.DataAtualizacao
        );
    }
}