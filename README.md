# Vortex Bank · Next-Gen Financial Core & Monorepo

[![Live Demo Web](https://img.shields.io/badge/Live%20Demo-bank.vortexsoftware.tech-0A84FF?style=for-the-badge&logo=google-cloud&logoColor=white)](https://bank.vortexsoftware.tech)
[![Download Desktop](https://img.shields.io/badge/Download%20App-Windows%20x64%20(Tauri%202)-E11D48?style=for-the-badge&logo=windows&logoColor=white)](https://github.com/joaop-gregorioDS/vortex-bank/releases/latest)
![Tauri 2](https://img.shields.io/badge/Tauri%202-FFC131?style=for-the-badge&logo=tauri&logoColor=black)
![Rust](https://img.shields.io/badge/Rust-000000?style=for-the-badge&logo=rust&logoColor=white)
![.NET 10](https://img.shields.io/badge/.NET%2010-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=csharp&logoColor=white)
![React 19](https://img.shields.io/badge/React%2019-61DAFB?style=for-the-badge&logo=react&logoColor=black)
![TypeScript](https://img.shields.io/badge/TypeScript-3178C6?style=for-the-badge&logo=typescript&logoColor=white)
![PostgreSQL 16](https://img.shields.io/badge/PostgreSQL%2016-336791?style=for-the-badge&logo=postgresql&logoColor=white)
![Redis 7](https://img.shields.io/badge/Redis%207-DC382D?style=for-the-badge&logo=redis&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-2496ED?style=for-the-badge&logo=docker&logoColor=white)

Plataforma bancária digital e motor transacional de alta fidelidade desenvolvido como **Monorepo Unificado**:
- 🌐 **Web SPA:** React 19 + TypeScript + Vite em produção na nuvem.
- 💻 **Desktop Nativo:** Aplicativo Windows de alta performance em **Tauri 2 (Rust) + React 19**.
- ⚙️ **Backend Core:** Microsserviços em **C# / .NET 10** com Clean Architecture, PostgreSQL 16 e Redis 7.

---

### 🌐 Demonstração Online & Downloads

- **Aplicação Web em Produção:** 👉 **[https://bank.vortexsoftware.tech](https://bank.vortexsoftware.tech)**
- **Documentação Interativa Swagger:** 👉 **[https://bank.vortexsoftware.tech/swagger](https://bank.vortexsoftware.tech/swagger)**
- **Instalador Desktop Windows:** 👉 **[Baixar Vortex Bank x64 (.exe)](https://github.com/joaop-gregorioDS/vortex-bank/releases/latest)**

---

## Estrutura do Monorepo

```text
vortex-bank/
├── src/                # Backend Microsserviços .NET 10 (Clean Architecture)
│   ├── Services/
│   │   ├── AuthService/         # Identidade, Sessão e Chaves Assimétricas ECDsa
│   │   └── TransactionsService/ # Livro-razão (Ledger), Pix, TED, Boletos e Cartões
├── desktop/            # Aplicativo Desktop Nativo Windows (Tauri 2 + Rust + React 19)
│   ├── src/                     # Telas do Desktop (Login, Pix, Extrato, Cartões, Invest)
│   ├── src-tauri/               # Backend nativo Rust (processo de comunicação segura)
│   └── DIRETRIZES.md            # Especificações de design e regras de negócio do app
├── web/                # Frontend Web SPA (React 19 + Vite)
├── tests/              # Bateria de testes automatizados (.NET xUnit + Testcontainers)
├── docker/             # Configurações de containerização, Nginx Gateway e deploy VPS
└── Bankcore.slnx       # Solução moderna .NET 10
```

---

## Principais Recursos & Capacidades

- **Microsserviço de Autenticação (`AuthService`):**
  - Emissão e validação de tokens JWT assimétricos (chaves pública e privada ECDsa NIST P-256).
  - Gestão de credenciais, controle de sessões e autorização granular.
- **Motor de Transações & Ledger (`TransactionsService`):**
  - Livro-razão (*double-entry bookkeeping*) com garantia de consistência ACID.
  - Transferências Pix instantâneas, extratos detalhados e saldos consolidados.
  - Cache de alta performance no Redis 7 e persistência relacional no PostgreSQL 16.
- **Frontend SPA Bancário Moderno (`web`):**
  - Desenvolvido em **React 19** com **TypeScript** e **Vite**.
  - Dashboard financeiro responsivo com extrato, Pix, cartões e investimentos.
  - Geração e exportação nativa de comprovantes e extrato em PDF client-side (`jsPDF`).
- **Aplicativo Desktop Nativo (`desktop`):**
  - Construído com **Tauri 2** e **Rust**, garantindo baixíssimo consumo de memória RAM (< 40 MB).
  - Janela nativa com barra lateral fixa, atalhos rápidos e acesso ao Swagger.
  - Login rápido com os titulares de demonstração:
    - **Ana Ribeiro:** `ana.ribeiro@vortexbank.demo`
    - **Bruno Lima:** `bruno.lima@vortexbank.demo`

---

## Arquitetura de Comunicação

```text
     Navegador Web (HTTPS)               Cliente Desktop Nativo (Tauri 2)
              │                                         │
              ▼                                         ▼
   bank.vortexsoftware.tech                 Processo Nativo Rust (IPC)
              │                                         │
              └────────────────────┬────────────────────┘
                                   │
                                   ▼
                   Nginx Gateway Reverso (:5060)
                                   │
                   ┌───────────────┴───────────────┐
                   ▼                               ▼
         AuthService (:8080)             TransactionsService (:8080)
                   │                               │
                   ▼                               ▼
         PostgreSQL (auth_db)            PostgreSQL (trans_db) + Redis 7
```

---

## Executar Localmente

### Pré-requisitos
- .NET 10 SDK
- Node.js 20+ e npm
- Rust e Cargo (para compilar o app desktop)
- Docker e Docker Compose

### 1. Iniciar Infraestrutura e Bancos de Dados
```bash
cd docker
docker compose up -d
```

### 2. Iniciar os Microsserviços .NET
```bash
dotnet run --project src/Services/AuthService/Api/Bankcore.Auth.Api.csproj
dotnet run --project src/Services/TransactionsService/Api/Bankcore.Transactions.Api.csproj
```

### 3. Iniciar o Frontend Web (Navegador)
```bash
cd web
npm install
npm run dev
# Acesse: http://localhost:5173
```

### 4. Iniciar o Aplicativo Desktop (Tauri)
```bash
cd desktop
npm install
npm run tauri dev
```

---

## Licença

MIT © [João Paulo Gregório](https://github.com/joaop-gregorioDS) · [Vortex Software](https://vortexsoftware.tech)
