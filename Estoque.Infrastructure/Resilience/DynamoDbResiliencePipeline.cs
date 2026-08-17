using System.Net;
using Amazon.DynamoDBv2;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace Estoque.Infrastructure.Resilience;

public static class DynamoDbResiliencePipeline
{
    // O Polly v8+ usa o padrão ResiliencePipeline para agrupar múltiplas estratégias
    public static ResiliencePipeline GetPipeline(ILogger logger)
    {
        return new ResiliencePipelineBuilder()
            // Estratégia 1: Retry com Exponential Backoff e Jitter
            .AddRetry(new RetryStrategyOptions
            {
                // O "Pulo do Gato": Definimos exatamente QUAIS erros merecem retry.
                ShouldHandle = new PredicateBuilder()
                    .Handle<AmazonDynamoDBException>(ex => 
                        // Ignoramos a falha de versão (Regra de Negócio), pois retentar mascararia o erro.
                        ex.ErrorCode != "ConditionalCheckFailedException" && 
                        // Retentamos apenas em casos de Throttling (Limite de capacidade) ou indisponibilidade da AWS
                        (ex.ErrorCode == "ProvisionedThroughputExceededException" || 
                         ex.StatusCode == HttpStatusCode.InternalServerError || 
                         ex.StatusCode == HttpStatusCode.ServiceUnavailable)),
                
                MaxRetryAttempts = 3, // Tenta até 3 vezes antes de desistir
                Delay = TimeSpan.FromMilliseconds(500), // Começa esperando 500ms
                BackoffType = DelayBackoffType.Exponential, // Escala o tempo: 500ms, 1s, 2s...
                UseJitter = true, // Adiciona milissegundos aleatórios para evitar ataque em massa (Thundering Herd)
                OnRetry = args =>
                {
                    // Usa LogWarning pois é uma anomalia, mas o sistema está tentando se recuperar
                    logger.LogWarning("[Resiliência - Retry] Falha transiente na AWS. Tentativa {Attempt}. Aguardando {Delay}ms.", 
                        args.AttemptNumber, args.RetryDelay.TotalMilliseconds);
                    return default;
                }
            })
            // Estratégia 2: Circuit Breaker (Disjuntor)
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                ShouldHandle = new PredicateBuilder()
                    .Handle<AmazonDynamoDBException>(ex => ex.ErrorCode != "ConditionalCheckFailedException"),
                
                FailureRatio = 0.5, // Se 50% das requisições falharem...
                SamplingDuration = TimeSpan.FromSeconds(30), // ...numa janela de 30 segundos...
                MinimumThroughput = 5, // ...considerando um mínimo de 5 chamadas no período...
                BreakDuration = TimeSpan.FromSeconds(15), // ...o circuito ABRE e bloqueia novas requisições por 15 segundos.
                OnOpened = args =>
                {
                    // LogError porque o circuito abriu, o banco caiu ou está inacessível
                    logger.LogError("CIRCUIT_BREAKER_OPENED! DynamoDB instável. Bloqueando tráfego por {Duration}s.", 
                        args.BreakDuration.TotalSeconds);
                    return default;
                },
                OnClosed = args =>
                {
                    // LogInformation para indicar que a saúde do sistema voltou ao normal
                    logger.LogInformation("[Resiliência - Disjuntor] CIRCUITO FECHADO! Operação normal restabelecida.");
                    return default;
                },
                OnHalfOpened = args =>
                {
                    // LogWarning
                    logger.LogWarning("CIRCUIT_BREAKER_HALF_OPENED! Tentado restaurar tráfego com DynamoDB.");
                    return default;
                }
            }
            )
            .Build(); // Constrói o tubo (pipeline) unindo Retry e Circuit Breaker
    }
}