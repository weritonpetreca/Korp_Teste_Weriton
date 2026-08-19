using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Estoque.API.Handlers;
using Estoque.Application.DTOs;
using Estoque.Application.UseCases;
using Estoque.Application.Validators;
using Estoque.Domain;
using Estoque.Domain.Validators;
using Estoque.Domain.Repositories;
using Estoque.Infrastructure.Repositories;
using FluentValidation;
using Estoque.API.Endpoints;
using Estoque.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. CONFIGURAÇÃO AWS E DYNAMODB (DevSecOps)
// ==========================================
builder.Services.AddSingleton<IAmazonDynamoDB>(sp =>
{
    // Verificamos se há uma URL de serviço local no appsettings.json (ex: nosso Docker local)
    var localServiceUrl = builder.Configuration["DynamoDb:ServiceUrl"];
    
    if (!string.IsNullOrEmpty(localServiceUrl))
    {
        // AMBIENTE LOCAL (Desenvolvimento/Testes): Usamos credenciais falsas para o container local
        var localConfig = new AmazonDynamoDBConfig { ServiceURL = localServiceUrl };
        return new AmazonDynamoDBClient("fakeMyKeyId", "fakeSecretAccessKey", localConfig);
    }

    // AMBIENTE CLOUD (AWS): 
    // Princípio do Least Privilege: Não passamos chaves! 
    // O SDK assume automaticamente a IAM Role (Task Role do ECS/EKS ou EC2 Profile) injetada pela AWS.
    var cloudConfig = new AmazonDynamoDBConfig 
    { 
        RegionEndpoint = Amazon.RegionEndpoint.USEast1 // Defina sua região correta (ex: sa-east-1 para São Paulo)
    };
    return new AmazonDynamoDBClient(cloudConfig);
});

// ==========================================
// 2. INJEÇÃO DE DEPENDÊNCIA (CLEAN ARCHITECTURE + POLLY)
// ==========================================

// A. Registramos o repositório REAL (apenas acessível para injeção)
builder.Services.AddScoped<ProdutoRepository>();
builder.Services.AddScoped<IIdempotenciaRepository, IdempotenciaRepository>();

// B. Registramos a INTERFACE entregando o DECORATOR (A Mágica da Resiliência)
// Quando a aplicação pedir 'IProdutoRepository', entregaremos o 'ResilientProdutoRepository' 
// passando o ProdutoRepository real dentro dele.
builder.Services.AddScoped<IProdutoRepository>(provider => 
{
    var repositoryReal = provider.GetRequiredService<ProdutoRepository>();
    var logger = provider.GetRequiredService<ILogger<ResilientProdutoRepository>>();
    return new ResilientProdutoRepository(repositoryReal, logger);
});

// C. Registramos os Casos de Uso (Use Cases)
builder.Services.AddScoped<CadastrarProdutoUseCase>();
builder.Services.AddScoped<DebitarEstoqueUseCase>();
builder.Services.AddScoped<CreditarEstoqueUseCase>();
builder.Services.AddScoped<AtualizarDescricaoUseCase>();
builder.Services.AddScoped<ObterProdutoPorCodigoUseCase>();

// D. Aqui registraremos os validadores do FluentValidation...
builder.Services.AddScoped<IValidator<Produto>, ProdutoValidator>();
builder.Services.AddScoped<IValidator<DebitarEstoqueRequest>, DebitarEstoqueRequestValidator>();
builder.Services.AddScoped<IValidator<CreditarEstoqueRequest>, CreditarEstoqueRequestValidator>();
builder.Services.AddScoped<IValidator<AtualizarDescricaoRequest>, AtualizarDescricaoRequestValidator>();
builder.Services.AddScoped<IValidator<string>, CodigoProdutoValidator>();

// E. REGISTRO DO TRATAMENTO DE EXCEÇÕES E PROBLEM DETAILS
builder.Services.AddExceptionHandler<GlobalExceptionHandler>(); // Registra nosso handler
builder.Services.AddProblemDetails(); // Ativa a formatação RFC 7807 no container

// F. SWAGGER / OPENAPI (O nosso Bestiário)
builder.Services.AddEndpointsApiExplorer(); // Permite que o .NET explore nossos endpoints
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo 
    { 
        Title = "Korp Estoque API", 
        Version = "v1",
        Description = "Microsserviço de gerenciamento de estoque com concorrência otimista (DynamoDB)."
    });
});

// G. CORS (Cross-Origin Resource Sharing) para permitir que o Angular acesse a API
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// H. REGISTRO DO SERVIÇO DE INTELIGÊNCIA ARTIFICIAL (Gemini)
builder.Services.AddHttpClient<GeminiAiService>();
builder.Services.AddTransient<GeminiAiService>(sp => {
    var client = sp.GetRequiredService<HttpClient>();
    var config = sp.GetRequiredService<IConfiguration>();
    var logger = sp.GetRequiredService<ILogger<GeminiAiService>>();
    var apiKey = config["Gemini:ApiKey"] ?? string.Empty;
    return new GeminiAiService(client, apiKey, logger);
});

// ==========================================
// 3. BUILD E CONFIGURAÇÃO HTTP
// ==========================================
var app = builder.Build();

app.UseExceptionHandler();
app.UseHttpsRedirection();

app.UseCors("AllowAngular");

// ATIVA O SWAGGER (Apenas para facilitar o teste local. Em prod rigoroso, podemos desativar por segurança)
app.UseSwagger();
app.UseSwaggerUI(c => 
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Estoque API v1");
    // Faz a interface gráfica abrir direto na URL raiz do servidor (localhost:porta/)
    c.RoutePrefix = string.Empty; 
});

// Rota de Teste para garantir que a API subiu
app.MapGet("/health", () => Results.Ok(new { Status = "Estoque Service is Online" }));

// REGISTRA O NOSSO QUADRO DE AVISOS (ENDPOINTS DE PRODUTOS)
app.MapProdutoEndpoints();

// No mundo real (Produção/AWS), o Terraform cria a tabela.
// Aqui, verificamos se estamos em desenvolvimento para criar a tabela automaticamente no Docker.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dynamoDbClient = scope.ServiceProvider.GetRequiredService<IAmazonDynamoDB>();
    var tableName = "Korp_Estoque_Table";

    try
    {
        app.Logger.LogInformation("Verificando se a tabela {TableName} já existe...", tableName);
        
        // Se ela existir, deletamos e aguardamos o término real da exclusão
        await dynamoDbClient.DeleteTableAsync(tableName);
        app.Logger.LogInformation("Aguardando exclusão completa da tabela no DynamoDB Local...");
        
        // Loop de espera ativa até o DynamoDB confirmar que a tabela sumiu
        bool tabelaDeletada = false;
        while (!tabelaDeletada)
        {
            try
            {
                await dynamoDbClient.DescribeTableAsync(tableName);
                await Task.Delay(500); // Espera meio segundo e tenta de novo
            }
            catch (ResourceNotFoundException)
            {
                tabelaDeletada = true; // A exceção confirma que a tabela finalmente sumiu!
            }
        }
        app.Logger.LogInformation("Tabela antiga removida com sucesso.");
    }
    catch (ResourceNotFoundException)
    {
        // Se a tabela não existia, seguimos normalmente
    }

    // Criamos a tabela do zero com apenas a PK
    app.Logger.LogInformation("Criando a tabela {TableName} com a estrutura correta (apenas PK)...", tableName);
    var request = new CreateTableRequest
    {
        TableName = tableName,
        AttributeDefinitions = new List<AttributeDefinition>
        {
            new AttributeDefinition { AttributeName = "PK", AttributeType = "S" }
        },
        KeySchema = new List<KeySchemaElement>
        {
            new KeySchemaElement { AttributeName = "PK", KeyType = "HASH" }
        },
        BillingMode = BillingMode.PAY_PER_REQUEST
    };

    await dynamoDbClient.CreateTableAsync(request);
    app.Logger.LogInformation("Tabela {TableName} criada com sucesso!", tableName);
}

app.Run();

// Necessário para o WebApplicationFactory enxergar o Program nos Testes de Integração
namespace Estoque.API
{
    public partial class Program { }
}