# Korp ERP - Microsserviços de Estoque e Faturamento 📦🚀

![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![Angular](https://img.shields.io/badge/Angular-DD0031?style=for-the-badge&logo=angular&logoColor=white)
![AWS DynamoDB](https://img.shields.io/badge/Amazon%20DynamoDB-4053D6?style=for-the-badge&logo=Amazon%20DynamoDB&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-2496ED?style=for-the-badge&logo=docker&logoColor=white)
![Google Gemini](https://img.shields.io/badge/Google%20Gemini-8E75B2?style=for-the-badge&logo=googlebard&logoColor=white)
![Clean Architecture](https://img.shields.io/badge/Architecture-Clean_Architecture-success?style=for-the-badge)

Ecossistema de microsserviços de alta performance desenvolvido para o desafio técnico Korp. Construído em **.NET 8** e **Angular**, o projeto implementa os princípios do **SOLID**, **Clean Architecture** e **Twelve-Factor App**, garantindo um design distribuído moderno, escalável e tolerante a falhas, incluindo integração nativa com Inteligência Artificial.

---

## 🏗️ 1. Arquitetura e Organização do Sistema

O sistema é composto por dois microsserviços autônomos no backend e uma aplicação SPA no frontend.

```text
Korp_Teste_Weriton/
│
├── frontend/                # SPA em Angular standalone (Gestão e Emissão de Notas)
│
├── backend/
│   ├── Estoque.API/         # Microsserviço de catálogo e integração com Google Gemini IA
│   ├── Faturamento.API/     # Microsserviço de orquestração de Notas Fiscais e comunicação
│   └── Tests/               # Pirâmide de Testes (Unitários, Testcontainers e E2E)
```

---

## 🛡️ 2. Decisões Técnicas e Funcionalidades (ADRs)

### Backend (C# .NET 8)
* **Concorrência Otimista & Idempotência:** Utilização do atributo `Version` (DynamoDB `ConditionExpression`) para evitar *race conditions* no estoque. Filtros de borda (`IEndpointFilter`) exigem `X-Idempotency-Key` para garantir segurança transacional em operações de mutação.
* **Resiliência entre Serviços (Polly v8):** Comunicação entre `Faturamento` e `Estoque` protegida por **Retry com Exponential Backoff** e **Circuit Breaker**, evitando falhas em cascata.
* **Single-Table Design (DynamoDB):** Isolamento de registros (Notas, Produtos e Idempotência) na mesma tabela via prefixos de Chave Primária (`NOTA#`, `PROD#`, `IDEMPOTENCY#`). Utilização de LINQ (`.OrderBy`, `.Select`) para ordenação cronológica em memória após operações de `Scan`.
* **Inteligência Artificial Generativa:** Integração via REST nativo (`HttpClient`) com o modelo **Google Gemini 3.6 Flash**, atuando como um Copilot de Supply Chain para análise semântica de saldos em tempo real.

### Frontend (Angular)
* **Ciclos de Vida e Reatividade:** Utilização extensiva do `ngOnInit` para sincronização inicial de estado com o backend.
* **RxJS e Tratamento Assíncrono:** Consumo das APIs utilizando observáveis (`.subscribe()`), aplicando o operador `finalize` do RxJS para garantir o encerramento de indicadores de carregamento (loading spinners) de forma determinística, independentemente de sucesso ou falha na requisição.
* **Renderização Segura:** Utilização da diretiva `[innerHTML]` atrelada a sanitização via Expressões Regulares (Regex) para converter e renderizar marcações markdown da IA (negrito e itálico) em tags HTML nativas sem quebrar o layout.

---

## ⚙️ 3. Configuração de Ambiente (Docker & Segurança)

Para rodar o projeto localmente garantindo isolamento e segurança, siga os passos abaixo:

### A. Subindo o Banco de Dados (Docker)
O sistema utiliza o DynamoDB Local. Para subir o banco rapidamente em background, execute o comando direto no seu terminal:
```bash
docker run -d -p 8000:8000 amazon/dynamodb-local
```
*O container rodará na porta `8000`. As APIs estão configuradas para criar as tabelas automaticamente no modo de desenvolvimento.*

### B. Configurando a Chave de IA de Forma Segura (User Secrets)
A chave da API do Google Gemini **não** deve ser exposta no código-fonte. Utilize a ferramenta de *User Secrets* do .NET para injetá-la com segurança no serviço de Estoque:

1. Navegue até a pasta do microsserviço de Estoque: `cd backend/Estoque.API`
2. Inicialize e configure o segredo:
```bash
dotnet user-secrets init
dotnet user-secrets set "Gemini:ApiKey" "SUA_CHAVE_REAL_DO_GEMINI_AQUI"
```

---

## 🚀 4. Execução e Testes

### Subindo os Microsserviços (Backend)
Em terminais separados, inicie cada API. O Swagger estará disponível em `http://localhost:<porta>/`.
```bash
# Terminal 1 - Estoque
dotnet run --project backend/Estoque.API/Estoque.API.csproj

# Terminal 2 - Faturamento
dotnet run --project backend/Faturamento.API/Faturamento.API.csproj
```

### Subindo o Frontend (Angular)
Navegue até a pasta `frontend`, instale as dependências (necessário apenas na primeira execução) e inicie o servidor:
```bash
npm install
npm start
```
*Acesse a aplicação no navegador em `http://localhost:4200`.*

### Executando a Pirâmide de Testes
A suíte de testes utiliza `Testcontainers` para validar a infraestrutura real. Com o Docker rodando em sua máquina, navegue até a raiz do backend e execute:
```bash
dotnet test
```