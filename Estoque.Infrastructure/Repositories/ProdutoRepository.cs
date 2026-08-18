using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Estoque.Domain;
using Estoque.Domain.Repositories;

namespace Estoque.Infrastructure.Repositories;

public class ProdutoRepository(IAmazonDynamoDB dynamoDb) : IProdutoRepository
{
    private readonly IAmazonDynamoDB _dynamoDb = dynamoDb;
    private const string TableName = "Korp_Estoque_Table";

    public async Task SalvarAsync(Produto produto)
    {
        var request = new PutItemRequest
        {
            TableName = TableName,
            Item = new Dictionary<string, AttributeValue>
            {
                { "PK", new AttributeValue { S = $"PROD#{produto.Codigo}" } },
                { "Codigo", new AttributeValue { S = produto.Codigo } },
                { "Descricao", new AttributeValue { S = produto.Descricao } },
                { "Saldo", new AttributeValue { N = produto.Saldo.ToString() } },
                { "Version", new AttributeValue { N = produto.Version.ToString() } },
                { "DataCriacao", new AttributeValue { S = produto.DataCriacao } },
                { "DataAtualizacao", new AttributeValue { S = produto.DataAtualizacao } }
            },
            ConditionExpression = "attribute_not_exists(PK)"
        };

        await _dynamoDb.PutItemAsync(request);
    }

    public async Task<Produto?> ObterPorCodigoAsync(string codigo)
    {
        var request = new GetItemRequest
        {
            TableName = TableName,
            Key = new Dictionary<string, AttributeValue>
            {
                { "PK", new AttributeValue { S = $"PROD#{codigo}" } }
            }
        };

        var response = await _dynamoDb.GetItemAsync(request);

        if (response.Item == null || response.Item.Count == 0)
        {
            return null;
        }

        // Reconstituição utilizando o construtor blindado que carrega a auditoria e a versão original
        var produto = new Produto(
            response.Item["Codigo"].S,
            response.Item["Descricao"].S,
            int.Parse(response.Item["Saldo"].N),
            int.Parse(response.Item["Version"].N),
            response.Item["DataCriacao"].S,
            response.Item["DataAtualizacao"].S
        );

        return produto;
    }

    public async Task AtualizarAsync(Produto produto)
    {
        var request = new PutItemRequest
        {
            TableName = TableName,
            Item = new Dictionary<string, AttributeValue>
            {
                { "PK", new AttributeValue { S = $"PROD#{produto.Codigo}" } },
                { "Codigo", new AttributeValue { S = produto.Codigo } },
                { "Descricao", new AttributeValue { S = produto.Descricao } },
                { "Saldo", new AttributeValue { N = produto.Saldo.ToString() } },
                { "Version", new AttributeValue { N = produto.Version.ToString() } },
                { "DataCriacao", new AttributeValue { S = produto.DataCriacao } }, // Preserva a data original de criação
                { "DataAtualizacao", new AttributeValue { S = produto.DataAtualizacao } } // Atualiza o carimbo de modificação
            },
            ConditionExpression = "Version = :expectedVersion",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":expectedVersion", new AttributeValue { N = (produto.Version - 1).ToString() } }
            }
        };

        await _dynamoDb.PutItemAsync(request);
    }
}