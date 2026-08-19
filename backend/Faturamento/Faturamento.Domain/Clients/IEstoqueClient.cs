namespace Faturamento.Domain.Clients;

public interface IEstoqueClient
{
    Task DebitarEstoqueAsync(string codigoProduto, int quantidade);
}