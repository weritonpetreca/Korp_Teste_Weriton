using Faturamento.Application.DTOs;
using Faturamento.Domain.Entities;
using Faturamento.Domain.Repositories;
using FluentValidation;

namespace Faturamento.Application.UseCases;

public class CriarNotaFiscalUseCase(
    INotaFiscalRepository repository,
    IValidator<CriarNotaFiscalRequest> validator)
{
    public async Task<string> ExecutarAsync(CriarNotaFiscalRequest request)
    {
        // 1. VALIDAÇÃO DE BORDA (Fail-Fast)
        // Se a lista estiver vazia ou um produto tiver qtd negativa, explode a ValidationException aqui (que vira HTTP 400).
        await validator.ValidateAndThrowAsync(request);

        // 2. REGRA DO DESAFIO: Numeração Sequencial
        // Em bancos relacionais usaríamos AUTO_INCREMENT ou Sequences. 
        // Em Serverless/NoSQL (DynamoDB), usamos contadores atômicos ou Time-based IDs. 
        // Para simplificar e garantir a ordem sem gargalos de concorrência na nuvem, usaremos os Ticks do sistema.
        string numeroSequencial = DateTime.UtcNow.Ticks.ToString();

        // 3. INSTANCIAÇÃO DO DOMÍNIO
        // A entidade aplica suas próprias Guard Clauses (Invariantes)
        var notaFiscal = new NotaFiscal(numeroSequencial);

        foreach (var item in request.Itens)
        {
            // O Domínio valida se a nota está aberta antes de aceitar itens
            notaFiscal.AdicionarItem(item.CodigoProduto, item.Quantidade);
        }

        // 4. PERSISTÊNCIA
        await repository.SalvarAsync(notaFiscal);

        // Retornamos o número gerado para o Front-end
        return notaFiscal.Numero;
    }
}