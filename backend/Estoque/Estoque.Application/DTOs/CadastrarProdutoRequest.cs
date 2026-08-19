namespace Estoque.Application.DTOs;

// No C#, o 'record' cria automaticamente os campos, construtor, getters e métodos de comparação por valor.
// Ele é imutável por padrão. Não precisamos escrever mais nenhuma linha.
public record CadastrarProdutoRequest(string Codigo, string Descricao, int Saldo);