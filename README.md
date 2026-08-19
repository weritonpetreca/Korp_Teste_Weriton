# Korp ERP - Microsserviços de Estoque e Faturamento 📦🚀

![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![AWS DynamoDB](https://img.shields.io/badge/Amazon%20DynamoDB-4053D6?style=for-the-badge&logo=Amazon%20DynamoDB&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-2496ED?style=for-the-badge&logo=docker&logoColor=white)
![Clean Architecture](https://img.shields.io/badge/Architecture-Clean_Architecture-success?style=for-the-badge)
![Test Coverage](https://img.shields.io/badge/Coverage-100%25-brightgreen?style=for-the-badge)

Ecossistema de microsserviços de alta performance e resiliência desenvolvido para o desafio técnico Korp. Construído em **.NET 8**, o projeto implementa os princípios do **SOLID**, **Clean Architecture** e **Twelve-Factor App**, garantindo um design distribuído moderno, escalável e tolerante a falhas.

---

## 🏗️ 1. Arquitetura e Organização do Sistema

O sistema é composto por dois microsserviços autônomos (`Estoque` e `Faturamento`), desacoplando o domínio de negócio dos detalhes de infraestrutura e frameworks.

```text
Korp_Teste_Weriton/
│
├── Estoque/                 # Microsserviço responsável pelo catálogo e saldo de produtos
│   ├── Estoque.Domain/      # Entidades, Contratos (Repositories) e Guard Clauses
│   ├── Estoque.Application/ # Casos de Uso (CQRS), DTOs e FluentValidation (Fail-Fast)
│   ├── Estoque.Infra/       # AWS DynamoDB SDK, Single-Table Design
│   └── Estoque.API/         # Minimal APIs, Filtros de Idempotência e RFC 7807 (Problem Details)
│
├── Faturamento/             # Microsserviço responsável pela emissão de notas fiscais
│   ├── Faturamento.Domain/  # Regras de transição de status de Notas e Itens
│   ├── Faturamento.App/     # Orquestração de impressão e comunicação com o Estoque
│   ├── Faturamento.Infra/   # Persistência de Notas Fiscais (Document Model no DynamoDB)
│   └── Faturamento.API/     # Políticas de Resiliência (Polly) e Injeção de Dependências
│
└── Tests/                   # Pirâmide de Testes (Unitários, Testcontainers e E2E Cross-Service)
```

---

## 🛡️ 2. Padrões de Projeto e Decisões Técnicas (ADRs)

* **Concorrência Otimista (Optimistic Locking):** Utilização do atributo `Version` mapeado com expressões condicionais do DynamoDB (`ConditionExpression`) para evitar *race conditions* em atualizações simultâneas de estoque.
* **Comunicação Resiliente entre Serviços (Polly v8):** O `Faturamento` se comunica com o `Estoque` via HTTP REST protegido por um pipeline de resiliência avançado. Implementamos **Retry com Exponential Backoff + Jitter** e **Circuit Breaker**, protegendo o ecossistema contra falhas em cascata e latência de rede.
* **Idempotência de Borda (Segurança Transacional):** Filtros personalizados (`IEndpointFilter`) nas Minimal APIs exigem rigorosamente o cabeçalho `X-Idempotency-Key` em operações de mutação (POST/PUT). O sistema atua como um cache de *short-circuit*, evitando duplo processamento por *retries* do cliente.
* **Single-Table Design & Document Model (DynamoDB):** 
  * *Estoque:* Armazena Produtos e Registros de Idempotência na mesma tabela com isolamento via prefixos de Chave Primária (`PROD#` e `IDEMPOTENCY#`).
  * *Faturamento:* Utiliza Arrays de Mapas (`List<Map>`) para gravar Notas Fiscais e seus Itens em uma única operação atômica, otimizando custos de RCU/WCU (FinOps).
  * *TTL (Time To Live):* Expurgo automático gerenciado pela AWS para limpar cache de idempotência após 24 horas a custo zero.
* **Tratamento Global de Erros (RFC 7807):** Respostas da API padronizadas com `ProblemDetails`, interceptando exceções de negócio (`InvalidOperationException`) e validação de borda, mapeando-as para os códigos HTTP corretos (`400`, `404`, `409`, `503`).

---

## 🧪 3. Suíte de Testes e Qualidade de Código

A qualidade do sistema é garantida por uma pirâmide de testes rigorosa, totalmente automatizada:

1. **Testes Unitários de Domínio & Aplicação (`xUnit`, `Moq`):** Garantem que regras de negócio, transições de estado e *Guard Clauses* sejam validadas sem dependência externa.
2. **Testes de Integração de Infraestrutura (`Testcontainers`):** Instanciam um container Docker dinâmico do `amazon/dynamodb-local` para provar a comunicação real com o banco de dados, validando schemas, *locking* e expressões condicionais.
3. **Testes E2E Cross-Service (O Ápice da Pirâmide):** Utilizando `WebApplicationFactory`, o projeto levanta as APIs de `Estoque` e `Faturamento` simultaneamente em memória. Uma transação iniciada no Faturamento atravessa a rede simulada, é processada no Estoque e reflete nos bancos de dados integrados, provando a eficácia do Circuit Breaker e do HTTP Client.

---

## 🚀 4. Como Executar o Projeto Localmente

### Pré-requisitos
* **.NET 8 SDK**
* **Docker** rodando na máquina (Obrigatório para os testes automatizados e banco local).

### Subindo os Microsserviços
Em terminais separados, navegue até a raiz do projeto e execute:

**Terminal 1 (Estoque API):**
```bash
dotnet run --project Estoque.API/Estoque.API.csproj
```

**Terminal 2 (Faturamento API):**
```bash
dotnet run --project Faturamento.API/Faturamento.API.csproj
```
*(Ambas as APIs criarão suas respectivas tabelas no DynamoDB Local automaticamente e disponibilizarão o Swagger UI).*

### Executando a Automação de Testes
Para rodar toda a pirâmide de testes de forma consolidada e ver a mágica dos containers subindo dinamicamente, execute na raiz do projeto:
```bash
dotnet test
```

---
*Desenvolvido com excelência técnica e foco em arquitetura de software escalável.*