using System.Threading.Tasks;

namespace Estoque.Domain.Repositories;

// No C#, por convenção, toda Interface começa com a letra 'I' maiúscula.
public interface IProdutoRepository
{
    // Método para salvar um novo produto
    Task SalvarAsync(Produto produto);

    // Método para buscar um produto pelo seu código (Partition Key no DynamoDB)
    Task<Produto?> ObterPorCodigoAsync(string codigo);

    // Método para atualizar o produto (necessário para darmos baixa no estoque)
    Task AtualizarAsync(Produto produto);
}