namespace Estoque.Domain;

public class Produto(string codigo, string descricao, int saldo)
{
    public string Codigo { get; private set; } = codigo;
    public string Descricao { get; private set; } = descricao;
    public int Saldo { get; private set; } = saldo;
    public int Version { get; private set; } = 1;

    // ==========================================
    // CONSTRUTOR DE RECONSTITUIÇÃO (HYDRATION)
    // ==========================================
    // Usado exclusivamente pelo Repositório para carregar o estado real do banco (incluindo a versão).
    public Produto(string codigo, string descricao, int saldo, int version) : this(codigo, descricao, saldo)
    {
        Version = version >= 1 ? version : throw new ArgumentException("Versão inválida.");
    }

    public void AtualizarDescricao(string novaDescricao)
    {
        if (string.IsNullOrWhiteSpace(novaDescricao)) throw new ArgumentException("Descrição inválida.");
        
        Descricao = novaDescricao;
        IncrementarVersao();
    }

    public void DebitarEstoque(int quantidade)
    {
        if (quantidade <= 0) throw new ArgumentException("Débito deve ser > 0.");
        if (Saldo < quantidade) throw new InvalidOperationException("Saldo insuficiente.");

        Saldo -= quantidade;
        IncrementarVersao();
    }

    public void CreditarEstoque(int quantidade)
    {
        if (quantidade <= 0) throw new ArgumentException("Crédito deve ser > 0.");
        
        Saldo += quantidade;
        IncrementarVersao();
    }

    private void IncrementarVersao() => Version++;
}