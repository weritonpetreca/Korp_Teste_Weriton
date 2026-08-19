using Faturamento.Application.DTOs;
using Faturamento.Application.UseCases;
using Faturamento.Application.Validators;
using Faturamento.Domain.Entities;
using Faturamento.Domain.Repositories;
using Moq;

namespace Faturamento.Application.Tests;

public class CriarNotaFiscalUseCaseTests
{
    private readonly Mock<INotaFiscalRepository> _repositoryMock;
    private readonly CriarNotaFiscalRequestValidator _validator;
    private readonly CriarNotaFiscalUseCase _useCase;

    public CriarNotaFiscalUseCaseTests()
    {
        _repositoryMock = new Mock<INotaFiscalRepository>();
        _validator = new CriarNotaFiscalRequestValidator();
        _useCase = new CriarNotaFiscalUseCase(_repositoryMock.Object, _validator);
    }

    [Fact]
    public async Task Deve_Criar_Nota_Fiscal_Com_Sucesso_E_Chamar_Repositorio()
    {
        // Arrange
        var request = new CriarNotaFiscalRequest(
        [
            new ItemNotaRequest("PROD-01", 2)
        ]);

        // Act
        var numeroNota = await _useCase.ExecutarAsync(request);

        // Assert
        Assert.NotNull(numeroNota);
        _repositoryMock.Verify(r => r.SalvarAsync(It.IsAny<NotaFiscal>()), Times.Once);
    }

    [Fact]
    public async Task Nao_Deve_Salvar_Quando_Request_For_Invalido()
    {
        // Arrange (Lista de itens vazia)
        var request = new CriarNotaFiscalRequest([]);

        // Act & Assert
        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => _useCase.ExecutarAsync(request));
        _repositoryMock.Verify(r => r.SalvarAsync(It.IsAny<NotaFiscal>()), Times.Never);
    }
}