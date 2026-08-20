using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
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
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

// ==========================================
// 1. CONFIGURAÇÃO AWS E DYNAMODB (DevSecOps)
// ==========================================
builder.Services.AddSingleton<IAmazonDynamoDB>(sp =>
{
    var serviceUrl = builder.Configuration["DynamoDb:ServiceUrl"];
    
    if (!string.IsNullOrEmpty(serviceUrl))
    {
        var localConfig = new AmazonDynamoDBConfig { ServiceURL = serviceUrl };
        return new AmazonDynamoDBClient("fakeMyKeyId", "fakeSecretAccessKey", localConfig);
    }

    var cloudConfig = new AmazonDynamoDBConfig { RegionEndpoint = Amazon.RegionEndpoint.USEast1 };
    return new AmazonDynamoDBClient(cloudConfig);
});

// ==========================================
// 2. INJEÇÃO DE DEPENDÊNCIA
// ==========================================
builder.Services.AddScoped<INotaFiscalRepository, NotaFiscalRepository>();
builder.Services.AddScoped<IIdempotenciaRepository, IdempotenciaRepository>();
builder.Services.AddScoped<CriarNotaFiscalUseCase>();
builder.Services.AddScoped<ImprimirNotaFiscalUseCase>();

// FluentValidation
builder.Services.AddScoped<IValidator<CriarNotaFiscalRequest>, CriarNotaFiscalRequestValidator>();

// Polly & HTTP Client
var estoqueBaseUrl = builder.Configuration["EstoqueService:BaseUrl"] ?? "http://localhost:5245";
builder.Services.AddHttpClient<IEstoqueClient, EstoqueClient>(client =>
{
    client.BaseAddress = new Uri(estoqueBaseUrl);
})
.AddStandardResilienceHandler(options =>
{
    options.Retry.MaxRetryAttempts = 3;
    options.Retry.Delay = TimeSpan.FromSeconds(1);
    options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(3);
    options.CircuitBreaker.FailureRatio = 0.5;
    options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(10);
    options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(5);
});

// Tratamento de Erros e ProblemDetails
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo 
    { 
        Title = "Korp Faturamento API", 
        Version = "v1",
        Description = "Microsserviço de gerenciamento de faturamento com concorrência otimista (DynamoDB)."
    });
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// ==========================================
// 3. BUILD E CONFIGURAÇÃO HTTP
// ==========================================
var app = builder.Build();

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseCors("AllowAngular");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => 
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Estoque API v1");
        // Faz a interface gráfica abrir direto na URL raiz do servidor (localhost:porta/)
        c.RoutePrefix = string.Empty; 
    });
}

app.MapNotaFiscalEndpoints();

// ==========================================
// 4. PROVISIONAMENTO AUTOMÁTICO (DEV)
// ==========================================
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dynamoDb = scope.ServiceProvider.GetRequiredService<IAmazonDynamoDB>();
    const string tableName = "Korp_Faturamento_Table";

    try
    {
        app.Logger.LogInformation("Verificando se a tabela {TableName} já existe...", tableName);

        await dynamoDb.CreateTableAsync(new CreateTableRequest
        {
            TableName = tableName,
            BillingMode = BillingMode.PAY_PER_REQUEST,
            AttributeDefinitions = [ new AttributeDefinition("PK", ScalarAttributeType.S) ],
            KeySchema = [ new KeySchemaElement("PK", KeyType.HASH) ]
        });
        
        app.Logger.LogInformation("Tabela {TableName} criada com sucesso!", tableName);
    }
    catch (ResourceInUseException)
    {
        app.Logger.LogInformation("A tabela {TableName} já existe no DynamoDB. Mantendo os dados existentes.", tableName);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Erro ao provisionar tabela {TableName}.", tableName);
    }
}

app.Run();

namespace Faturamento.API { public partial class Program { } }