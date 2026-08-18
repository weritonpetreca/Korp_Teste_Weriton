# Korp Estoque API 📦🚀

Microsserviço de gerenciamento de estoque de alta performance e resiliência, desenvolvido em **.NET 8** com **Clean Architecture**, controle de concorrência otimista (**Optimistic Locking**) integrado ao **AWS DynamoDB**, padrões avançados de resiliência (**Polly**) e segurança de transações (**Idempotência**).

---

## 🏗️ 1. Arquitetura e Organização de Pastas
O projeto segue estritamente os princípios do **SOLID**, **Clean Architecture** e **Twelve-Factor App**, desacoplando o domínio de negócio dos detalhes de infraestrutura e frameworks:

```text
Korp_Teste_Weriton/
│
├── Estoque.Domain/          # Regras de negócio puras, Entidades, Contratos de Repositório, Validadores de Domínio e Modelos (ex: Idempotência)
├── Estoque.Application/     # Casos de Uso (Use Cases), DTOs e Validadores de Entrada (Fail-Fast)
├── Estoque.Infrastructure/  # Implementações reais (AWS DynamoDB Low-Level, Polly Resilience Pipelines, Single-Table Design)
├── Estoque.API/             # Minimal APIs, Endpoints, Tratamento Global de Erros (RFC 7807), Filtros de Rota (Endpoint Filters) e DI
│
├── Estoque.Domain.Tests/        # Testes unitários de domínio e comportamentos (Invariantes e Guard Clauses)
├── Estoque.Application.Tests/   # Testes unitários de Casos de Uso com Moq
├── Estoque.Infrastructure.Tests/# Testes de integração reais com bancos em container (Testcontainers)
└── Estoque.API.Tests/           # Testes funcionais/E2E de API com WebApplicationFactory
```

---

## 🛡️ 2. Padrões de Projeto e Decisões Técnicas (ADRs)

* **Domínio Rico e Protegido:** As entidades de negócio (`Produto`) gerenciam suas próprias invariantes usando *Guard Clauses* explícitas em seus construtores e comportamentos (`DebitarEstoque`, `CreditarEstoque`, `AtualizarDescricao`), garantindo integridade de estado em tempo de execução.
* **Fail-Fast com FluentValidation:** Validações de sintaxe e borda executadas antes de tocar no domínio ou no banco de dados, retornando falhas claras instantaneamente.
* **Idempotência HTTP (Segurança de Transação):** Implementação de filtro na borda (`IEndpointFilter` nas Minimal APIs) que exige rigorosamente o cabeçalho `X-Idempotency-Key` em operações de mutação (POST/PUT). Evita processamento duplicado causado por *retries* de rede do lado do cliente, operando como um "cache" de resposta.
* **Single-Table Design (DynamoDB) e FinOps:** Otimização de custos de infraestrutura e performance utilizando a mesma tabela (`Korp_Estoque_Table`) para armazenar domínios diferentes (Produtos e Registros de Idempotência). O isolamento é feito via prefixos de Chave Primária (`PROD#` e `IDEMPOTENCY#`), utilizando o recurso de **TTL (Time To Live)** nativo da AWS para expurgo automático de lixo gerado pela idempotência após 24 horas.
* **Concorrência Otimista (Optimistic Locking):** Utilização do atributo `Version` mapeado em conjunto com expressões condicionais do DynamoDB (`Version = :expectedVersion`) para evitar condições de corrida (*race conditions*) em atualizações simultâneas de estoque.
* **Resiliência com Polly v8+:** Pipeline integrada de **Retry com Exponential Backoff + Jitter** (evitando picos de requisições no servidor em falhas transientes) e **Circuit Breaker** (protegendo o sistema contra falhas em cascata se a AWS/DynamoDB cair).
* **Tratamento Global de Erros (RFC 7807):** Padronização de respostas de erro da API utilizando `ProblemDetails`, mapeando exceções de negócio e infraestrutura para códigos HTTP semânticos (`400`, `404`, `409`, `500`).

---

## 🧪 3. Pirâmide de Testes Automatizados
O projeto conta com uma suíte de testes 100% automatizada e isolada (Todos passando no pipeline):

1. **Testes Unitários de Domínio:** Validam regras de negócio, invariantes e *Guard Clauses* das entidades (`ArgumentException`, `InvalidOperationException`).
2. **Testes de Casos de Uso (Application):** Isolam as regras de aplicação utilizando mocks (`Moq`) para garantir que os repositórios e fluxos se comportem corretamente perante entradas válidas e inválidas.
3. **Testes de Integração de Infraestrutura:** Utilizam **Testcontainers** para subir um container Docker real do `amazon/dynamodb-local` dinamicamente, testando operações reais de banco de dados e controle de concorrência.
4. **Testes Funcionais/API (E2E):** Utilizam `WebApplicationFactory` em memória combinada com Testcontainers para testar os endpoints HTTP de ponta a ponta (`POST`, `GET`, validações de borda, tratamentos de erro e proteção rigorosa contra quebra de Idempotência e falta de Headers).

---

## 🚀 4. Como Executar o Projeto Localmente

### Pré-requisitos
* **.NET 8 SDK** instalado.
* **Docker** rodando na máquina (necessário para os testes de integração com Testcontainers e DynamoDB Local).

### Passos para Rodar a Aplicação
1. Clone o repositório:
   ```bash
   git clone [https://github.com/weritonpetreca/Korp_Teste_Weriton.git](https://github.com/weritonpetreca/Korp_Teste_Weriton.git)
   cd Korp_Teste_Weriton
   ```
2. Restaure as dependências:
   ```bash
   dotnet restore
   ```
3. Execute a aplicação:
   ```bash
   dotnet run --project Estoque.API/Estoque.API.csproj
   ```
   *(A API sobe com Swagger habilitado na raiz e cria automaticamente a tabela local no ambiente de desenvolvimento).*

### Como Executar a Suíte de Testes
Para rodar toda a pirâmide de testes (Unitários, Integração com containers e API) de uma só vez com logs consolidados:
```bash
dotnet test
```