using Faturamento.Domain.Enums;

namespace Faturamento.Domain.Entities;

public class NotaFiscal
{
    public string Numero { get; private set; }
    public StatusNota Status { get; private set; }
    public List<ItemNota> Itens { get; private set; } = new();
    public string DataCriacao { get; private set; }

    // ==========================================
    // CONSTRUTOR PRINCIPAL (Nova Nota)
    // ==========================================
    public NotaFiscal(string numero)
    {
        Numero = !string.IsNullOrWhiteSpace(numero) ? numero : throw new ArgumentException("Número da nota é obrigatório.");
        
        // REGRA DE NEGÓCIO: Nasce sempre ABERTA.
        Status = StatusNota.Aberta; 
        
        DataCriacao = DateTime.UtcNow.ToString("o");
    }

    // ==========================================
    // CONSTRUTOR DE RECONSTITUIÇÃO (Lendo do Banco)
    // ==========================================
    public NotaFiscal(string numero, StatusNota status, List<ItemNota> itens, string dataCriacao)
    {
        Numero = numero;
        Status = status;
        Itens = itens ?? new List<ItemNota>();
        DataCriacao = dataCriacao;
    }

    // ==========================================
    // COMPORTAMENTOS DO DOMÍNIO
    // ==========================================
    public void AdicionarItem(string codigoProduto, int quantidade)
    {
        if (Status != StatusNota.Aberta)
        {
            throw new InvalidOperationException("Não é possível adicionar itens em uma nota que já foi fechada.");
        }

        Itens.Add(new ItemNota(codigoProduto, quantidade));
    }

    public void FecharNota()
    {
        // REGRA DE NEGÓCIO: Não pode fechar o que já não está aberto.
        if (Status != StatusNota.Aberta)
        {
            throw new InvalidOperationException("A nota já está fechada ou em um estado inválido para fechamento.");
        }
        
        if (Itens.Count == 0)
        {
            throw new InvalidOperationException("Não é possível imprimir/fechar uma nota sem itens.");
        }
        
        Status = StatusNota.Fechada;
    }
}