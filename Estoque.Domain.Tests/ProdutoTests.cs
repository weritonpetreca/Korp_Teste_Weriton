using Estoque.Domain;
using Estoque.Domain.Validators;

namespace Estoque.Domain.Tests;

public class ProdutoTests
{
    private readonly ProdutoValidator _validator;

    // No xUnit, o construtor da classe de teste roda antes de cada método de teste (equivale ao @BeforeEach do JUnit)
    public ProdutoTests()
    {
        _validator = new ProdutoValidator();
    }

    [Fact]
    public void Deve_Validar_Com_Sucesso_Quando_Dados_Forem_Validos()
    {
        // Arrange
        var produto = new Produto("PROD-001", "Teclado Mecânico", 10);

        // Act
        var resultado = _validator.Validate(produto);

        // Assert
        Assert.True(resultado.IsValid);
        Assert.Empty(resultado.Errors); // A lista de erros deve estar vazia
    }

    [Theory]
    [InlineData("", "O código do produto não pode ser vazio.")]
    [InlineData("PROD@001", "O código do produto deve conter apenas letras, números e hifens.")]
    [InlineData("<script>alert(1)</script>", "O código do produto deve conter apenas letras, números e hifens.")]
    public void Nao_Deve_Validar_Quando_Codigo_For_Invalido_Ou_Malicioso(string codigoInvalido, string mensagemEsperada)
    {
        // Arrange - O código inválido entra, o resto permanece válido
        var produto = new Produto(codigoInvalido, "Teclado", 10);

        // Act
        var resultado = _validator.Validate(produto);

        // Assert
        Assert.False(resultado.IsValid);
        // Verifica se dentro da lista de erros existe algum erro com a mensagem exata que estamos esperando
        Assert.Contains(resultado.Errors, e => e.ErrorMessage == mensagemEsperada);
    }

    [Theory]
    [InlineData("", "A descrição do produto não pode ser vazia.")]
    [InlineData("Monitor 24\" <script>fetch('hacker')</script>", "A descrição contém caracteres inválidos de formatação.")]
    public void Nao_Deve_Validar_Quando_Descricao_For_Invalida_Ou_Tentar_XSS(string descricaoInvalida, string mensagemEsperada)
    {
        var produto = new Produto("PROD-001", descricaoInvalida, 10);
        
        var resultado = _validator.Validate(produto);

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.ErrorMessage == mensagemEsperada);
    }

    [Theory]
    [InlineData(-1, "O saldo inicial não pode ser negativo.")]
    [InlineData(1000000, "O saldo inicial não pode exceder 999.999 unidades.")]
    public void Nao_Deve_Validar_Quando_Saldo_Fugir_Dos_Limites(int saldoInvalido, string mensagemEsperada)
    {
        var produto = new Produto("PROD-001", "Teclado", saldoInvalido);
        
        var resultado = _validator.Validate(produto);

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.ErrorMessage == mensagemEsperada);
    }
}