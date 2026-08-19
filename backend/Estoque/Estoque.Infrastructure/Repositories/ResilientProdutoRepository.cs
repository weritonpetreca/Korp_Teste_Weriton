using Estoque.Domain;
using Estoque.Domain.Repositories;
using Estoque.Infrastructure.Resilience;
using Microsoft.Extensions.Logging;
using Polly;

namespace Estoque.Infrastructure.Repositories;

// 1. Assina o contrato IProdutoRepository (Para a aplicação, ele é um repositório normal)
// 2. Recebe no construtor primário o repositório REAL que de fato fala com o banco
public class ResilientProdutoRepository(
    IProdutoRepository innerRepository,
    ILogger<ResilientProdutoRepository> logger) : IProdutoRepository
{
    // Carrega a pipeline blindada que acabamos de criar
    private readonly ResiliencePipeline _pipeline = DynamoDbResiliencePipeline.GetPipeline(logger);

    public async Task SalvarAsync(Produto produto)
    {
        // Envolve a chamada real dentro da execução do Polly
        await _pipeline.ExecuteAsync(async token => 
        {
            await innerRepository.SalvarAsync(produto);
        });
    }

    public async Task<Produto?> ObterPorCodigoAsync(string codigo)
    {
        // Envolve a chamada real dentro da execução do Polly
        return await _pipeline.ExecuteAsync(async token => 
        {
            return await innerRepository.ObterPorCodigoAsync(codigo);
        });
    }

    public async Task AtualizarAsync(Produto produto)
    {
        // Envolve a chamada real dentro da execução do Polly
        await _pipeline.ExecuteAsync(async token => 
        {
            await innerRepository.AtualizarAsync(produto);
        });
    }

    public async Task<IEnumerable<Produto>> ObterTodosAsync()
    {
        // Envolve a chamada de listagem (Scan) na mesma blindagem de resiliência do Polly
        return await _pipeline.ExecuteAsync(async token => 
        {
            return await innerRepository.ObterTodosAsync();
        });
    }
}