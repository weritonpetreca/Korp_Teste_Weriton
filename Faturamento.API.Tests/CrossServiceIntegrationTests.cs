using System.Net.Http.Json;
using System.Text.Json;
using Amazon.DynamoDBv2;
using Faturamento.Application.DTOs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Faturamento.Infrastructure.Clients;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.DynamoDb;
using Faturamento.Domain.Clients;

namespace Faturamento.API.Tests;

public class CrossServiceIntegrationTests : IAsyncLifetime
{
    // 1. O BANCO DE DADOS: Sobe um container Docker real do DynamoDB
    private readonly DynamoDbContainer _dynamoDbContainer = new DynamoDbBuilder("amazon/dynamodb-local:2.5.0")
        .Build();

    private WebApplicationFactory<Estoque.API.Program> _estoqueFactory = null!;
    private WebApplicationFactory<Faturamento.API.Program> _faturamentoFactory = null!;
    private HttpClient _faturamentoClient = null!;

    public async Task InitializeAsync()
    {
        await _dynamoDbContainer.StartAsync();

        // 2. O SERVIÇO DE ESTOQUE: Sobe a API de Estoque em memória
        _estoqueFactory = new WebApplicationFactory<Estoque.API.Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(ReplaceDynamoDb); // Aponta o banco para o Docker
        });

        // Pegamos o endereço HTTP falso gerado em memória pelo Estoque
        var estoqueBaseAddress = _estoqueFactory.Server.BaseAddress;

        // 3. O SERVIÇO DE FATURAMENTO: Sobe a API e configuramos o HttpClient do Polly para apontar para o Estoque
        _faturamentoFactory = new WebApplicationFactory<Faturamento.API.Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(ReplaceDynamoDb); // Aponta o banco para o Docker
            
            // Override da rede para usar comunicação em memória
            builder.ConfigureTestServices(services =>
            {
                // Reconfigura o HttpClient do Estoque para usar o handler em memória em vez de TCP
                services.AddHttpClient<IEstoqueClient, EstoqueClient>(client =>
                {
                    client.BaseAddress = estoqueBaseAddress;
                })
                .ConfigurePrimaryHttpMessageHandler(() => _estoqueFactory.Server.CreateHandler());
            });
        });
        // O HttpClient que simula o Front-end Angular chamando o Faturamento
        _faturamentoClient = _faturamentoFactory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _faturamentoClient.Dispose();
        await _faturamentoFactory.DisposeAsync();
        await _estoqueFactory.DisposeAsync();
        await _dynamoDbContainer.StopAsync();
    }

    private void ReplaceDynamoDb(IServiceCollection services)
    {
        var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IAmazonDynamoDB));
        if (descriptor != null) services.Remove(descriptor);

        var config = new AmazonDynamoDBConfig { ServiceURL = _dynamoDbContainer.GetConnectionString() };
        services.AddSingleton<IAmazonDynamoDB>(new AmazonDynamoDBClient("dummy", "dummy", config));
    }

    [Fact]
    public async Task Deve_Integrar_Microsservicos_Com_Sucesso_Ao_Imprimir_Nota_E_Debitar_Estoque()
    {
        // Cliente que simula requisições diretas ao Estoque
        // A MÁGICA DE REDE: Para a comunicação in-memory funcionar perfeitamente, usamos o handler do Estoque
        var estoqueHandler = _estoqueFactory.Server.CreateHandler();
        var estoqueClient = new HttpClient(estoqueHandler) { BaseAddress = _estoqueFactory.Server.BaseAddress };

        // ====================================================================
        // PASSO 1: Cadastrar Produto no Estoque (com saldo inicial 10)
        // ====================================================================
        var produtoRequest = new { Codigo = "PROD-CROSS-01", Descricao = "Produto Teste Cross", Saldo = 10 };
        
        var createProdutoMsg = new HttpRequestMessage(HttpMethod.Post, "/api/produtos")
        {
            Content = JsonContent.Create(produtoRequest)
        };
        // CORREÇÃO: Faltava o Header de Idempotência aqui!
        createProdutoMsg.Headers.Add("X-Idempotency-Key", Guid.NewGuid().ToString()); 

        var createProdutoResponse = await estoqueClient.SendAsync(createProdutoMsg);
        
        // Se falhar de novo, isso vai nos dizer exatamente o motivo no terminal
        if (!createProdutoResponse.IsSuccessStatusCode)
        {
            var erro = await createProdutoResponse.Content.ReadAsStringAsync();
            throw new Exception($"Falha no Passo 1 (Estoque): Status {createProdutoResponse.StatusCode}. Detalhe: {erro}");
        }

        // ====================================================================
        // PASSO 2: Criar Nota Fiscal no Faturamento (pedindo 3 unidades)
        // ====================================================================
        var notaRequest = new CriarNotaFiscalRequest([new ItemNotaRequest("PROD-CROSS-01", 3)]);
        
        var createNotaMsg = new HttpRequestMessage(HttpMethod.Post, "/api/notas")
        {
            Content = JsonContent.Create(notaRequest)
        };
        createNotaMsg.Headers.Add("X-Idempotency-Key", Guid.NewGuid().ToString());

        var createNotaResponse = await _faturamentoClient.SendAsync(createNotaMsg);
        
        if (!createNotaResponse.IsSuccessStatusCode)
        {
            var erro = await createNotaResponse.Content.ReadAsStringAsync();
            throw new Exception($"Falha no Passo 2 (Criar Nota): Status {createNotaResponse.StatusCode}. Detalhe: {erro}");
        }

        var locationHeader = createNotaResponse.Headers.Location?.ToString();
        var numeroNota = locationHeader!.Split('/').Last();

        // ====================================================================
        // PASSO 3: Imprimir Nota (Faturamento chama o Estoque via HTTP)
        // ====================================================================
        var printMsg = new HttpRequestMessage(HttpMethod.Post, $"/api/notas/{numeroNota}/imprimir");
        printMsg.Headers.Add("X-Idempotency-Key", Guid.NewGuid().ToString());

        var printResponse = await _faturamentoClient.SendAsync(printMsg);
        
        if (!printResponse.IsSuccessStatusCode)
        {
            var erro = await printResponse.Content.ReadAsStringAsync();
            throw new Exception($"Falha no Passo 3 (Imprimir): Status {printResponse.StatusCode}. Detalhe: {erro}");
        }

        // ====================================================================
        // PASSO 4: Validar se o Saldo do Estoque foi reduzido (10 - 3 = 7)
        // ====================================================================
        var getProdutoResponse = await estoqueClient.GetAsync("/api/produtos/PROD-CROSS-01");
        var produtoJson = await getProdutoResponse.Content.ReadFromJsonAsync<JsonElement>();
        
        // Verifica se a propriedade existe e valida o valor
        var saldoAtual = produtoJson.GetProperty("saldo").GetInt32();
        Assert.Equal(7, saldoAtual);
    }
}