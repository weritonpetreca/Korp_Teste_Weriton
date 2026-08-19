using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Faturamento.Domain.Idempotencia;
using Faturamento.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Faturamento.Infrastructure.Repositories;

public class IdempotenciaRepository(
    IAmazonDynamoDB dynamoDb, 
    ILogger<IdempotenciaRepository> logger) : IIdempotenciaRepository
{
    private readonly IAmazonDynamoDB _dynamoDb = dynamoDb;
    private readonly ILogger<IdempotenciaRepository> _logger = logger;
    private const string TableName = "Korp_Faturamento_Table";

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
            // Blindagem contra race conditions: Se outro thread/pod já gravou, o DynamoDB barra
            ConditionExpression = "attribute_not_exists(PK)"
        };

        try
        {
            await _dynamoDb.PutItemAsync(request);
        }
        catch (ConditionalCheckFailedException)
        {
            _logger.LogWarning("Colisão de Idempotência detectada e evitada para a chave {Chave}.", registro.Chave);
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