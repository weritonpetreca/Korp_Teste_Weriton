using System.Threading.Tasks;
using FluentValidation;
using Estoque.Application.DTOs;
using Estoque.Domain;
using Estoque.Domain.Repositories;

namespace Estoque.Application.UseCases;

// Injetamos o Repositório e o Validador direto na assinatura da classe (Primary Constructor)
public class CadastrarProdutoUseCase(
    IProdutoRepository repository,
    IValidator<Produto> validator)
{
    public async Task ExecutarAsync(CadastrarProdutoRequest request)
    {
        // 1. Instancia o Domínio com os dados do DTO
        var produto = new Produto(request.Codigo, request.Descricao, request.Saldo);
        
        // 2. Valida utilizando o validador injetado via DI (Fail-Fast automático)
        await validator.ValidateAndThrowAsync(produto);

        // 3. Persiste no banco usando a dependência injetada
        await repository.SalvarAsync(produto);
    }
}