namespace Faturamento.Domain.Repositories;

using Faturamento.Domain.Idempotencia;

public interface IIdempotenciaRepository
{
    Task<RegistroIdempotencia?> ObterPorChaveAsync(string chave);
    Task SalvarAsync(RegistroIdempotencia registro);
}