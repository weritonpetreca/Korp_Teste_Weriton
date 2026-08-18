using FluentValidation;
using Estoque.Application.DTOs;
using Estoque.Domain;
using Estoque.Domain.Repositories;

namespace Estoque.Application.UseCases;

public class CadastrarProdutoUseCase(
    IProdutoRepository repository,
    IValidator<Produto> validator)
{
    public async Task<string> ExecutarAsync(CadastrarProdutoRequest request)
    {
        // 1. Instancia a Entidade de Domínio (ela já carimba a DataCriacao e DataAtualizacao via construtor)
        var produto = new Produto(request.Codigo, request.Descricao, request.Saldo);
        
        // 2. Valida a entidade utilizando o validador do domínio (Fail-Fast)
        await validator.ValidateAndThrowAsync(produto);

        // 3. Persiste no repositório
        await repository.SalvarAsync(produto);

        return produto.Codigo;
    }
}