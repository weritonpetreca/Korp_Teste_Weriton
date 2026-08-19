using Amazon.DynamoDBv2;
using Faturamento.API.Endpoints;
using Faturamento.API.Handlers;
using Faturamento.Application.DTOs;
using Faturamento.Application.UseCases;
using Faturamento.Application.Validators;
using Faturamento.Domain.Clients;
using Faturamento.Domain.Repositories;
using Faturamento.Infrastructure.Clients;
using Faturamento.Infrastructure.Repositories;
using FluentValidation;

var builder = WebApplication.CreateBuilder(args);

// 1. Configuração do AWS DynamoDB (Suporte a Local / Testcontainers ou Nuvem)
builder.Services.AddSingleton<IAmazonDynamoDB>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var serviceUrl = configuration["DynamoDb:ServiceUrl"];
    
    if (!string.IsNullOrEmpty(serviceUrl))
    {
        return new AmazonDynamoDBClient(new AmazonDynamoDBConfig { ServiceURL = serviceUrl });
    }
    return new AmazonDynamoDBClient();
});

// 2. Registro de Repositórios e Casos de Uso na Injeção de Dependências
builder.Services.AddScoped<INotaFiscalRepository, NotaFiscalRepository>();
builder.Services.AddScoped<IIdempotenciaRepository, IdempotenciaRepository>();
builder.Services.AddScoped<CriarNotaFiscalUseCase>();
builder.Services.AddScoped<ImprimirNotaFiscalUseCase>();

// 3. Registro do FluentValidation
builder.Services.AddScoped<IValidator<CriarNotaFiscalRequest>, CriarNotaFiscalRequestValidator>();

// 4. Configuração do Cliente HTTP Resiliente (Polly v8) para o Microsserviço de Estoque
var estoqueBaseUrl = builder.Configuration["EstoqueService:BaseUrl"] ?? "http://localhost:5000";

builder.Services.AddHttpClient<IEstoqueClient, EstoqueClient>(client =>
{
    client.BaseAddress = new Uri(estoqueBaseUrl);
})
.AddStandardResilienceHandler(options =>
{
    // Configurações avançadas de Retry, Circuit Breaker e Timeout padrão da indústria
    options.Retry.MaxRetryAttempts = 3;
    options.Retry.Delay = TimeSpan.FromSeconds(1);
    options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(3);
    options.CircuitBreaker.FailureRatio = 0.5; // Abre se 50% das chamadas falharem
    options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(10);
    options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(5); // Tempo de Fail-Fast
});

// 5. Tratamento Global de Erros (RFC 7807)
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// 6. Swagger / OpenAPI para documentação da API
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Inicialização automática da tabela no DynamoDB para ambiente de desenvolvimento/testes
await EnsureTableCreatedAsync(app.Services);

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Mapeamento dos Endpoints
app.MapNotaFiscalEndpoints();

app.Run();

// Método auxiliar para provisionar a tabela de Faturamento no DynamoDB local automaticamente
static async Task EnsureTableCreatedAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var dynamoDb = scope.ServiceProvider.GetRequiredService<IAmazonDynamoDB>();
    const string tableName = "Korp_Faturamento_Table";

    try
    {
        var tables = await dynamoDb.ListTablesAsync();
        if (!tables.TableNames.Contains(tableName))
        {
            await dynamoDb.CreateTableAsync(new Amazon.DynamoDBv2.Model.CreateTableRequest
            {
                TableName = tableName,
                BillingMode = BillingMode.PAY_PER_REQUEST,
                AttributeDefinitions = [
                    new Amazon.DynamoDBv2.Model.AttributeDefinition("PK", Amazon.DynamoDBv2.ScalarAttributeType.S)
                ],
                KeySchema = [
                    new Amazon.DynamoDBv2.Model.KeySchemaElement("PK", Amazon.DynamoDBv2.KeyType.HASH)
                ]
            });
        }
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "Aviso ao tentar criar a tabela Korp_Faturamento_Table automaticamente.");
    }
}

// Expõe a classe Program para testes de integração com WebApplicationFactory
namespace Faturamento.API
{
    public partial class Program { }
}