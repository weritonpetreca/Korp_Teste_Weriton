using System.Net.Http.Json;
using Faturamento.Domain.Clients;
using Microsoft.Extensions.Logging;

namespace Faturamento.Infrastructure.Clients;

public class EstoqueClient(HttpClient httpClient, ILogger<EstoqueClient> logger) : IEstoqueClient
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<EstoqueClient> _logger = logger;

    public async Task DebitarEstoqueAsync(string codigoProduto, int quantidade)
    {
        var requestPayload = new { Quantidade = quantidade };
        _logger.LogInformation("Enviando requisição para debitar estoque do produto {Codigo} (Qtd: {Qtd})", codigoProduto, quantidade);

        // 1. Usamos HttpRequestMessage para controlar exatamente o Método (PUT) e os Cabeçalhos
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/produtos/{codigoProduto}/debitar")
        {
            Content = JsonContent.Create(requestPayload)
        };
        
        // 2. Segurança Shift-Left: O Estoque exige a chave de idempotência.
        // Geramos um UUID único para esta tentativa. Se o Polly fizer o retry, 
        // ele reenviará a mesma requisição (com a mesma chave), protegendo o Estoque!
        request.Headers.Add("X-Idempotency-Key", Guid.NewGuid().ToString());

        // Dispara o PUT para o microsserviço de Estoque
        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Falha ao debitar estoque. Status: {Status}, Detalhes: {Details}", response.StatusCode, errorContent);
            
            // Lança uma exceção para que o Polly interprete se deve tentar novamente (Retry) ou abrir o Circuito
            throw new HttpRequestException($"Erro no Serviço de Estoque ao debitar produto {codigoProduto}. Status: {response.StatusCode}");
        }
    }
}