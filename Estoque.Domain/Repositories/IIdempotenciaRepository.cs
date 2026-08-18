using Estoque.Domain.Idempotencia;

namespace Estoque.Domain.Repositories;

public interface IIdempotenciaRepository
{
    // Verifica se a chave já existe no banco
    Task<RegistroIdempotencia?> ObterPorChaveAsync(string chave);
    
    // Salva o resultado do processamento
    Task SalvarAsync(RegistroIdempotencia registro);
}