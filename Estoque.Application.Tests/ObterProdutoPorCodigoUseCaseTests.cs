using FluentValidation;
using Moq;
using Estoque.Application.DTOs;
using Estoque.Application.UseCases;
using Estoque.Application.Validators;
using Estoque.Domain;
using Estoque.Domain.Repositories;

namespace Estoque.Application.Tests;

public class ObterProdutoPorCodigoUseCaseTests
{
    private readonly Mock<IProdutoRepository> _repositoryMock;
    private readonly CodigoProdutoValidator _validator;
    private readonly ObterProdutoPorCodigoUseCase _useCase;

    public ObterProdutoPorCodigoUseCaseTests()
    {
        _repositoryMock = new Mock<IProdutoRepository>();
        _validator = new CodigoProdutoValidator(); // Usamos o validador real de string/código
        
        _useCase = new ObterProdutoPorCodigoUseCase(_repositoryMock.Object, _validator);
    }

    [Fact]
    public async Task Deve_Retornar_ProdutoDto_Com_Sucesso_Quando_Codigo_Existir()
    {
        // Arrange
        var codigo = "PROD-001";
        var agora = DateTime.UtcNow.ToString("o");
        var produtoExistente = new Produto(codigo, "Teclado Mecânico", 15, version: 2, dataCriacao: agora, dataAtualizacao: agora);

        _repositoryMock.Setup(r => r.ObterPorCodigoAsync(codigo))
            .ReturnsAsync(produtoExistente);

        // Act
        var resultado = await _useCase.ExecutarAsync(codigo);

        // Assert
        Assert.NotNull(resultado);
        Assert.Equal("PROD-001", resultado.Codigo);
        Assert.Equal("Teclado Mecânico", resultado.Descricao);
        Assert.Equal(15, resultado.Saldo);
        Assert.Equal(2, resultado.Version);
        
        _repositoryMock.Verify(r => r.ObterPorCodigoAsync(codigo), Times.Once);
    }

    [Fact]
    public async Task Deve_Lancar_KeyNotFoundException_Quando_Produto_Nao_Existir_No_Banco()
    {
        // Arrange
        var codigo = "PROD-999";

        _repositoryMock.Setup(r => r.ObterPorCodigoAsync(codigo))
            .ReturnsAsync((Produto?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => 
            _useCase.ExecutarAsync(codigo)
        );

        _repositoryMock.Verify(r => r.ObterPorCodigoAsync(codigo), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("AB")] // Menor que 3 caracteres (conforme a regra do CodigoProdutoValidator)
    [InlineData("PROD@001")] // Caractere inválido (@)
    public async Task Deve_Lancar_ValidationException_Quando_Codigo_For_Invalido_Na_Borda(string codigoInvalido)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => 
            _useCase.ExecutarAsync(codigoInvalido)
        );

        // Como falhou na validação da borda, o repositório nem deve ser consultado
        _repositoryMock.Verify(r => r.ObterPorCodigoAsync(It.IsAny<string>()), Times.Never);
    }
}