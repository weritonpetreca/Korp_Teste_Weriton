using System.Text.Json;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace Estoque.Infrastructure.Services;

public class GeminiAiService(
    HttpClient httpClient, 
    string apiKey,
    ILogger<GeminiAiService> logger)
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly string _apiKey = apiKey;
    private readonly ILogger<GeminiAiService> _logger = logger;

    public async Task<string> GerarInsightEstoqueAsync(string dadosEstoque)
    {
        _logger.LogInformation("Verificando chave do Gemini. A chave está vazia? {IsEmpty}. Tamanho: {Length}", 
        string.IsNullOrEmpty(_apiKey), _apiKey?.Length ?? 0);

        if (string.IsNullOrEmpty(_apiKey) || _apiKey == "SUA_CHAVE_REAL_DO_GEMINI_AQUI")
        {
            return "⚠️ AVISO: Chave da API do Gemini não configurada via User Secrets ou Variável de Ambiente.";
        }

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-3.6-flash:generateContent?key={_apiKey}";

        var payload = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = $"Atue como um consultor sênior de ERP e Supply Chain. Analise os seguintes dados do nosso estoque e forneça um breve insight executivo e estratégico (máximo de 3 linhas) em português: {dadosEstoque}" }
                    }
                }
            }
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync(url, payload);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Falha ao comunicar com a API do Gemini. Status: {Status}", response.StatusCode);
                return "Erro temporário ao consultar a Inteligência Artificial.";
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(jsonResponse);
            
            var text = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            return text ?? "Nenhum insight gerado.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro crítico ao processar requisição para a IA.");
            return "Erro interno ao processar inteligência artificial.";
        }
    }
}