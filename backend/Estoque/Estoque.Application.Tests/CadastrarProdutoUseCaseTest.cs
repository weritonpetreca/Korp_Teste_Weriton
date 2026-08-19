using Moq;
using Estoque.Application.DTOs;
using Estoque.Application.UseCases;
using Estoque.Domain;
using Estoque.Domain.Repositories;
using Estoque.Domain.Validators;

namespace Estoque.Application.Tests;

public class CadastrarProdutoUseCaseTests
{
    private readonly Mock<IProdutoRepository> _produtoRepositoryMock;
    private readonly CadastrarProdutoUseCase _useCase;

    public CadastrarProdutoUseCaseTests()
   {
        // 1. Instanciamos o Mock do Repositório (o "dublê" do banco)
        _produtoRepositoryMock = new Mock<IProdutoRepository>();
        
        // 2. Instanciamos o validador real (ele não tem efeitos colaterais ou dependências externas)
        var validator = new ProdutoValidator();
        
        // 3. Injetamos ambos no Caso de Uso via Construtor Primário
        _useCase = new CadastrarProdutoUseCase(_produtoRepositoryMock.Object, validator);
    }

    [Fact]
    public async Task Deve_Cadastrar_Produto_Com_Sucesso_E_Chamar_Repositorio()
    {
        // Arrange
        var request = new CadastrarProdutoRequest("PROD-001", "Teclado Mecânico", 10);

        // Act
        // Como o método é assíncrono (Task), usamos o 'await'
        await _useCase.ExecutarAsync(request);

        // Assert
        // Verificamos (Verify) se o método SalvarAsync foi chamado exatamente 1 vez (Times.Once)
        // com qualquer objeto (It.IsAny) do tipo Produto. É o equivalente ao verify() do Mockito.
        _produtoRepositoryMock.Verify(repo => repo.SalvarAsync(It.IsAny<Produto>()), Times.Once);
    }

    [Fact]
    public async Task Nao_Deve_Chamar_Repositorio_Quando_Dados_Forem_Invalidos()
    {
        // Arrange (Código vazio para forçar o erro de validação)
        var request = new CadastrarProdutoRequest("", "Teclado", 10);

        // Act & Assert
        // O método deve lançar a ValidationException do FluentValidation. 
        // Note o uso do ThrowsAsync em vez do Throws normal, pois o método é assíncrono.
        await Assert.ThrowsAsync<ArgumentException>(() => _useCase.ExecutarAsync(request));

        // Verificamos que, como deu erro, o repositório NUNCA (Times.Never) foi chamado para salvar nada.
        _produtoRepositoryMock.Verify(repo => repo.SalvarAsync(It.IsAny<Produto>()), Times.Never);
    }
}