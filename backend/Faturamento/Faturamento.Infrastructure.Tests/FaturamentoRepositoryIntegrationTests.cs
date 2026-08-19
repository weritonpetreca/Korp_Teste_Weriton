using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Faturamento.Domain.Entities;
using Faturamento.Domain.Enums;
using Faturamento.Domain.Idempotencia;
using Faturamento.Infrastructure.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Testcontainers.DynamoDb;

namespace Faturamento.Infrastructure.Tests;

public class FaturamentoRepositoryIntegrationTests : IAsyncLifetime
{
    private readonly DynamoDbContainer _dynamoDbContainer = new DynamoDbBuilder("amazon/dynamodb-local:2.5.0")
        .Build();

    private IAmazonDynamoDB _dynamoDbClient = null!;
    private NotaFiscalRepository _notaFiscalRepository = null!;
    private IdempotenciaRepository _idempotenciaRepository = null!;

    public async Task InitializeAsync()
    {
        // Spins up the real DynamoDB container via Docker
        await _dynamoDbContainer.StartAsync();

        var config = new AmazonDynamoDBConfig
        {
            ServiceURL = _dynamoDbContainer.GetConnectionString()
        };

        // Connects the AWS SDK client to the container instance
        _dynamoDbClient = new AmazonDynamoDBClient("dummy", "dummy", config);

        _notaFiscalRepository = new NotaFiscalRepository(_dynamoDbClient, NullLogger<NotaFiscalRepository>.Instance);
        _idempotenciaRepository = new IdempotenciaRepository(_dynamoDbClient, NullLogger<IdempotenciaRepository>.Instance);

        // Creates the physical table required for the tests
        await CreateTableAsync();
    }

    public async Task DisposeAsync()
    {
        _dynamoDbClient?.Dispose();
        await _dynamoDbContainer.StopAsync();
    }

    private async Task CreateTableAsync()
    {
        var request = new CreateTableRequest
        {
            TableName = "Korp_Faturamento_Table",
            BillingMode = BillingMode.PAY_PER_REQUEST,
            AttributeDefinitions =
            [
                new AttributeDefinition("PK", ScalarAttributeType.S)
            ],
            KeySchema =
            [
                new KeySchemaElement("PK", KeyType.HASH)
            ]
        };

        await _dynamoDbClient.CreateTableAsync(request);
    }

    [Fact]
    public async Task Deve_Salvar_E_Obter_Nota_Fiscal_Com_Sucesso()
    {
        // Arrange
        var nota = new NotaFiscal("NOTA-TEST-001");
        nota.AdicionarItem("PROD-X", 3);

        // Act
        await _notaFiscalRepository.SalvarAsync(nota);
        var notaConsultada = await _notaFiscalRepository.ObterPorNumeroAsync("NOTA-TEST-001");

        // Assert
        Assert.NotNull(notaConsultada);
        Assert.Equal("NOTA-TEST-001", notaConsultada.Numero);
        Assert.Equal(StatusNota.Aberta, notaConsultada.Status);
        Assert.Single(notaConsultada.Itens);
        Assert.Equal("PROD-X", notaConsultada.Itens[0].CodigoProduto);
        Assert.Equal(3, notaConsultada.Itens[0].Quantidade);
    }

    [Fact]
    public async Task Deve_Salvar_E_Obter_Registro_Idempotencia_Com_Sucesso()
    {
        // Arrange
        var registro = new RegistroIdempotencia("uuid-key-999", 200, "{\"ok\":true}");

        // Act
        await _idempotenciaRepository.SalvarAsync(registro);
        var registroConsultado = await _idempotenciaRepository.ObterPorChaveAsync("uuid-key-999");

        // Assert
        Assert.NotNull(registroConsultado);
        Assert.Equal("uuid-key-999", registroConsultado.Chave);
        Assert.Equal(200, registroConsultado.StatusCode);
        Assert.Equal("{\"ok\":true}", registroConsultado.RespostaJson);
    }

    [Fact]
    public async Task Deve_Garantir_Condicional_De_Idempotencia_Evitando_Duplicidade()
    {
        // Arrange
        var registro1 = new RegistroIdempotencia("uuid-duplicate", 200, "first");
        var registro2 = new RegistroIdempotencia("uuid-duplicate", 200, "second");

        // Act
        await _idempotenciaRepository.SalvarAsync(registro1);
        // Saving again with the same key should hit the ConditionExpression (attribute_not_exists) and fail silently
        await _idempotenciaRepository.SalvarAsync(registro2);

        // Assert - should preserve the original record ("first")
        var registroConsultado = await _idempotenciaRepository.ObterPorChaveAsync("uuid-duplicate");
        Assert.NotNull(registroConsultado);
        Assert.Equal("first", registroConsultado.RespostaJson);
    }
}