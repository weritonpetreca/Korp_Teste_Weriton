using System.Collections.Generic;
using System.Threading.Tasks;
using FluentValidation;
using Moq;
using Xunit;
using Estoque.Application.DTOs;
using Estoque.Application.UseCases;
using Estoque.Domain;
using Estoque.Domain.Repositories;

namespace Estoque.Application.Tests;

public class CreditarEstoqueUseCaseTests
{
    private readonly Mock<IProdutoRepository> _repositoryMock;
    private readonly Mock<IValidator<CreditarEstoqueRequest>> _validatorMock;
    private readonly CreditarEstoqueUseCase _useCase;

    public CreditarEstoqueUseCaseTests()
    {
        _repositoryMock = new Mock<IProdutoRepository>();
        _validatorMock = new Mock<IValidator<CreditarEstoqueRequest>>();
        
        _useCase = new CreditarEstoqueUseCase(_repositoryMock.Object, _validatorMock.Object);
    }

    [Fact]
    public async Task Deve_Creditar_Estoque_Com_Sucesso_Quando_Produto_Existir()
    {
        // Arrange
        var codigo = "PROD-001";
        var produtoExistente = new Produto(codigo, "Teclado", 10);
        var request = new CreditarEstoqueRequest(5);

        _repositoryMock.Setup(r => r.ObterPorCodigoAsync(codigo))
            .ReturnsAsync(produtoExistente);

        // Act
        await _useCase.ExecutarAsync(codigo, request);

        // Assert
        Assert.Equal(15, produtoExistente.Saldo); // 10 + 5 = 15
        Assert.Equal(2, produtoExistente.Version); // Versão incrementada para o Optimistic Locking
        _repositoryMock.Verify(r => r.AtualizarAsync(produtoExistente), Times.Once);
    }

    [Fact]
    public async Task Deve_Lancar_Excecao_Quando_Produto_Nao_For_Encontrado()
    {
        // Arrange
        var codigo = "INEXISTENTE";
        var request = new CreditarEstoqueRequest(5);

        _repositoryMock.Setup(r => r.ObterPorCodigoAsync(codigo))
            .ReturnsAsync((Produto?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => 
            _useCase.ExecutarAsync(codigo, request)
        );

        _repositoryMock.Verify(r => r.AtualizarAsync(It.IsAny<Produto>()), Times.Never);
    }
}