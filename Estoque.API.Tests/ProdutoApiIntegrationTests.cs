using System.Net;
using System.Net.Http.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.DynamoDb;
using Xunit;
using Estoque.Application.DTOs;

namespace Estoque.API.Tests;

public class ProdutoApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly DynamoDbContainer _dynamoDbContainer = new DynamoDbBuilder("amazon/dynamodb-local:2.5.0")
        .Build();

    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        // 1. Sobe o container real do DynamoDB Local
        await _dynamoDbContainer.StartAsync();

        // 2. Configura a fábrica da API injetando a URL do container do Testcontainers nas configurações da API
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("DynamoDb:ServiceUrl", _dynamoDbContainer.GetConnectionString());
        });

        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose(); // HttpClient usa Dispose() normal
        await _factory.DisposeAsync();
        await _dynamoDbContainer.DisposeAsync();
    }

    [Fact]
    public async Task Deve_Retornar_Online_No_Endpoint_Health()
    {
        var response = await _client.GetAsync("/health");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Estoque Service is Online", content);
    }

    [Fact]
    public async Task Deve_Cadastrar_E_Consultar_Produto_Com_Sucesso()
    {
        var codigoUnico = $"PROD-API-{Guid.NewGuid().ToString()[..6].ToUpper()}";
        var requestCadastrar = new CadastrarProdutoRequest(codigoUnico, "Poção de Vida", 50);

        var responseCadastrar = await _client.PostAsJsonAsync("/api/produtos", requestCadastrar);
        Assert.Equal(HttpStatusCode.Created, responseCadastrar.StatusCode);

        var responseConsultar = await _client.GetAsync($"/api/produtos/{codigoUnico}");
        Assert.Equal(HttpStatusCode.OK, responseConsultar.StatusCode);
        
        var produtoDto = await responseConsultar.Content.ReadFromJsonAsync<ProdutoResponseDto>();
        Assert.NotNull(produtoDto);
        Assert.Equal(codigoUnico, produtoDto.Codigo);
        Assert.Equal("Poção de Vida", produtoDto.Descricao);
        Assert.Equal(50, produtoDto.Saldo);
        Assert.Equal(1, produtoDto.Version);
    }

    [Fact]
    public async Task Deve_Retornar_BadRequest_When_Cadastrar_Com_Dados_Invalidos()
    {
        var requestInvalido = new CadastrarProdutoRequest("PROD@INVALIDO!", "", -5);
        var response = await _client.PostAsJsonAsync("/api/produtos", requestInvalido);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Deve_Retornar_NotFound_Quando_Consultar_Produto_Inexistente()
    {
        var response = await _client.GetAsync("/api/produtos/CODIGO-QUE-NAO-EXISTE-999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}