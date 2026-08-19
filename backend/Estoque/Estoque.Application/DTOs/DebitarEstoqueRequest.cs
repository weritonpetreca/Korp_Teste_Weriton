namespace Estoque.Application.DTOs;

// Usamos um record imutável para transportar apenas a quantidade necessária para a baixa
public record DebitarEstoqueRequest(int Quantidade);