namespace Estoque.Domain;

public class Produto(string codigo, string descricao, int saldo)
{
    public string Codigo { get; private set; } = codigo;
    public string Descricao { get; private set; } = descricao;
    public int Saldo { get; private set; } = saldo;
    public int Version { get; private set; } = 1;

    // Ações de Domínio (Gerenciamento de Estado e Versão para Optimistic Locking)

    // Construtor de Reconstituição (Hydration): Usado SOMENTE pelo Repositório para carregar do Banco com a versão real
    public Produto(string codigo, string descricao, int saldo, int version) : this(codigo, descricao, saldo)
    {
        Version = version;
    }

    public void AtualizarDescricao(string novaDescricao)
    {
        Descricao = novaDescricao;
        Version++;
    }

    public void DebitarEstoque(int quantidade)
    {
        Saldo -= quantidade;
        Version++;
    }

    public void CreditarEstoque(int quantidade)
    {
        Saldo += quantidade;
        Version++;
    }
}