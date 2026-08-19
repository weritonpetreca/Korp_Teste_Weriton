using Faturamento.Domain.Clients;
using Faturamento.Domain.Enums;
using Faturamento.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Faturamento.Application.UseCases;

public class ImprimirNotaFiscalUseCase(
    INotaFiscalRepository notaRepository,
    IEstoqueClient estoqueClient,
    ILogger<ImprimirNotaFiscalUseCase> logger)
{
    public async Task ExecutarAsync(string numeroNota)
    {
        // 1. Busca a nota no banco
        var nota = await notaRepository.ObterPorNumeroAsync(numeroNota)
                   ?? throw new KeyNotFoundException($"Nota fiscal de número '{numeroNota}' não foi encontrada.");

        // 2. REGRA DO DESAFIO: Não permitir imprimir notas com status diferente de "Aberta"
        if (nota.Status != StatusNota.Aberta)
        {
            throw new InvalidOperationException("Apenas notas fiscais com status 'Aberta' podem ser impressas.");
        }

        // 3. Itera sobre os itens da nota e debita o estoque de cada um de forma síncrona/orquestrada
        foreach (var item in nota.Itens)
        {
            // Se o microsserviço de Estoque falhar ou estiver fora, o Polly entra em ação aqui
            await estoqueClient.DebitarEstoqueAsync(item.CodigoProduto, item.Quantidade);
        }

        // 4. REGRA DO DESAFIO: Atualiza o status da nota para Fechada após o sucesso no estoque
        nota.FecharNota();

        // 5. Persiste a nota atualizada no DynamoDB
        await notaRepository.SalvarAsync(nota);

        logger.LogInformation("Nota Fiscal {Numero} impressa e fechada com sucesso.", numeroNota);
    }
}