namespace Estoque.Application.DTOs;

public record ProdutoResponseDto(
    string Codigo,
    string Descricao,
    int Saldo,
    int Version
);