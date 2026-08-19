using Faturamento.Application.DTOs;
using Faturamento.Application.Validators;

namespace Faturamento.Application.Tests.Validators;

public class CriarNotaFiscalRequestValidatorTests
{
    private readonly CriarNotaFiscalRequestValidator _validator = new();

    [Fact]
    public void Deve_Passar_Quando_Request_For_Valido()
    {
        // Arrange
        var request = new CriarNotaFiscalRequest(
        [
            new ItemNotaRequest("PROD-001", 10)
        ]);

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Deve_Falhar_Quando_Codigo_Produto_For_Vazio(string codigoInvalido)
    {
        // Arrange
        var request = new CriarNotaFiscalRequest(
        [
            new ItemNotaRequest(codigoInvalido, 10)
        ]);

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("CodigoProduto"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Deve_Falhar_Quando_Quantidade_For_Zero_Ou_Negativa(int quantidadeInvalida)
    {
        // Arrange
        var request = new CriarNotaFiscalRequest(
        [
            new ItemNotaRequest("PROD-001", quantidadeInvalida)
        ]);

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("Quantidade"));
    }

    [Fact]
    public void Deve_Falhar_Quando_Lista_De_Itens_Estiver_Vazia()
    {
        // Arrange
        var request = new CriarNotaFiscalRequest([]);

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("Itens"));
    }
}