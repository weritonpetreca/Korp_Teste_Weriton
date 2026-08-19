using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Estoque.Domain.Idempotencia;
using Estoque.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Estoque.Infrastructure.Repositories;

public class IdempotenciaRepository(
    IAmazonDynamoDB dynamoDb, 
    ILogger<IdempotenciaRepository> logger) : IIdempotenciaRepository
{
    private readonly IAmazonDynamoDB _dynamoDb = dynamoDb;
    private readonly ILogger<IdempotenciaRepository> _logger = logger;
    private const string TableName = "Korp_Estoque_Table";

    public async Task SalvarAsync(RegistroIdempotencia registro)
    {
        var request = new PutItemRequest
        {
            TableName = TableName,
            Item = new Dictionary<string, AttributeValue>
            {
                { "PK", new AttributeValue { S = $"IDEMPOTENCY#{registro.Chave}" } },
                { "StatusCode", new AttributeValue { N = registro.StatusCode.ToString() } },
                { "RespostaJson", new AttributeValue { S = registro.RespostaJson } },
                { "DataExpiracaoTtl", new AttributeValue { N = registro.DataExpiracaoTtl.ToString() } } 
            },
            ConditionExpression = "attribute_not_exists(PK)"
        };

        try
        {
            await _dynamoDb.PutItemAsync(request);
        }
        catch (ConditionalCheckFailedException)
        {
            // OBSERVABILIDADE (CloudWatch): Registramos a colisão para geração de métricas de SRE
            _logger.LogWarning(
                "Colisão de Idempotência detectada e evitada. A chave {Chave} já estava sendo processada por outra thread ou servidor.", 
                registro.Chave);
        }
    }

    public async Task<RegistroIdempotencia?> ObterPorChaveAsync(string chave)
    {
        var request = new GetItemRequest
        {
            TableName = TableName,
            Key = new Dictionary<string, AttributeValue>
            {
                { "PK", new AttributeValue { S = $"IDEMPOTENCY#{chave}" } }
            }
        };

        var response = await _dynamoDb.GetItemAsync(request);

        if (response.Item == null || response.Item.Count == 0)
        {
            return null;
        }

        return new RegistroIdempotencia(
            chave,
            int.Parse(response.Item["StatusCode"].N),
            response.Item["RespostaJson"].S,
            long.Parse(response.Item["DataExpiracaoTtl"].N)
        );
    }
}