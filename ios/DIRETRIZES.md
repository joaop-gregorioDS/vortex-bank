# Vortex Bank — diretrizes do aplicativo iOS nativo

Este arquivo é o ponto de partida de um chat novo, no MacBook Air M1. O aplicativo iOS ainda não existe. A API, o site, o cliente Windows e o aplicativo Android já existem e não devem ser reescritos.

No Mac, abra a pasta que contém este arquivo e peça para ler a diretriz. O código nasce aqui.

## O que construir

Um aplicativo iPhone nativo do Vortex Bank, em Swift e SwiftUI. Não é WKWebView, não é o site dentro do Safari, não é React Native, Flutter, Kotlin Multiplatform nem Catalyst como produto. A interface é desenhada no iPhone. Quem fala com a API é o código Swift, com `URLSession`.

Não implementar ledger, senha, PIX, boleto ou cartão no aplicativo. Essas regras já estão na API. O aplicativo só autentica, chama os endpoints e desenha a tela.

A pasta deste encargo é a pasta deste arquivo. No Windows ela foi criada em `K:\A developer-local\vortex-bank-ios`. No Mac o caminho muda; o que vale é esta pasta. O código não entra em `vortex-bank`, `vortex-bank-tauri` nem `vortex-bank-android`.

Identificador: `com.vortexsoftware.vortexbank`.

Destino deste chat: simulador de iPhone no MacBook Air M1, arm64. Aparelho físico fica para um pedido separado.

Referências, sem copiar o código de nenhuma:

| Peça | Papel |
| --- | --- |
| Site em `vortex-bank/web` | Visual, inclusive o modo estreito, com barra embaixo |
| Cliente Windows em `vortex-bank-tauri` | Comportamento já aceito, inclusive cookie de refresh |
| Android em `vortex-bank-android` | Comportamento já aceito no celular. A barra curta é Início, Pix, Extrato, Cartões e Mais |

Se algum desses checkouts não estiver ao lado no Mac, a diretriz basta. Não recriar o que ela não pede.

## Como subir a API antes de testar o app

A API continua a mesma. No Mac, na pasta `docker` do checkout `vortex-bank`:

```text
docker compose up -d
```

| Serviço | No Mac e no simulador de iPhone |
| --- | --- |
| Auth | http://127.0.0.1:5081 |
| Transações | http://127.0.0.1:5082 |
| Saúde | GET /health em cada uma, resposta `{ "status": "ok" }` |
| Swagger para avaliador | http://127.0.0.1:8080/swagger, aberto no Safari |

O simulador de iOS usa a rede do Mac. `127.0.0.1` no simulador é o loopback do Mac. Não usar `10.0.2.2`: esse endereço é só do emulador Android.

Auth e transações ficam publicados só em `127.0.0.1`. Não mudar o Docker para escutar a rede.

O iOS bloqueia HTTP claro. No `Info.plist`, permitir carga insegura só para local, com `NSAllowsLocalNetworking` e exceção para `localhost` e `127.0.0.1`. Não liberar HTTP arbitrário.

O site de referência sobe com `npm run dev` em `vortex-bank/web` e abre em http://localhost:5173.

## Autenticação

POST /login no serviço de auth.

```json
{ "email": "ana.ribeiro@vortexbank.demo", "password": "Ana-demo-2026" }
```

A resposta JSON traz `accessToken`, `userId`, `name`, `email`, `cpf` e `accessExpiresUtc`. O refresh vai no cookie `bankcore_refresh`, HttpOnly, caminho `/api/auth`. Esse caminho não casa com POST /refresh no host direto. O `URLSession` guarda o cookie e não o reenvia para `/refresh` nem para `/logout`.

Ler `bankcore_refresh` em `HTTPCookieStorage.shared.cookies` pelo nome, ignorando o path. Reenviar o valor no cabeçalho `Cookie` em POST /refresh e POST /logout. Não contar com o jar automático. Isso já foi provado no cliente Windows e no Android. Não alterar a API e não trocar o login da web.

Demais chamadas: cabeçalho `Authorization: Bearer <accessToken>`. Se uma chamada autenticada voltar 401, renovar uma vez e repetir. No POST de dinheiro, repetir com a mesma `Idempotency-Key`. O token fica na camada Swift. A tela só recebe nome, e-mail, CPF e o que a API devolver para desenhar.

Titulares da tela de entrada, só estes dois:

| Nome | CPF na tela | E-mail enviado à API | Senha |
| --- | --- | --- | --- |
| Ana Ribeiro | 390.533.447-05 | ana.ribeiro@vortexbank.demo | Ana-demo-2026 |
| Bruno Lima | 529.982.247-25 | bruno.lima@vortexbank.demo | Bruno-demo-2026 |

A tela não pede e-mail. O toque no titular entra direto. CPF e senha digitados servem só para estes dois: o app traduz o CPF para o e-mail da tabela e chama `/login`. Carla Mendes existe no banco de demonstração e não aparece no login.

POST /me/provision em transações, com o token, body `{ "name", "cpf" }`, logo depois do login. Para Ana e Bruno o cadastro já existe; a chamada é idempotente. O CPF enviado é o que o login devolveu.

## Endpoints que o app usa

Base de transações: http://127.0.0.1:5082. Todo POST de dinheiro exige o cabeçalho `Idempotency-Key` com um UUID novo. Repetir a mesma chave não lança de novo.

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

GET /home devolve contas, cartões, chaves Pix, boletos, TED internas e os últimos lançamentos. Ignorar o campo `teds` na tela. O extrato inclui `createdAt` em UTC, no formato `2026-09-27T13:47:13.941516Z`. Mostrar data e hora em `America/Sao_Paulo`. Valores em real, `pt-BR`. Conta corrente tem `kind` `corrente`. Poupança tem `kind` `poupanca`. Agência na tela: 0001.

Chave Pix de CPF vai para a API só com os 11 dígitos. Valor maior que zero, com duas casas.

Saldo do dia no extrato: ordenar do lançamento mais novo para o mais antigo por `createdAt`. O saldo mostrado no dia é o saldo da conta depois dos lançamentos daquele dia. Dentro do dia, desenhar do mais antigo para o mais novo.

## Telas

iPhone em pé. Barra fixa embaixo, como o site estreito e como o Android já aceito. Rótulos curtos: Início, Pix, Extrato, Cartões e Mais. Item sob o toque e item da página atual: fundo `#e11d48`, texto branco. O mesmo vale para Sair. A barra fica acima da área segura.

Ordem da barra:

1. Início
2. Pix
3. Extrato
4. Cartões
5. Mais

Mais abre Central de Pagamentos, Vortex Invest, Meu perfil e ajustes, e Sair.

Topo da área logada: indicador de ledger conectado, botão **A+ Fonte** (três tamanhos, 15, 17 e 19, ciclando) e atalho **Swagger** abrindo http://127.0.0.1:8080/swagger no Safari.

Entrada: cartão centralizado, escudo, título VortexBank com Bank em magenta, dois titulares em um toque, e abaixo CPF mais senha. Sem e-mail. Sem Carla. Sem poupança.

Início: saudação, saldo da corrente, soma dos boletos em aberto (`status` `aberto` e `mine` falso), atalhos e cartão. Sem botão de avançar o dia.

Central Pix: blocos Pagar, Receber e Consultar. Só **Fazer um Pix** grava no ledger. QR Code, copia e cola, presente e golpe são vitrine e precisam dizer isso.

Extrato: abas conta corrente e poupança, meses, filtro, grupos por dia com saldo do dia, lançamento em duas linhas (título e horário mais descrição), valor à direita. Débito em vermelho, crédito em verde. PDF no mesmo recorte, gerado no aplicativo com `UIGraphicsPDFRenderer`, com cabeçalho Vortex Bank, CPF, agência 0001, conta, emissão, grupos por dia e totais de entradas e saídas. Texto do rodapé: documento de demonstração, não é extrato de instituição real. Não usar logotipo nem dados do Banco do Brasil. O arquivo sai pela folha de compartilhamento do iOS.

Central de Pagamentos: saldo, total em aberto, lista com Pagar. Pagar grava no ledger. Débito automático e teto de R$ 50.000,00 são vitrine.

Central de Cartões: crédito com limite e fatura, débito com CVV sob demanda. Pagar fatura e lançar compra gravam. Ajustar limite, bloquear e gerar outro cartão não gravam.

Vortex Invest: números de cenário, iguais para qualquer cliente. Patrimônio ilustrativo R$ 45.890,20, referência 104% do CDI, liquidez R$ 25.000,00, produtos e simulador de 12 meses. Investir e novo aporte não alteram saldo. Neste cenário, 12 meses rendem o aporte vezes 1,1144. A comparação com a poupança antiga é o aporte vezes 1,0617.

Perfil: nome, CPF, e-mail e conta reais. Telefone, endereço, biometria, token, troca de senha, outros dispositivos e dados da empresa são vitrine. Não escrever que a conta está protegida pelo SPI ou pelo BACEN.

## Aparência

Paleta já fechada, a da Vortex Software:

| Uso | Cor |
| --- | --- |
| Ação, item atual da barra, débito | `#e11d48` |
| Ação pressionada | `#be123c` |
| Texto | `#1c1418` |
| Texto secundário | `#6d6168` |
| Fundo da aplicação | `#f7f2f4` |
| Cartão | `#ffffff` |
| Crédito | `#157a45` |
| Faixa e degradê | `#fb7185` para `#e11d48` para `#9f1239` |

Fonte: Manrope, com a fonte do sistema por baixo. Não usar o dourado, o bege carbono nem o azul do Banco do Brasil.

Faixa ou frase permanente: ambiente simulado, nenhum valor é real.

## Fora deste aplicativo

Não recriar a API, o Docker, o Nginx, o ledger, o site React, o cliente Windows nem o Android. Não publicar na VPS neste chat. Não criar README no repositório da API. O registro do que é vitrine e do que grava partida, para o GitHub, fica para um pedido separado.
