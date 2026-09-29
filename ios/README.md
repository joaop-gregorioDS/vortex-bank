# Vortex Bank — iOS App

Aplicativo nativo para iOS do **Vortex Bank**, desenvolvido em **Swift** e **SwiftUI** para iPhone.

O app consome a API de produção do Vortex Bank hospedada na nuvem (`https://bank.vortexsoftware.tech`), suportando autenticação via Bearer token, renovação de sessão via cookie HttpOnly (`bankcore_refresh`), idempotência financeira e design system proprietário da Vortex Software.

---

## 📱 Tecnologias & Arquitetura

* **Linguagem**: Swift 5.10+
* **Interface**: SwiftUI nativo (sem webviews ou wrappers híbridos)
* **Arquitetura de Estado**: `@Observable` (`BankStore`) + Actor (`APIClient`)
* **Rede**: `URLSession` nativo, com tratamento automático de 401 (refresh token) e propagação manual do cookie de renovação
* **Relatórios**: Geração de extrato em PDF nativo via `UIGraphicsPDFRenderer`
* **Tipografia**: Manrope com controle dinâmico de tamanho de fonte (A+ Fonte: 15, 17 e 19 pt)

---

## 🧭 Estrutura da API & Rotas

O aplicativo se conecta ao Nginx Gateway de produção em `https://bank.vortexsoftware.tech`:

### Autenticação (`/api/auth/`)
* `POST /api/auth/login` — Autenticação de titular e obtenção de Bearer token + cookie de refresh
* `POST /api/auth/refresh` — Renovação automática de sessão
* `POST /api/auth/logout` — Encerramento de sessão
* `GET /api/auth/health` — Verificação de integridade

### Transações & Ledger (`/api/transactions/`)
* `GET /api/transactions/home` — Painel consolidado (saldos, faturas, boletos e lançamentos recentes)
* `POST /api/transactions/me/provision` — Provisionamento pós-login
* `GET /api/transactions/statement?accountId={id}` — Extrato detalhado com agrupamento por dia
* `GET /api/transactions/receipts/{journalId}` — Comprovante individual de lançamento
* `POST /api/transactions/pix` — Envio de Pix (Header `Idempotency-Key`)
* `POST /api/transactions/boletos/pay` — Pagamento de boletos (Header `Idempotency-Key`)
* `POST /api/transactions/cards/invoices/{invoiceId}/pay` — Pagamento de fatura de cartão de crédito
* `POST /api/transactions/cards/purchases` — Lançamento de compras no cartão de crédito
* `GET /api/transactions/health` — Health check do serviço de transações

### Documentação
* `https://bank.vortexsoftware.tech/swagger` — Swagger UI

---

## ⚖️ Ledger vs. Vitrine (Demonstração)

Em conformidade com a especificação do produto, o aplicativo diferencia com clareza o que efetivamente grava no ledger contábil do que é vitrine ilustrativa:

| Ação | Tipo | Descrição |
| :--- | :---: | :--- |
| **Fazer Pix** | **Grava no Ledger** | Envia Pix com chave e valor, gerando lançamento real |
| **Pagar Boleto** | **Grava no Ledger** | Liquida o boleto pelo código de barras/linha digitável |
| **Pagar Fatura** | **Grava no Ledger** | Liquida a fatura aberta do cartão de crédito |
| **Lançar Compra** | **Grava no Ledger** | Lança despesa no cartão nos estabelecimentos aceitos |
| **Pix QR Code / Copia e Cola / Presente** | *Vitrine* | Indicativo ilustrativo, não altera o saldo |
| **Débito Automático / Teto de R$ 50k** | *Vitrine* | Demonstração funcional sem efeito contábil |
| **Ajustar Limite / Bloquear Cartão** | *Vitrine* | Recursos visuais de demonstração |
| **Vortex Invest** | *Vitrine* | Simulador de 12 meses e carteira ilustrativa |
| **Meu Perfil e Ajustes** | *Vitrine* | Demonstração de biometria, token e segurança |

---

## 👥 Titulares de Demonstração

| Titular | CPF | E-mail | Senha |
| :--- | :--- | :--- | :--- |
| **Ana Ribeiro** | `390.533.447-05` | `ana.ribeiro@vortexbank.demo` | `Ana-demo-2026` |
| **Bruno Lima** | `529.982.247-25` | `bruno.lima@vortexbank.demo` | `Bruno-demo-2026` |

---

## 🛠️ Como Executar o Projeto

1. Abra o projeto no Xcode:
   ```bash
   open VortexBank.xcodeproj
   ```
2. Selecione o target `VortexBank` e um simulador (ex: *iPhone 16 Pro*).
3. Pressione `Cmd + R` para compilar e executar.
4. Para rodar os testes unitários de regras e URLs:
   ```bash
   Cmd + U
   ```
