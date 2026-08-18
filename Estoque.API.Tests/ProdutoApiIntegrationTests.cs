using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.DynamoDb;
using Xunit;
using Estoque.Application.DTOs;

namespace Estoque.API.Tests;

public class ProdutoApiIntegrationTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly DynamoDbContainer _dynamoDbContainer = new DynamoDbBuilder("amazon/dynamodb-local:2.5.0")
        .Build();

    private readonly WebApplicationFactory<Program> _factory = factory;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await _dynamoDbContainer.StartAsync();

        var customFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("DynamoDb:ServiceUrl", _dynamoDbContainer.GetConnectionString());
        });

        _client = customFactory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
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
        // Arrange: Injetamos a chave de Idempotência
        _client.DefaultRequestHeaders.Add("X-Idempotency-Key", Guid.NewGuid().ToString());
        
        var codigoUnico = $"PROD-API-{Guid.NewGuid().ToString()[..6].ToUpper()}";
        var requestCadastrar = new CadastrarProdutoRequest(codigoUnico, "Poção de Vida", 50);

        // Act: Cadastra
        var responseCadastrar = await _client.PostAsJsonAsync("/api/produtos", requestCadastrar);
        Assert.Equal(HttpStatusCode.Created, responseCadastrar.StatusCode);

        // Act: Consulta (GET não precisa do header)
        var responseConsultar = await _client.GetAsync($"/api/produtos/{codigoUnico}");
        Assert.Equal(HttpStatusCode.OK, responseConsultar.StatusCode);
        
        var produtoDto = await responseConsultar.Content.ReadFromJsonAsync<ProdutoResponseDto>();
        Assert.NotNull(produtoDto);
        Assert.Equal(codigoUnico, produtoDto.Codigo);
        Assert.Equal("Poção de Vida", produtoDto.Descricao);
    }

    [Fact]
    public async Task Deve_Retornar_BadRequest_When_Cadastrar_Com_Dados_Invalidos()
    {
        _client.DefaultRequestHeaders.Add("X-Idempotency-Key", Guid.NewGuid().ToString());
        
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

    [Fact]
    public async Task Deve_Retornar_BadRequest_Quando_Faltar_Header_Idempotencia()
    {
        // NÃO injetamos o header "_client.DefaultRequestHeaders.Add(...)" de propósito
        
        var requestCadastrar = new CadastrarProdutoRequest("PROD-SEM-HEADER", "Item Teste", 10);
        var response = await _client.PostAsJsonAsync("/api/produtos", requestCadastrar);
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Cabeçalho Obrigatório Ausente", content);
    }

    [Fact]
    public async Task Deve_Garantir_Idempotencia_Com_Mesma_Chave()
    {
        var chaveIdempotenciaUnica = Guid.NewGuid().ToString();
        _client.DefaultRequestHeaders.Add("X-Idempotency-Key", chaveIdempotenciaUnica);

        var requestCadastrar = new CadastrarProdutoRequest("PROD-DUPLO", "Espada de Prata", 1);

        // 1ª Chamada - Vai pro banco real
        var response1 = await _client.PostAsJsonAsync("/api/produtos", requestCadastrar);
        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);
        var content1 = await response1.Content.ReadAsStringAsync();

        // 2ª Chamada idêntica (Simulando o usuário clicando duas vezes ou retry da rede)
        var response2 = await _client.PostAsJsonAsync("/api/produtos", requestCadastrar);
        
        // Assert: A API tem que retornar 201 Created (não pode dar conflito) e o payload deve ser idêntico
        Assert.Equal(HttpStatusCode.Created, response2.StatusCode);
        var content2 = await response2.Content.ReadAsStringAsync();
        
        Assert.Equal(content1, content2);
    }
}