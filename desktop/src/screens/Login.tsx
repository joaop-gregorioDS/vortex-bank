import { useState, type FormEvent } from "react";
import { explain, holders, login, type Session } from "../api";
import { initials } from "../format";
import { Shield } from "../mark";

export function Login({ onEnter }: { onEnter: (session: Session) => void }) {
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);

  async function enter(email: string, password: string) {
    setError("");
    setBusy(true);
    try {
      onEnter(await login(email, password));
    } catch (caught) {
      setError(explain(caught));
    } finally {
      setBusy(false);
    }
  }

  function enterByCpf(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    const digits = String(data.get("cpf")).replace(/\D/g, "");
    const person = holders.find((item) => item.cpf.replace(/\D/g, "") === digits);
    if (!person) {
      setError("CPF não encontrado neste simulador.");
      return;
    }
    void enter(person.email, String(data.get("password")));
  }

  return (
    <div className="app-frame">
      <p className="sim-banner">Ambiente simulado. Nenhum valor é real.</p>
      <main id="conteudo" className="login-screen">
        <section className="login-card">
          <div className="mark-box"><Shield light /></div>
          <h1>Vortex<span>Bank</span></h1>
          <p className="sub">Web banking de varejo e ledger de partidas dobradas</p>
          <span className="badge">PORTFÓLIO / LABORATÓRIO DE DEMONSTRAÇÃO</span>
          <p className="muted" style={{ marginTop: 16 }}>Contas de um clique</p>
          <div className="one-click">
            {holders.map((person) => (
              <button key={person.cpf} className="persona" type="button" disabled={busy} onClick={() => void enter(person.email, person.password)}>
                <span className="avatar">{initials(person.name)}</span>
                <span><strong>{person.name}</strong><small>{person.cpf}</small></span>
              </button>
            ))}
          </div>
          <div className="rule">OU CPF E SENHA</div>
          <form onSubmit={enterByCpf}>
            <label>CPF do titular<input name="cpf" inputMode="numeric" placeholder="000.000.000-00" required /></label>
            <label>Senha de acesso<input name="password" type="password" placeholder="••••••••" required /></label>
            {error && <p className="error">{error}</p>}
            <button className="button button-wide" type="submit" disabled={busy}>{busy ? "Entrando…" : "Acessar conta segura →"}</button>
          </form>
          <p className="fine">versão 1.0 · Vortex Software</p>
        </section>
      </main>
    </div>
  );
}
