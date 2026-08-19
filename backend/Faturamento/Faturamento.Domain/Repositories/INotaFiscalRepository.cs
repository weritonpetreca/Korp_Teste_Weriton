using Faturamento.Domain.Entities;

namespace Faturamento.Domain.Repositories;

public interface INotaFiscalRepository 
{
    Task SalvarAsync(NotaFiscal notaFiscal);
    Task<NotaFiscal?> ObterPorNumeroAsync(string numero);
    Task<IEnumerable<NotaFiscal>> ObterTodosAsync();
}