using System.Net;
using System.Net.Http.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Faturamento.Application.DTOs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.DynamoDb;

namespace Faturamento.API.Tests;

public class FaturamentoApiIntegrationTests : IAsyncLifetime
{
    private readonly DynamoDbContainer _dynamoDbContainer = new DynamoDbBuilder("amazon/dynamodb-local:2.5.0")
        .Build();

    private WebApplicationFactory<Faturamento.API.Program> _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await _dynamoDbContainer.StartAsync();

        // Configures the WebApplicationFactory to override DynamoDB service URL pointing to the test container
        _factory = new WebApplicationFactory<Faturamento.API.Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services =>
            {
                // Replaces production DynamoDB client with the Testcontainer instance
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IAmazonDynamoDB));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                var config = new AmazonDynamoDBConfig
                {
                    ServiceURL = _dynamoDbContainer.GetConnectionString()
                };
                
                services.AddSingleton<IAmazonDynamoDB>(new AmazonDynamoDBClient("dummy", "dummy", config));
            });
        });

        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        await _dynamoDbContainer.StopAsync();
    }

    private async Task EnsureTableCreatedAsync()
    {
        var config = new AmazonDynamoDBConfig { ServiceURL = _dynamoDbContainer.GetConnectionString() };
        using var client = new AmazonDynamoDBClient("dummy", "dummy", config);

        await client.CreateTableAsync(new CreateTableRequest
        {
            TableName = "Korp_Faturamento_Table",
            BillingMode = BillingMode.PAY_PER_REQUEST,
            AttributeDefinitions = [new AttributeDefinition("PK", ScalarAttributeType.S)],
            KeySchema = [new KeySchemaElement("PK", KeyType.HASH)]
        });
    }

    [Fact]
    public async Task Deve_Retornar_BadRequest_Quando_Faltar_Cabecalho_Idempotencia()
    {
        // Arrange
        var requestBody = new CriarNotaFiscalRequest([new ItemNotaRequest("PROD-1", 2)]);
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/api/notas")
        {
            Content = JsonContent.Create(requestBody)
        };
        // Intentionally omitting 'X-Idempotency-Key' header

        // Act
        var response = await _client.SendAsync(requestMessage);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Deve_Criar_Nota_Fiscal_Com_Sucesso_Enviando_Idempotencia()
    {
        // Arrange
        var requestBody = new CriarNotaFiscalRequest([new ItemNotaRequest("PROD-1", 2)]);
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/api/notas")
        {
            Content = JsonContent.Create(requestBody)
        };
        requestMessage.Headers.Add("X-Idempotency-Key", Guid.NewGuid().ToString());

        // Act
        var response = await _client.SendAsync(requestMessage);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}