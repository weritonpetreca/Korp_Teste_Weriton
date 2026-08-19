using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Faturamento.Domain.Entities;
using Faturamento.Domain.Enums;
using Faturamento.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Faturamento.Infrastructure.Repositories;

public class NotaFiscalRepository(
    IAmazonDynamoDB dynamoDb, 
    ILogger<NotaFiscalRepository> logger) : INotaFiscalRepository
{
    private readonly IAmazonDynamoDB _dynamoDb = dynamoDb;
    private readonly ILogger<NotaFiscalRepository> _logger = logger;
    private const string TableName = "Korp_Faturamento_Table";

    public async Task SalvarAsync(NotaFiscal nota)
    {
        // Mapeia os itens do domínio para o formato de lista de mapas do DynamoDB
        var itensAttributeList = nota.Itens.Select(item => new AttributeValue
        {
            M = new Dictionary<string, AttributeValue>
            {
                { "CodigoProduto", new AttributeValue { S = item.CodigoProduto } },
                { "Quantidade", new AttributeValue { N = item.Quantidade.ToString() } }
            }
        }).ToList();

        var request = new PutItemRequest
        {
            TableName = TableName,
            Item = new Dictionary<string, AttributeValue>
            {
                { "PK", new AttributeValue { S = $"NOTA#{nota.Numero}" } },
                { "Numero", new AttributeValue { S = nota.Numero } },
                { "Status", new AttributeValue { S = nota.Status.ToString() } },
                { "DataCriacao", new AttributeValue { S = nota.DataCriacao } },
                { "Itens", new AttributeValue { L = itensAttributeList } }
            }
        };

        await _dynamoDb.PutItemAsync(request);
        _logger.LogInformation("Nota Fiscal {Numero} salva com sucesso no DynamoDB.", nota.Numero);
    }

    public async Task<NotaFiscal?> ObterPorNumeroAsync(string numero)
    {
        var request = new GetItemRequest
        {
            TableName = TableName,
            Key = new Dictionary<string, AttributeValue>
            {
                { "PK", new AttributeValue { S = $"NOTA#{numero}" } }
            }
        };

        var response = await _dynamoDb.GetItemAsync(request);

        if (response.Item == null || response.Item.Count == 0)
        {
            return null;
        }

        var statusStr = response.Item["Status"].S;
        var status = Enum.Parse<StatusNota>(statusStr);
        var dataCriacao = response.Item["DataCriacao"].S;

        // Reconstrói os itens do domínio a partir do DynamoDB
        var itens = response.Item["Itens"].L.Select(attrMap => new ItemNota(
            attrMap.M["CodigoProduto"].S,
            int.Parse(attrMap.M["Quantidade"].N)
        )).ToList();

        return new NotaFiscal(numero, status, itens, dataCriacao);
    }
}