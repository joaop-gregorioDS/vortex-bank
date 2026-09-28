# Vortex Bank — diretrizes do aplicativo Tauri

Este arquivo é o ponto de partida de um chat novo. O aplicativo desktop ainda não existe. A API e o site local já existem e não devem ser reescritos.

## O que construir

Um cliente desktop do Vortex Bank, em Tauri 2, para Windows. O processo Rust fala com a API que já está em `K:\A developer-local\vortex-bank`. A janela mostra a mesma experiência do site local, não um banco novo.

Não implementar ledger, senha, PIX, boleto ou cartão em Rust. Essas regras já estão na API. O aplicativo só autentica, chama os endpoints e desenha a tela.

Pasta deste encargo: `K:\A developer-local\vortex-bank-tauri`. O código do app nasce aqui, ao lado da API, não dentro de `vortex-bank`.

## Como subir a API antes de testar o app

Na pasta `K:\A developer-local\vortex-bank\docker`:

```text
docker compose up -d
```

| Serviço | URL local |
| --- | --- |
| Auth | http://127.0.0.1:5081 |
| Transações | http://127.0.0.1:5082 |
| Saúde | GET /health em cada uma, resposta `{ "status": "ok" }` |
| Swagger para avaliador | http://127.0.0.1:8080/swagger |

O site que já existe, só como referência visual, sobe com `npm run dev` em `vortex-bank\web` e abre em http://localhost:5173.

## Autenticação

POST http://127.0.0.1:5081/login

```json
{ "email": "ana.ribeiro@vortexbank.demo", "password": "Ana-demo-2026" }
```

A resposta JSON traz `accessToken`, `userId`, `name`, `email`, `cpf` e `accessExpiresUtc`. O refresh vai no cookie `bankcore_refresh`, HttpOnly, caminho `/api/auth`. Um cliente nativo não está nesse caminho. No app, guardar o cookie que vier no `Set-Cookie` e reenviá-lo em POST /refresh. Se isso falhar, alterar a API o mínimo necessário para devolver o refresh também no JSON, só para o cliente desktop. Não trocar o login da web.

Demais chamadas: cabeçalho `Authorization: Bearer <accessToken>`.

Titulares da tela de entrada, só estes dois:

| Nome | CPF na tela | E-mail enviado à API | Senha |
| --- | --- | --- | --- |
| Ana Ribeiro | 390.533.447-05 | ana.ribeiro@vortexbank.demo | Ana-demo-2026 |
| Bruno Lima | 529.982.247-25 | bruno.lima@vortexbank.demo | Bruno-demo-2026 |

A tela não pede e-mail. O clique no titular entra direto. CPF e senha digitados servem só para estes dois: o app traduz o CPF para o e-mail da tabela e chama `/login`. Carla Mendes existe no banco de demonstração e não aparece no login.

POST /me/provision em transações, com o token, body `{ "name", "cpf" }`, logo depois do login. Para Ana e Bruno o cadastro já existe; a chamada é idempotente.

## Endpoints que o app usa

Base: http://127.0.0.1:5082. Todo POST de dinheiro exige o cabeçalho `Idempotency-Key` com um UUID novo. Repetir a mesma chave não lança de novo.

| Uso na tela | Método e caminho |
| --- | --- |
| Painel | GET /home |
| Extrato | GET /statement?accountId= |
| Comprovante | GET /receipts/{journalId} |
| Enviar Pix | POST /pix body `{ "key", "amount" }` |
| Pagar boleto | POST /boletos/pay body `{ "line" }` |
| Pagar fatura | POST /cards/invoices/{invoiceId}/pay |
| Compra no cartão | POST /cards/purchases body `{ "cardId", "merchant", "amount" }` |

Merchant aceito: Mercado, Combustível, Farmácia, Streaming.

Não colocar na interface, mesmo que a API ainda responda: TED (`/ted`), transferência para poupança (`/savings`) e avançar dia útil (`/clock/advance`).

GET /home devolve contas, cartões, chaves Pix, boletos, TED internas e os últimos lançamentos. O extrato inclui `createdAt` em UTC. Mostrar data e hora em `America/Sao_Paulo`.

## Telas

Janela larga, barra lateral fixa à esquerda. Item sob o mouse e item da página atual: fundo `#e11d48`, texto branco. O mesmo vale para Sair.

Ordem do menu:

1. Início
2. Central Pix
3. Extrato
4. Central de Pagamentos
5. Central de Cartões
6. Vortex Invest
7. Meu perfil e ajustes

Topo da área logada: indicador de ledger conectado, botão **A+ Fonte** (três tamanhos, 15, 17 e 19 px, ciclando) e atalho **Swagger** abrindo http://127.0.0.1:8080/swagger.

Entrada: cartão centralizado, escudo, título VortexBank com Bank em magenta, dois titulares em um clique, e abaixo CPF mais senha. Sem e-mail. Sem Carla. Sem poupança.

Início: saudação, saldo da corrente, soma dos boletos em aberto, atalhos e cartão. Sem botão de avançar o dia.

Central Pix: blocos Pagar, Receber e Consultar. Só **Fazer um Pix** grava no ledger. QR Code, copia e cola, presente e golpe são vitrine e precisam dizer isso.

Extrato: abas conta corrente e poupança, meses, filtro, grupos por dia com saldo do dia, lançamento em duas linhas (título e horário mais descrição), valor à direita. Débito em vermelho, crédito em verde. PDF no mesmo recorte, gerado no aplicativo, com cabeçalho Vortex Bank, CPF, agência 0001, conta, emissão, grupos por dia e totais de entradas e saídas. Texto do rodapé: documento de demonstração, não é extrato de instituição real. Não usar logotipo nem dados do Banco do Brasil.

Central de Pagamentos: saldo, total em aberto, lista com Pagar. Pagar grava no ledger. Débito automático e teto de R$ 50.000,00 são vitrine.

Central de Cartões: crédito com limite e fatura, débito com CVV sob demanda. Pagar fatura e lançar compra gravam. Ajustar limite, bloquear e gerar outro cartão não gravam.

Vortex Invest: números de cenário, iguais para qualquer cliente. Patrimônio ilustrativo R$ 45.890,20, referência 104% do CDI, liquidez R$ 25.000,00, produtos e simulador de 12 meses. Investir e novo aporte não alteram saldo.

Perfil: nome, CPF, e-mail e conta reais. Telefone, endereço, biometria, token, troca de senha, outros dispositivos e dados da empresa são vitrine. Não escrever que a conta está protegida pelo SPI ou pelo BACEN.

## Aparência

Paleta já fechada, a da Vortex Software:

| Uso | Cor |
| --- | --- |
| Ação, hover da barra, débito | `#e11d48` |
| Ação pressionada | `#be123c` |
| Texto | `#1c1418` |
| Texto secundário | `#6d6168` |
| Fundo da aplicação | `#f7f2f4` |
| Cartão | `#ffffff` |
| Crédito | `#157a45` |
| Faixa e degradê | `#fb7185` para `#e11d48` para `#9f1239` |

Fonte: Manrope, com Segoe UI e Arial por baixo. Valores em real, `pt-BR`. Não usar o dourado, o bege carbono nem o azul do Banco do Brasil.

Faixa ou frase permanente: ambiente simulado, nenhum valor é real.

## Fora deste aplicativo

Não recriar a API, o Docker de produção, o Nginx, o ledger nem o site React, salvo o ajuste mínimo de refresh para cliente nativo, se o cookie não bastar. Não publicar na VPS neste chat. Não criar README no repositório da API. O registro do que é vitrine e do que grava partida, para o GitHub, fica para um pedido separado.
