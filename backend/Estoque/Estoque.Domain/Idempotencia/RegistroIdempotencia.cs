namespace Estoque.Domain.Idempotencia;

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
        
        // AWS DynamoDB TTL: Exige que o tempo de expiração seja em Unix Epoch Time (Segundos).
        // Estamos configurando para a chave expirar automaticamente da tabela após 24 horas.
        DataExpiracaoTtl = DateTimeOffset.UtcNow.AddHours(24).ToUnixTimeSeconds();
    }

    // Construtor de reconstituição (Hydration) para quando lermos do banco
    public RegistroIdempotencia(string chave, int statusCode, string respostaJson, long dataExpiracaoTtl)
    {
        Chave = chave;
        StatusCode = statusCode;
        RespostaJson = respostaJson;
        DataExpiracaoTtl = dataExpiracaoTtl;
    }
}