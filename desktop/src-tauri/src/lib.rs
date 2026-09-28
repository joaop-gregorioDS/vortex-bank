use reqwest::header::{HeaderMap, COOKIE, SET_COOKIE};
use reqwest::{Client, Method, StatusCode};
use serde::{Deserialize, Serialize};
use serde_json::{json, Value};
use std::sync::Mutex;
use std::time::Duration;
use tauri::State;
use uuid::Uuid;

const AUTH: &str = "http://127.0.0.1:5081";
const TRANSACTIONS: &str = "http://127.0.0.1:5082";

#[derive(Clone, Serialize)]
#[serde(rename_all = "camelCase")]
struct Profile {
    user_id: String,
    name: String,
    email: String,
    cpf: String,
}

#[derive(Deserialize)]
#[serde(rename_all = "camelCase")]
struct AuthBody {
    user_id: String,
    name: String,
    email: String,
    cpf: String,
    access_token: String,
}

#[derive(Default)]
struct Session {
    access_token: Option<String>,
    refresh_token: Option<String>,
    profile: Option<Profile>,
}

struct Bank {
    http: Client,
    session: Mutex<Session>,
}

impl Bank {
    fn new() -> Self {
        let http = Client::builder()
            .timeout(Duration::from_secs(20))
            .build()
            .expect("cliente HTTP");
        Self {
            http,
            session: Mutex::new(Session::default()),
        }
    }

    fn clear(&self) {
        if let Ok(mut session) = self.session.lock() {
            *session = Session::default();
        }
    }

    fn profile(&self) -> Result<Option<Profile>, String> {
        let session = self
            .session
            .lock()
            .map_err(|_| "Estado da sessão indisponível.".to_string())?;
        Ok(session.profile.clone())
    }

    fn access_token(&self) -> Result<String, String> {
        self.session
            .lock()
            .map_err(|_| "Estado da sessão indisponível.".to_string())?
            .access_token
            .clone()
            .ok_or_else(|| "Entre de novo para continuar.".to_string())
    }

    async fn login(&self, email: String, password: String) -> Result<Profile, String> {
        let response = self
            .http
            .post(format!("{AUTH}/login"))
            .json(&json!({ "email": email, "password": password }))
            .send()
            .await
            .map_err(net_error)?;
        let profile = self.store(response).await?;
        if let Err(error) = self
            .authed(
                Method::POST,
                &format!("{TRANSACTIONS}/me/provision"),
                Some(json!({ "name": profile.name, "cpf": profile.cpf })),
                None,
            )
            .await
        {
            self.clear();
            return Err(error);
        }
        Ok(profile)
    }

    async fn logout(&self) -> Result<(), String> {
        let refresh = self
            .session
            .lock()
            .ok()
            .and_then(|session| session.refresh_token.clone());
        if let Some(refresh) = refresh {
            let _ = self
                .http
                .post(format!("{AUTH}/logout"))
                .header(COOKIE, format!("bankcore_refresh={refresh}"))
                .send()
                .await;
        }
        self.clear();
        Ok(())
    }

    async fn refresh(&self) -> Result<(), String> {
        let refresh = self
            .session
            .lock()
            .map_err(|_| "Estado da sessão indisponível.".to_string())?
            .refresh_token
            .clone()
            .ok_or_else(|| "Sessão expirada. Entre de novo.".to_string())?;
        let response = self
            .http
            .post(format!("{AUTH}/refresh"))
            .header(COOKIE, format!("bankcore_refresh={refresh}"))
            .send()
            .await
            .map_err(net_error)?;
        if !response.status().is_success() {
            self.clear();
            return Err("Sessão expirada. Entre de novo.".into());
        }
        if let Err(error) = self.store(response).await {
            self.clear();
            return Err(error);
        }
        Ok(())
    }

    async fn store(&self, response: reqwest::Response) -> Result<Profile, String> {
        // Path=/api/auth no Set-Cookie não casa com POST /refresh neste host.
        // O token fica aqui e volta no cabeçalho Cookie.
        let refresh = refresh_cookie(response.headers());
        if !response.status().is_success() {
            return Err(api_message(response).await);
        }
        let body: AuthBody = response
            .json()
            .await
            .map_err(|_| "Resposta de autenticação inválida.".to_string())?;
        let refresh = refresh.ok_or_else(|| "O login não devolveu o cookie de renovação.".to_string())?;
        let profile = Profile {
            user_id: body.user_id,
            name: body.name,
            email: body.email,
            cpf: body.cpf,
        };
        let mut session = self
            .session
            .lock()
            .map_err(|_| "Estado da sessão indisponível.".to_string())?;
        session.access_token = Some(body.access_token);
        session.refresh_token = Some(refresh);
        session.profile = Some(profile.clone());
        Ok(profile)
    }

    async fn authed(
        &self,
        method: Method,
        url: &str,
        body: Option<Value>,
        idempotency: Option<String>,
    ) -> Result<Value, String> {
        let response = self
            .send(method.clone(), url, body.clone(), idempotency.as_deref(), true)
            .await?;
        if response.status() == StatusCode::UNAUTHORIZED {
            self.refresh().await?;
            let response = self.send(method, url, body, idempotency.as_deref(), true).await?;
            return read_body(response).await;
        }
        read_body(response).await
    }

    async fn send(
        &self,
        method: Method,
        url: &str,
        body: Option<Value>,
        idempotency: Option<&str>,
        authed: bool,
    ) -> Result<reqwest::Response, String> {
        let mut request = self.http.request(method, url);
        if authed {
            request = request.bearer_auth(self.access_token()?);
        }
        if let Some(key) = idempotency {
            request = request.header("Idempotency-Key", key);
        }
        if let Some(body) = body {
            request = request.json(&body);
        }
        request.send().await.map_err(net_error)
    }

    async fn home(&self) -> Result<Value, String> {
        self.authed(Method::GET, &format!("{TRANSACTIONS}/home"), None, None)
            .await
    }

    async fn statement(&self, account_id: String) -> Result<Value, String> {
        self.authed(
            Method::GET,
            &format!("{TRANSACTIONS}/statement?accountId={account_id}"),
            None,
            None,
        )
        .await
    }

    async fn receipt(&self, journal_id: String) -> Result<Value, String> {
        self.authed(
            Method::GET,
            &format!("{TRANSACTIONS}/receipts/{journal_id}"),
            None,
            None,
        )
        .await
    }

    async fn money(&self, path: &str, body: Value) -> Result<Value, String> {
        self.authed(
            Method::POST,
            &format!("{TRANSACTIONS}{path}"),
            Some(body),
            Some(Uuid::new_v4().to_string()),
        )
        .await
    }

    async fn ledger_up(&self) -> bool {
        self.http
            .get(format!("{TRANSACTIONS}/health"))
            .send()
            .await
            .map(|response| response.status().is_success())
            .unwrap_or(false)
    }
}

fn amount_value(amount: f64) -> Result<serde_json::Number, String> {
    if !amount.is_finite() || amount <= 0.0 {
        return Err("Informe um valor maior que zero.".into());
    }
    let rounded = (amount * 100.0).round() / 100.0;
    serde_json::Number::from_f64(rounded).ok_or_else(|| "Valor inválido.".into())
}

fn refresh_cookie(headers: &HeaderMap) -> Option<String> {
    headers.get_all(SET_COOKIE).iter().find_map(|value| {
        let raw = value.to_str().ok()?;
        parse_refresh(raw)
    })
}

fn parse_refresh(raw: &str) -> Option<String> {
    let pair = raw.split(';').next()?.trim();
    let (name, token) = pair.split_once('=')?;
    let token = token.trim().trim_matches('"');
    if name.trim() == "bankcore_refresh" && !token.is_empty() {
        Some(token.to_string())
    } else {
        None
    }
}

fn net_error(error: reqwest::Error) -> String {
    if error.is_connect() || error.is_timeout() {
        "A API local não respondeu. Na pasta vortex-bank\\docker, suba com docker compose up -d.".into()
    } else {
        "Não foi possível falar com a API.".into()
    }
}

async fn api_message(response: reqwest::Response) -> String {
    let status = response.status();
    let text = response.text().await.unwrap_or_default();
    if let Ok(value) = serde_json::from_str::<Value>(&text) {
        if let Some(message) = value.get("message").and_then(|item| item.as_str()) {
            if !message.is_empty() {
                return message.to_string();
            }
        }
        if let Some(title) = value.get("title").and_then(|item| item.as_str()) {
            if !title.is_empty() {
                return title.to_string();
            }
        }
    }
    if text.trim().is_empty() {
        format!("A API recusou a chamada ({status}).")
    } else {
        text
    }
}

async fn read_body(response: reqwest::Response) -> Result<Value, String> {
    let status = response.status();
    if !status.is_success() {
        return Err(api_message(response).await);
    }
    if status == StatusCode::NO_CONTENT {
        return Ok(Value::Null);
    }
    let text = response.text().await.map_err(net_error)?;
    if text.trim().is_empty() {
        return Ok(Value::Null);
    }
    serde_json::from_str(&text).map_err(|_| "A API devolveu uma resposta inválida.".to_string())
}

#[tauri::command]
fn current_session(bank: State<'_, Bank>) -> Result<Option<Profile>, String> {
    bank.profile()
}

#[tauri::command]
async fn login(bank: State<'_, Bank>, email: String, password: String) -> Result<Profile, String> {
    bank.login(email, password).await
}

#[tauri::command]
async fn logout(bank: State<'_, Bank>) -> Result<(), String> {
    bank.logout().await
}

#[tauri::command]
async fn ledger_status(bank: State<'_, Bank>) -> Result<bool, String> {
    Ok(bank.ledger_up().await)
}

#[tauri::command]
async fn home(bank: State<'_, Bank>) -> Result<Value, String> {
    bank.home().await
}

#[tauri::command]
async fn statement(bank: State<'_, Bank>, account_id: String) -> Result<Value, String> {
    bank.statement(account_id).await
}

#[tauri::command]
async fn receipt(bank: State<'_, Bank>, journal_id: String) -> Result<Value, String> {
    bank.receipt(journal_id).await
}

#[tauri::command]
async fn pix(bank: State<'_, Bank>, key: String, amount: f64) -> Result<Value, String> {
    let key = key.trim().to_string();
    if key.is_empty() {
        return Err("Informe a chave Pix.".into());
    }
    bank.money("/pix", json!({ "key": key, "amount": amount_value(amount)? }))
        .await
}

#[tauri::command]
async fn pay_boleto(bank: State<'_, Bank>, line: String) -> Result<Value, String> {
    let line = line.trim().to_string();
    if line.is_empty() {
        return Err("Linha digitável vazia.".into());
    }
    bank.money("/boletos/pay", json!({ "line": line })).await
}

#[tauri::command]
async fn pay_invoice(bank: State<'_, Bank>, invoice_id: String) -> Result<Value, String> {
    bank.money(&format!("/cards/invoices/{invoice_id}/pay"), json!({}))
        .await
}

#[tauri::command]
async fn purchase(
    bank: State<'_, Bank>,
    card_id: String,
    merchant: String,
    amount: f64,
) -> Result<Value, String> {
    bank.money(
        "/cards/purchases",
        json!({ "cardId": card_id, "merchant": merchant, "amount": amount_value(amount)? }),
    )
    .await
}

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    tauri::Builder::default()
        .plugin(tauri_plugin_opener::init())
        .manage(Bank::new())
        .invoke_handler(tauri::generate_handler![
            current_session,
            login,
            logout,
            ledger_status,
            home,
            statement,
            receipt,
            pix,
            pay_boleto,
            pay_invoice,
            purchase
        ])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}

#[cfg(test)]
mod tests {
    use super::{parse_refresh, Bank};

    #[test]
    fn reads_refresh_cookie_ignoring_path() {
        let raw = "bankcore_refresh=ABC123; expires=Sun, 11 Oct 2026 16:50:53 GMT; path=/api/auth; httponly; samesite=lax";
        assert_eq!(parse_refresh(raw).as_deref(), Some("ABC123"));
        assert_eq!(parse_refresh("other=1; path=/"), None);
    }

    #[tokio::test]
    #[ignore = "precisa da API local em 5081 e 5082"]
    async fn login_reads_home_and_leaves() {
        let bank = Bank::new();
        let profile = bank
            .login(
                "ana.ribeiro@vortexbank.demo".into(),
                "Ana-demo-2026".into(),
            )
            .await
            .expect("login");
        assert_eq!(profile.name, "Ana Ribeiro");
        assert!(bank.profile().unwrap().is_some());
        let home = bank.home().await.expect("home");
        let accounts = home.get("accounts").and_then(|value| value.as_array()).expect("contas");
        assert!(accounts.iter().any(|account| account.get("kind").and_then(|kind| kind.as_str()) == Some("corrente")));
        bank.logout().await.expect("logout");
        assert!(bank.profile().unwrap().is_none());
    }
}
