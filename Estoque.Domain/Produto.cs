namespace Estoque.Domain;

public class Produto
{
    public string Codigo { get; private set; }
    public string Descricao { get; private set; }
    public int Saldo { get; private set; }
    public int Version { get; private set; } = 1;
    public string DataCriacao { get; private set; }
    public string DataAtualizacao { get; private set; }

    // ==========================================
    // CONSTRUTOR PRINCIPAL (Novo Produto) - Com Guard Clauses de Domínio
    // ==========================================
    public Produto(string codigo, string descricao, int saldo)
    {
        Codigo = !string.IsNullOrWhiteSpace(codigo) ? codigo : throw new ArgumentException("Código inválido.");
        Descricao = !string.IsNullOrWhiteSpace(descricao) ? descricao : throw new ArgumentException("Descrição inválida.");
        Saldo = saldo >= 0 ? saldo : throw new ArgumentException("Saldo não pode ser negativo.");
        
        var agora = DateTime.UtcNow.ToString("o");
        DataCriacao = agora;
        DataAtualizacao = agora;
    }

    // ==========================================
    // CONSTRUTOR DE RECONSTITUIÇÃO (HYDRATION)
    // ==========================================
    public Produto(string codigo, string descricao, int saldo, int version, string dataCriacao, string dataAtualizacao) 
        : this(codigo, descricao, saldo)
    {
        Version = version >= 1 ? version : throw new ArgumentException("Versão inválida.");
        DataCriacao = !string.IsNullOrWhiteSpace(dataCriacao) ? dataCriacao : throw new ArgumentException("Data de criação inválida.");
        DataAtualizacao = !string.IsNullOrWhiteSpace(dataAtualizacao) ? dataAtualizacao : throw new ArgumentException("Data de atualização inválida.");
    }

    public void AtualizarDescricao(string novaDescricao)
    {
        if (string.IsNullOrWhiteSpace(novaDescricao)) throw new ArgumentException("Descrição inválida.");
        
        Descricao = novaDescricao;
        RegistrarModificacao();
    }

    public void DebitarEstoque(int quantidade)
    {
        if (quantidade <= 0) throw new ArgumentException("Débito deve ser > 0.");
        if (Saldo < quantidade) throw new InvalidOperationException("Saldo insuficiente.");

        Saldo -= quantidade;
        RegistrarModificacao();
    }

    public void CreditarEstoque(int quantidade)
    {
        if (quantidade <= 0) throw new ArgumentException("Crédito deve ser > 0.");
        
        Saldo += quantidade;
        RegistrarModificacao();
    }

    private void RegistrarModificacao()
    {
        Version++;
        DataAtualizacao = DateTime.UtcNow.ToString("o");
    }
}