using FluentValidation;
using Moq;
using Estoque.Application.DTOs;
using Estoque.Application.UseCases;
using Estoque.Domain;
using Estoque.Domain.Repositories;

namespace Estoque.Application.Tests;

public class DebitarEstoqueUseCaseTests
{
    private readonly Mock<IProdutoRepository> _repositoryMock;
    private readonly Mock<IValidator<DebitarEstoqueRequest>> _validatorMock;
    private readonly DebitarEstoqueUseCase _useCase;

    public DebitarEstoqueUseCaseTests()
    {
        _repositoryMock = new Mock<IProdutoRepository>();
        _validatorMock = new Mock<IValidator<DebitarEstoqueRequest>>();
        
        _useCase = new DebitarEstoqueUseCase(_repositoryMock.Object, _validatorMock.Object);
    }

    [Fact]
    public async Task Deve_Debitar_Estoque_Com_Sucesso_Quando_Produto_Existir()
    {
        // Arrange
        var codigo = "PROD-001";
        var produtoExistente = new Produto(codigo, "Teclado", 10);
        var request = new DebitarEstoqueRequest(3);

        _repositoryMock.Setup(r => r.ObterPorCodigoAsync(codigo))
            .ReturnsAsync(produtoExistente);

        // Act
        await _useCase.ExecutarAsync(codigo, request);

        // Assert
        Assert.Equal(7, produtoExistente.Saldo); // 10 - 3 = 7
        Assert.Equal(2, produtoExistente.Version); // Versão incrementada para o Optimistic Locking
        _repositoryMock.Verify(r => r.AtualizarAsync(produtoExistente), Times.Once);
    }

    [Fact]
    public async Task Deve_Lancar_Excecao_Quando_Produto_Nao_For_Encontrado()
    {
        // Arrange
        var codigo = "INEXISTENTE";
        var request = new DebitarEstoqueRequest(5);

        _repositoryMock.Setup(r => r.ObterPorCodigoAsync(codigo))
            .ReturnsAsync((Produto?)null); // Simula que o banco não achou

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => 
            _useCase.ExecutarAsync(codigo, request)
        );

        _repositoryMock.Verify(r => r.AtualizarAsync(It.IsAny<Produto>()), Times.Never);
    }
}