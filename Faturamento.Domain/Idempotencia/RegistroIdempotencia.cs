namespace Faturamento.Domain.Idempotencia;

public class RegistroIdempotencia
{
    public string Chave { get; private set; }
    public int StatusCode { get; private set; }
    public string RespostaJson { get; private set; }
    public long DataExpiracaoTtl { get; private set; }

    public RegistroIdempotencia(string chave, int statusCode, string respostaJson)
    {
        Chave = !string.IsNullOrWhiteSpace(chave) ? chave : throw new ArgumentException("A chave de idempotência não pode ser vazia.");
        StatusCode = statusCode;
        RespostaJson = respostaJson;
        
        // TTL de 24 horas em Unix Epoch Time (Segundos) para a AWS limpar automaticamente
        DataExpiracaoTtl = DateTimeOffset.UtcNow.AddHours(24).ToUnixTimeSeconds();
    }

    public RegistroIdempotencia(string chave, int statusCode, string respostaJson, long dataExpiracaoTtl)
    {
        Chave = chave;
        StatusCode = statusCode;
        RespostaJson = respostaJson;
        DataExpiracaoTtl = dataExpiracaoTtl;
    }
}