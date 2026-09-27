# Vortex Bank · Next-Gen Financial Core

[![Live Demo](https://img.shields.io/badge/Live%20Demo-bank.vortexsoftware.tech-0A84FF?style=for-the-badge&logo=google-cloud&logoColor=white)](https://bank.vortexsoftware.tech)
![.NET 10](https://img.shields.io/badge/.NET%2010-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=csharp&logoColor=white)
![React 19](https://img.shields.io/badge/React%2019-61DAFB?style=for-the-badge&logo=react&logoColor=black)
![TypeScript](https://img.shields.io/badge/TypeScript-3178C6?style=for-the-badge&logo=typescript&logoColor=white)
![PostgreSQL 16](https://img.shields.io/badge/PostgreSQL%2016-336791?style=for-the-badge&logo=postgresql&logoColor=white)
![Redis 7](https://img.shields.io/badge/Redis%207-DC382D?style=for-the-badge&logo=redis&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-2496ED?style=for-the-badge&logo=docker&logoColor=white)

Plataforma bancária digital e motor transacional de alta fidelidade desenvolvido em **C# / .NET 10** com Clean Architecture e frontend SPA moderno em **React 19 + TypeScript + Vite**. Integrado ao ecossistema institucional da **Vortex Software**.

---

### 🌐 Demonstração Online em Produção

Acesse a plataforma bancária completa na infraestrutura de alta disponibilidade da **Vortex Software**:

👉 **[https://bank.vortexsoftware.tech](https://bank.vortexsoftware.tech)**

---

## Principais Recursos & Capacidades

- **Microsserviço de Autenticação (`AuthService`):**
  - Emissão e validação de tokens JWT assimétricos (chaves pública e privada).
  - Gestão de credenciais, controle de sessões e autorização granular.
- **Motor de Transações & Ledger (`TransactionsService`):**
  - Livro-razão (*double-entry bookkeeping*) com garantia de consistência ACID.
  - Transferências Pix instantâneas, extratos detalhados e saldos consolidados.
  - Cache de alta performance no Redis 7 e persistência relacional no PostgreSQL 16.
- **Frontend SPA Bancário Moderno (`web`):**
  - Desenvolvido em **React 19** com **TypeScript** e **Vite**.
  - Dashboard financeiro responsivo com extrato, Pix, cartões e investimentos.
  - Geração e exportação nativa de comprovantes e extrato em PDF client-side (`jsPDF`).
- **Engenharia de Qualidade:**
  - 100% dos testes unitários e de integração aprovados com xUnit e Testcontainers.

---

## Arquitetura de Microsserviços

```text
       Usuário / Navegador (HTTPS)
                   │
                   ▼
       Nginx Gateway Reverso (bank.vortexsoftware.tech)
                   │
     ┌─────────────┴─────────────┐
     ▼                           ▼
Frontend Web (React 19)    API Gateway (/api)
                                 │
                 ┌───────────────┴───────────────┐
                 ▼                               ▼
       AuthService (:8080)             TransactionsService (:8080)
                 │                               │
                 ▼                               ▼
       PostgreSQL (auth_db)            PostgreSQL (trans_db) + Redis 7
```

---

## Estrutura da Solução (`Bankcore.slnx`)

- `src/Services/AuthService`: API, Domínio, Aplicação e Infraestrutura de identidade.
- `src/Services/TransactionsService`: API, Domínio, Aplicação e Infraestrutura de pagamentos e ledger.
- `tests/`: Bateria de testes de domínio e persistência.
- `web/`: Aplicação frontend React 19 + TypeScript.
- `docker/`: Configurações de containerização, Nginx e orquestração Docker Compose.

---

## Executar Localmente

### Pré-requisitos
- .NET 10 SDK
- Node.js 20+ e npm
- Docker e Docker Compose

1. Inicie a infraestrutura e os bancos de dados:
   ```bash
   cd docker
   docker compose up -d
   ```

2. Inicie os microsserviços .NET:
   ```bash
   dotnet run --project src/Services/AuthService/Api/Bankcore.Auth.Api.csproj
   dotnet run --project src/Services/TransactionsService/Api/Bankcore.Transactions.Api.csproj
   ```

3. Inicie o frontend React:
   ```bash
   cd web
   npm install
   npm run dev
   ```

---

## Licença

MIT © [João Paulo Gregório](https://github.com/joaop-gregorioDS) · [Vortex Software](https://vortexsoftware.tech)
