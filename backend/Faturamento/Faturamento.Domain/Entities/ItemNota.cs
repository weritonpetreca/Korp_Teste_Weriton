namespace Faturamento.Domain.Entities;

public class ItemNota(string codigoProduto, int quantidade)
{
    public string CodigoProduto { get; private set; } = !string.IsNullOrWhiteSpace(codigoProduto) ? codigoProduto : throw new ArgumentException("Código do produto é obrigatório.");
    public int Quantidade { get; private set; } = quantidade > 0 ? quantidade : throw new ArgumentException("A quantidade do item deve ser maior que zero.");
}