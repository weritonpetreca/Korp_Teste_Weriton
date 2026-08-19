namespace Estoque.Domain.Tests;

public class ProdutoComportamentoTests
{
    [Fact]
    public void Deve_Criar_Produto_Com_Sucesso_E_Versao_Inicial_Um()
    {
        // Act
        var produto = new Produto("PROD-001", "Espada de Prata", 10);

        // Assert
        Assert.Equal("PROD-001", produto.Codigo);
        Assert.Equal("Espada de Prata", produto.Descricao);
        Assert.Equal(10, produto.Saldo);
        Assert.Equal(1, produto.Version);
    }

    [Theory]
    [InlineData("", "Descrição inválida.")]
    [InlineData("   ", "Descrição inválida.")]
    [InlineData(null, "Descrição inválida.")]
    public void Nao_Deve_Permitir_Atualizar_Com_Descricao_Invalida(string descricaoInvalida, string mensagemEsperada)
    {
        // Arrange
        var produto = new Produto("PROD-001", "Espada", 10);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => produto.AtualizarDescricao(descricaoInvalida));
        Assert.Equal(mensagemEsperada, exception.Message);
    }

    [Fact]
    public void Deve_Atualizar_Descricao_E_Incrementar_Versao()
    {
        // Arrange
        var produto = new Produto("PROD-001", "Espada Antiga", 10);

        // Act
        produto.AtualizarDescricao("Espada de Aço");

        // Assert
        Assert.Equal("Espada de Aço", produto.Descricao);
        Assert.Equal(2, produto.Version); // Versão deve subir de 1 para 2
    }

    [Fact]
    public void Deve_Debitar_Estoque_Com_Sucesso_E_Incrementar_Versao()
    {
        // Arrange
        var produto = new Produto("PROD-001", "Poção", 20);

        // Act
        produto.DebitarEstoque(5);

        // Assert
        Assert.Equal(15, produto.Saldo);
        Assert.Equal(2, produto.Version);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Nao_Deve_Permitir_Debitar_Quantidade_Invalida(int quantidadeInvalida)
    {
        // Arrange
        var produto = new Produto("PROD-001", "Poção", 20);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => produto.DebitarEstoque(quantidadeInvalida));
    }

    [Fact]
    public void Nao_Deve_Permitir_Debitar_Acima_Do_Saldo_Atual()
    {
        // Arrange
        var produto = new Produto("PROD-001", "Poção", 10);

        // Act & Assert (Deve lançar InvalidOperationException que vira HTTP 400)
        var exception = Assert.Throws<InvalidOperationException>(() => produto.DebitarEstoque(15));
        Assert.Equal("Saldo insuficiente.", exception.Message);
    }

    [Fact]
    public void Deve_Creditar_Estoque_Com_Sucesso_E_Incrementar_Versao()
    {
        // Arrange
        var produto = new Produto("PROD-001", "Escudo", 10);

        // Act
        produto.CreditarEstoque(5);

        // Assert
        Assert.Equal(15, produto.Saldo);
        Assert.Equal(2, produto.Version);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Nao_Deve_Permitir_Creditar_Quantidade_Invalida(int quantidadeInvalida)
    {
        // Arrange
        var produto = new Produto("PROD-001", "Escudo", 10);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => produto.CreditarEstoque(quantidadeInvalida));
    }
}