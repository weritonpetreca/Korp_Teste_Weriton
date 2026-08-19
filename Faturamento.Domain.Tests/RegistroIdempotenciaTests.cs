using Faturamento.Domain.Idempotencia;

namespace Faturamento.Domain.Tests;

public class RegistroIdempotenciaTests
{
    [Fact]
    public void Deve_Criar_Registro_Idempotencia_Com_Sucesso_E_Ttl_Valido()
    {
        // Arrange
        var chave = "test-uuid-key-123";
        var statusCode = 200;
        var respostaJson = "{\"mensagem\":\"sucesso\"}";

        // Act
        var registro = new RegistroIdempotencia(chave, statusCode, respostaJson);

        // Assert
        Assert.Equal(chave, registro.Chave);
        Assert.Equal(statusCode, registro.StatusCode);
        Assert.Equal(respostaJson, registro.RespostaJson);
        
        // Verifies that the TTL is set to roughly 24 hours from now (in Unix Epoch seconds)
        var expectedMinTtl = DateTimeOffset.UtcNow.AddHours(23.9).ToUnixTimeSeconds();
        var expectedMaxTtl = DateTimeOffset.UtcNow.AddHours(24.1).ToUnixTimeSeconds();
        Assert.InRange(registro.DataExpiracaoTtl, expectedMinTtl, expectedMaxTtl);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Nao_Deve_Criar_Registro_Com_Chave_Vazia(string chaveInvalida)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new RegistroIdempotencia(chaveInvalida, 200, "{}"));
    }
}