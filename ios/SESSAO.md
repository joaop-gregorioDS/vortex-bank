# Sessão Windows já gravada

Chat em que o Android foi construído e esta pasta iOS foi preparada.

| | |
| --- | --- |
| Máquina | Windows, pasta `K:\A developer-local\vortex-bank-android` |
| Identificador | `01a0e3f8-f4b1-7831-a8db-32e7363f78c7` |
| Título | Vortex Bank Android app and Galaxy APK |
| Retomar neste PC | na pasta do Android, `grok --resume 01a0e3f8-f4b1-7831-a8db-32e7363f78c7` |

O Grok grava a conversa sozinho em `C:\Users\DevUser\.grok\sessions\`. Este arquivo é a cópia leve para levar ao Mac. O histórico bruto não entra nesta pasta.

## O que essa sessão fechou

O aplicativo Android nativo está em `K:\A developer-local\vortex-bank-android`, pacote `com.vortexsoftware.vortexbank`, Kotlin e Jetpack Compose. A API, o Docker, o site e o cliente Windows não foram reescritos.

No emulador a API é `http://10.0.2.2:5081` e `http://10.0.2.2:5082`. No Galaxy S22 Plus o app usa `http://127.0.0.1` nas mesmas portas, com `adb reverse` no cabo. O Docker segue escutando só `127.0.0.1`.

APK de teste: `K:\A developer-local\vortex-bank-android\VortexBank.apk`.

A barra aceita foi Início, Pix, Extrato, Cartões e Mais. Só Pix, boleto, fatura e compra gravam no ledger. O cookie `bankcore_refresh` é lido do `Set-Cookie` e reenviado no cabeçalho `Cookie`, porque o path `/api/auth` não casa com `/refresh`.

## No MacBook Air M1

Levar esta pasta. O chat novo lê `DIRETRIZES.md` e constrói o iOS aqui. Não refazer o Android.
