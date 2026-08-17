using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Estoque.Domain;
using Estoque.Domain.Repositories;

namespace Estoque.Infrastructure.Repositories;

// A classe assina o contrato : IProdutoRepository
public class ProdutoRepository(IAmazonDynamoDB dynamoDb) : IProdutoRepository
{
    private readonly IAmazonDynamoDB _dynamoDb = dynamoDb;
    private const string TableName = "Korp_Estoque_Table";

    public async Task SalvarAsync(Produto produto)
    {
        var request = new PutItemRequest
        {
            TableName = TableName,
            // Mapeamento manual para proteger o Domínio. 
            // Tudo no DynamoDB low-level é tratado como 'S' (String), 'N' (Number), etc.
            Item = new Dictionary<string, AttributeValue>
            {
                { "PK", new AttributeValue { S = $"PROD#{produto.Codigo}" } },
                { "Codigo", new AttributeValue { S = produto.Codigo } },
                { "Descricao", new AttributeValue { S = produto.Descricao } },
                { "Saldo", new AttributeValue { N = produto.Saldo.ToString() } },
                { "Version", new AttributeValue { N = produto.Version.ToString() } }
            },
            // SHIFT-LEFT SECURITY (Idempotência no Banco): 
            // Só insere se a Chave Primária (PK) não existir. 
            // Se tentarem cadastrar dois produtos com o mesmo código simultaneamente, o DynamoDB bloqueia o segundo.
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

        // Se o banco não achar o item, a coleção vem vazia. Retornamos null com segurança.
        if (response.Item == null || response.Item.Count == 0)
        {
            return null;
        }

        // Reconstruímos a entidade a partir dos dados do banco
        var produto = new Produto(
            response.Item["Codigo"].S,
            response.Item["Descricao"].S,
            int.Parse(response.Item["Saldo"].N),
            int.Parse(response.Item["Version"].N)
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
                { "Version", new AttributeValue { N = produto.Version.ToString() } }
            },
            // OPTIMISTIC LOCKING (Controle de Concorrência Bônus do SRS):
            // Garante que o Update só aconteça se ninguém tiver mexido no produto no meio tempo.
            ConditionExpression = "Version = :expectedVersion",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":expectedVersion", new AttributeValue { N = (produto.Version - 1).ToString() } }
            }
        };

        await _dynamoDb.PutItemAsync(request);
    }
}