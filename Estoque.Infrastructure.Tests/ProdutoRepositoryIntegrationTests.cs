using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Estoque.Domain;
using Estoque.Infrastructure.Repositories;
using Testcontainers.DynamoDb;
using Xunit;

namespace Estoque.Infrastructure.Tests;

public class ProdutoRepositoryIntegrationTests : IAsyncLifetime
{
    private readonly DynamoDbContainer _dynamoDbContainer = new DynamoDbBuilder("amazon/dynamodb-local:2.5.0")
        .Build();

    private IAmazonDynamoDB _dynamoDbClient = null!;
    private ProdutoRepository _repository = null!;

    public async Task InitializeAsync()
    {
        await _dynamoDbContainer.StartAsync();

        var endpointConfig = new AmazonDynamoDBConfig
        {
            ServiceURL = _dynamoDbContainer.GetConnectionString()
        };

        _dynamoDbClient = new AmazonDynamoDBClient("fakeMyKeyId", "fakeSecretAccessKey", endpointConfig);
        _repository = new ProdutoRepository(_dynamoDbClient);

        await _dynamoDbClient.CreateTableAsync(new CreateTableRequest
        {
            TableName = "Korp_Estoque_Table",
            AttributeDefinitions = [new AttributeDefinition("PK", ScalarAttributeType.S)],
            KeySchema = [new KeySchemaElement("PK", KeyType.HASH)],
            BillingMode = BillingMode.PAY_PER_REQUEST
        });
    }

    public async Task DisposeAsync()
    {
        await _dynamoDbContainer.DisposeAsync();
    }

    [Fact]
    public async Task Deve_Salvar_E_Buscar_Produto_Com_Sucesso_No_DynamoDb_Real()
    {
        var produto = new Produto("PROD-TEST-01", "Mouse Gamer", 50);

        await _repository.SalvarAsync(produto);
        var produtoDoBanco = await _repository.ObterPorCodigoAsync("PROD-TEST-01");

        Assert.NotNull(produtoDoBanco);
        Assert.Equal("PROD-TEST-01", produtoDoBanco.Codigo);
        Assert.Equal("Mouse Gamer", produtoDoBanco.Descricao);
        Assert.Equal(50, produtoDoBanco.Saldo);
        Assert.Equal(1, produtoDoBanco.Version);
    }

    [Fact]
    public async Task Deve_Atualizar_Descricao_E_Creditar_Estoque_No_Banco_Real()
    {
        // Arrange
        var produto = new Produto("PROD-TEST-02", "Monitor Antigo", 10);
        await _repository.SalvarAsync(produto);

        // Act - Passo 1: Busca, altera a descrição e persiste (Version vira 2)
        var produtoParaAtualizar = await _repository.ObterPorCodigoAsync("PROD-TEST-02");
        produtoParaAtualizar!.AtualizarDescricao("Monitor Gamer 144Hz");
        await _repository.AtualizarAsync(produtoParaAtualizar);

        // Act - Passo 2: Busca a versão atualizada do banco (que já está na Version = 2), credita e persiste (Version vira 3)
        var produtoParaCreditar = await _repository.ObterPorCodigoAsync("PROD-TEST-02");
        produtoParaCreditar!.CreditarEstoque(15);
        await _repository.AtualizarAsync(produtoParaCreditar);

        // Assert - Verifica o estado final consolidado no DynamoDB real
        var produtoFinal = await _repository.ObterPorCodigoAsync("PROD-TEST-02");

        Assert.NotNull(produtoFinal);
        Assert.Equal("Monitor Gamer 144Hz", produtoFinal.Descricao);
        Assert.Equal(25, produtoFinal.Saldo); // 10 + 15
        Assert.Equal(3, produtoFinal.Version); // Version evoluiu corretamente de 1 para 3
    }

    [Fact]
    public async Task Deve_Falhar_Atualizacao_Quando_Ocorrer_Conflito_De_Concorrencia_Optimistic_Locking()
    {
        // Arrange
        var produto = new Produto("PROD-CONCORRENTE", "Teclado", 10);
        await _repository.SalvarAsync(produto);

        var instanciaA = await _repository.ObterPorCodigoAsync("PROD-CONCORRENTE");
        var instanciaB = await _repository.ObterPorCodigoAsync("PROD-CONCORRENTE");

        // Instância A debita 1 e atualiza com sucesso (Versão vai para 2 no banco)
        instanciaA!.DebitarEstoque(1);
        await _repository.AtualizarAsync(instanciaA);

        // Act & Assert
        // Instância B tenta atualizar com base na versão antiga (Versão 1). O DynamoDB rejeita.
        instanciaB!.DebitarEstoque(2);
        
        var excecao = await Assert.ThrowsAsync<ConditionalCheckFailedException>(() => 
            _repository.AtualizarAsync(instanciaB)
        );

        // Valida corretamente a mensagem exata retornada pela exceção do AWS SDK
        Assert.Equal("The conditional request failed", excecao.Message);
    }
}