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

public class AtualizarDescricaoUseCaseTests
{
    private readonly Mock<IProdutoRepository> _repositoryMock;
    private readonly Mock<IValidator<AtualizarDescricaoRequest>> _validatorMock;
    private readonly AtualizarDescricaoUseCase _useCase;

    public AtualizarDescricaoUseCaseTests()
    {
        _repositoryMock = new Mock<IProdutoRepository>();
        _validatorMock = new Mock<IValidator<AtualizarDescricaoRequest>>();
        
        _useCase = new AtualizarDescricaoUseCase(_repositoryMock.Object, _validatorMock.Object);
    }

    [Fact]
    public async Task Deve_Atualizar_Descricao_Com_Sucesso_Quando_Produto_Existir()
    {
        // Arrange
        var codigo = "PROD-001";
        var produtoExistente = new Produto(codigo, "Teclado Antigo", 10);
        var request = new AtualizarDescricaoRequest("Teclado Mecânico RGB");

        _repositoryMock.Setup(r => r.ObterPorCodigoAsync(codigo))
            .ReturnsAsync(produtoExistente);

        // Act
        await _useCase.ExecutarAsync(codigo, request);

        // Assert
        Assert.Equal("Teclado Mecânico RGB", produtoExistente.Descricao);
        Assert.Equal(2, produtoExistente.Version); // Versão incrementada para o Optimistic Locking
        _repositoryMock.Verify(r => r.AtualizarAsync(produtoExistente), Times.Once);
    }

    [Fact]
    public async Task Deve_Lancar_Excecao_Quando_Produto_Nao_For_Encontrado()
    {
        // Arrange
        var codigo = "INEXISTENTE";
        var request = new AtualizarDescricaoRequest("Nova Descrição");

        _repositoryMock.Setup(r => r.ObterPorCodigoAsync(codigo))
            .ReturnsAsync((Produto?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => 
            _useCase.ExecutarAsync(codigo, request)
        );

        _repositoryMock.Verify(r => r.AtualizarAsync(It.IsAny<Produto>()), Times.Never);
    }
}