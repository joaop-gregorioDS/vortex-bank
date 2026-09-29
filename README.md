# Vortex Bank · Next-Gen Financial Core & Monorepo

[![Live Demo Web](https://img.shields.io/badge/Live%20Demo-bank.vortexsoftware.tech-0A84FF?style=for-the-badge&logo=google-cloud&logoColor=white)](https://bank.vortexsoftware.tech)
![iOS](https://img.shields.io/badge/iOS%2017%2B-SwiftUI-000000?style=for-the-badge&logo=apple&logoColor=white)
![Swift](https://img.shields.io/badge/Swift%205.10%2B-F05138?style=for-the-badge&logo=swift&logoColor=white)
[![Download Android](https://img.shields.io/badge/Download%20APK-Android%20(Kotlin%20Compose)-3DDC84?style=for-the-badge&logo=android&logoColor=white)](https://github.com/joaop-gregorioDS/vortex-bank/releases/latest)
[![Download Desktop](https://img.shields.io/badge/Download%20App-Windows%20x64%20(Tauri%202)-E11D48?style=for-the-badge&logo=windows&logoColor=white)](https://github.com/joaop-gregorioDS/vortex-bank/releases/latest)
![Android](https://img.shields.io/badge/Android-3DDC84?style=for-the-badge&logo=android&logoColor=white)
![Kotlin](https://img.shields.io/badge/Kotlin%202.4-7F52FF?style=for-the-badge&logo=kotlin&logoColor=white)
![Jetpack Compose](https://img.shields.io/badge/Jetpack%20Compose-4285F4?style=for-the-badge&logo=jetpackcompose&logoColor=white)
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
- 📱 **Mobile Nativo Android:** Aplicativo nativo em **Kotlin + Jetpack Compose + Material 3** conectado diretamente à nuvem de produção.
- 🍎 **Mobile Nativo iOS:** Aplicativo nativo em **Swift + SwiftUI** para iPhone conectado diretamente à nuvem de produção.
- 💻 **Desktop Nativo:** Aplicativo Windows de alta performance em **Tauri 2 (Rust) + React 19**.
- ⚙️ **Backend Core:** Microsserviços em **C# / .NET 10** com Clean Architecture, PostgreSQL 16 e Redis 7.

---

### 🌐 Demonstração Online & Downloads

- **Aplicação Web em Produção:** 👉 **[https://bank.vortexsoftware.tech](https://bank.vortexsoftware.tech)**
- **Documentação Interativa Swagger:** 👉 **[https://bank.vortexsoftware.tech/swagger](https://bank.vortexsoftware.tech/swagger)**
- **Instalador Desktop Windows:** 👉 **[Baixar Vortex Bank x64 (.exe)](https://github.com/joaop-gregorioDS/vortex-bank/releases/latest)**
- **Aplicativo Mobile Android:** 👉 **[Baixar Vortex Bank APK (.apk)](https://github.com/joaop-gregorioDS/vortex-bank/releases/latest)**

---

## Estrutura do Monorepo

```text
vortex-bank/
├── src/                # Backend Microsserviços .NET 10 (Clean Architecture)
│   ├── Services/
│   │   ├── AuthService/         # Identidade, Sessão e Chaves Assimétricas ECDsa
│   │   └── TransactionsService/ # Livro-razão (Ledger), Pix, TED, Boletos e Cartões
├── android/            # Aplicativo Mobile Nativo Android (Kotlin + Jetpack Compose)
│   ├── app/src/main/            # Telas Compose, ViewModels, Cliente HTTP OkHttp e PDF nativo
│   └── app/src/test/            # Testes unitários automatizados (regras de ledger e cartões)
├── ios/                # Aplicativo Mobile Nativo iOS (Swift + SwiftUI)
│   ├── VortexBank/              # Telas SwiftUI, ViewModels, APIClient, Design System Manrope e PDF nativo
│   └── VortexBankTests/         # Testes unitários automatizados de regras e URLs de produção
├── desktop/            # Aplicativo Desktop Nativo Windows (Tauri 2 + Rust + React 19)
│   ├── src/                     # Telas do Desktop (Login, Pix, Extrato, Cartões, Invest)
│   └── src-tauri/               # Backend nativo Rust (processo de comunicação segura)
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
- **Aplicativo Mobile Nativo Android (`android`):**
  - Desenvolvido 100% nativo com **Kotlin** e **Jetpack Compose** (Material 3).
  - Telas reativas: Home com saldos e limites, Central Pix, Extrato com exportação nativa em PDF via `android.graphics.pdf.PdfDocument`, Gestão de Cartões, Boletos e Simulador Vortex Invest.
  - Conexão direta com a nuvem de produção (`https://bank.vortexsoftware.tech`) com suporte a chave de idempotência (`Idempotency-Key`).
- **Aplicativo Mobile Nativo iOS (`ios`):**
  - Desenvolvido 100% nativo com **Swift** e **SwiftUI** para iPhone (iOS 17+).
  - Telas reativas: Home com saldos e faturas, Central Pix, Extrato com agrupamento diário e exportação nativa em PDF via `UIGraphicsPDFRenderer`, Central de Cartões com CVV sob demanda, Central de Pagamentos e Simulador Vortex Invest.
  - Conexão direta com a nuvem de produção (`https://bank.vortexsoftware.tech`) com suporte a token Bearer, cookie HttpOnly `bankcore_refresh` e idempotência financeira (`Idempotency-Key`).
- **Aplicativo Desktop Nativo (`desktop`):**
  - Construído com **Tauri 2** e **Rust**, garantindo baixíssimo consumo de memória RAM (< 40 MB).
  - Janela nativa com barra lateral fixa, atalhos rápidos e acesso ao Swagger.
- **Frontend SPA Bancário Moderno (`web`):**
  - Desenvolvido em **React 19** com **TypeScript** e **Vite**.
  - Dashboard financeiro responsivo com extrato, Pix, cartões e investimentos.
  - Geração e exportação nativa de comprovantes e extrato em PDF client-side (`jsPDF`).
- **Titulares de Demonstração em Todas as Plataformas:**
  - **Ana Ribeiro:** `ana.ribeiro@vortexbank.demo` (Senha: `Ana-demo-2026`)
  - **Bruno Lima:** `bruno.lima@vortexbank.demo` (Senha: `Bruno-demo-2026`)

---

## Arquitetura de Comunicação

```text
  Navegador Web (HTTPS)       Cliente Desktop (Tauri 2)        App Android Nativo (Kotlin)
           │                             │                                 │
           ▼                             ▼                                 ▼
 bank.vortexsoftware.tech    Processo Nativo Rust (IPC)         OkHttp / Coroutines (HTTPS)
           │                             │                                 │
           └─────────────────────────────┼─────────────────────────────────┘
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
- Android SDK / JDK 17+ (para o app Android)
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

### 5. Executar o Aplicativo Mobile Android
```bash
cd android
./gradlew test
./gradlew assembleDebug
# Ou abra o diretório android/ no Android Studio
```

### 6. Executar o Aplicativo Mobile iOS
```bash
cd ios
open VortexBank.xcodeproj
# Selecione o target VortexBank e execute no simulador com Cmd + R (ou Cmd + U para rodar os testes de regras)
```


---

## Licença

MIT © [João Paulo Gregório](https://github.com/joaop-gregorioDS) · [Vortex Software](https://vortexsoftware.tech)
