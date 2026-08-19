using Faturamento.Domain.Entities;
using Faturamento.Domain.Enums;

namespace Faturamento.Domain.Tests;

public class NotaFiscalTests
{
    [Fact]
    public void Deve_Criar_Nota_Fiscal_Com_Status_Aberta()
    {
        // Act
        var nota = new NotaFiscal("12345");

        // Assert
        Assert.Equal("12345", nota.Numero);
        Assert.Equal(StatusNota.Aberta, nota.Status);
        Assert.Empty(nota.Itens);
        Assert.NotNull(nota.DataCriacao);
    }

    [Fact]
    public void Deve_Adicionar_Item_Com_Sucesso_Quando_Aberta()
    {
        // Arrange
        var nota = new NotaFiscal("12345");

        // Act
        nota.AdicionarItem("PROD-01", 5);

        // Assert
        Assert.Single(nota.Itens);
        Assert.Equal("PROD-01", nota.Itens[0].CodigoProduto);
        Assert.Equal(5, nota.Itens[0].Quantidade);
    }

    [Fact]
    public void Nao_Deve_Adicionar_Item_Quando_Nota_Fechada()
    {
        // Arrange
        var nota = new NotaFiscal("12345");
        nota.AdicionarItem("PROD-01", 5);
        nota.FecharNota();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => nota.AdicionarItem("PROD-02", 2));
    }

    [Fact]
    public void Deve_Fechar_Nota_Com_Sucesso()
    {
        // Arrange
        var nota = new NotaFiscal("12345");
        nota.AdicionarItem("PROD-01", 2);
        
        // Act
        nota.FecharNota();

        // Assert
        Assert.Equal(StatusNota.Fechada, nota.Status);
    }

    [Fact]
    public void Nao_Deve_Fechar_Nota_Sem_Itens()
    {
        // Arrange
        var nota = new NotaFiscal("12345");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => nota.FecharNota());
    }
}