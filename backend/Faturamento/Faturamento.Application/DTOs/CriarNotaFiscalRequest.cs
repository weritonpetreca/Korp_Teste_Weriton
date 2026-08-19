namespace Faturamento.Application.DTOs;

public record ItemNotaRequest(string CodigoProduto, int Quantidade);

public record CriarNotaFiscalRequest(List<ItemNotaRequest> Itens);