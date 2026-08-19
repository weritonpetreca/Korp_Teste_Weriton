using Faturamento.Application.UseCases;
using Faturamento.Domain.Clients;
using Faturamento.Domain.Entities;
using Faturamento.Domain.Enums;
using Faturamento.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Moq;

namespace Faturamento.Application.Tests;

public class ImprimirNotaFiscalUseCaseTests
{
    private readonly Mock<INotaFiscalRepository> _repositoryMock;
    private readonly Mock<IEstoqueClient> _estoqueClientMock;
    private readonly Mock<ILogger<ImprimirNotaFiscalUseCase>> _loggerMock;
    private readonly ImprimirNotaFiscalUseCase _useCase;

    public ImprimirNotaFiscalUseCaseTests()
    {
        _repositoryMock = new Mock<INotaFiscalRepository>();
        _estoqueClientMock = new Mock<IEstoqueClient>();
        _loggerMock = new Mock<ILogger<ImprimirNotaFiscalUseCase>>();
        
        _useCase = new ImprimirNotaFiscalUseCase(
            _repositoryMock.Object, 
            _estoqueClientMock.Object, 
            _loggerMock.Object);
    }

    [Fact]
    public async Task Deve_Imprimir_Nota_Fiscal_E_Debitar_Estoque_Com_Sucesso()
    {
        // Arrange
        var nota = new NotaFiscal("123");
        nota.AdicionarItem("PROD-01", 2);

        _repositoryMock.Setup(r => r.ObterPorNumeroAsync("123")).ReturnsAsync(nota);

        // Act
        await _useCase.ExecutarAsync("123");

        // Assert
        Assert.Equal(StatusNota.Fechada, nota.Status);
        _estoqueClientMock.Verify(e => e.DebitarEstoqueAsync("PROD-01", 2), Times.Once);
        _repositoryMock.Verify(r => r.SalvarAsync(nota), Times.Once);
    }

    [Fact]
    public async Task Nao_Deve_Imprimir_Nota_Inexistente()
    {
        // Arrange
        _repositoryMock.Setup(r => r.ObterPorNumeroAsync("999")).ReturnsAsync((NotaFiscal?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _useCase.ExecutarAsync("999"));
    }

    [Fact]
    public async Task Nao_Deve_Imprimir_Nota_Com_Status_Diferente_De_Aberta()
    {
        // Arrange
        var nota = new NotaFiscal("123");
        nota.AdicionarItem("PROD-01", 2);
        nota.FecharNota(); // Força o status para Fechada

        _repositoryMock.Setup(r => r.ObterPorNumeroAsync("123")).ReturnsAsync(nota);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _useCase.ExecutarAsync("123"));
    }
}