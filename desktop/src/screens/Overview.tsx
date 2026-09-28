import { useState } from "react";
import { Link } from "react-router-dom";
import { day, initials, money } from "../format";
import { useBank } from "./Shell";

export function Overview() {
  const [hidden, setHidden] = useState(false);
  const { home, session } = useBank();
  if (!home) return <p>Carregando saldos…</p>;
  const checking = home.accounts.find((account) => account.kind === "corrente");
  const openBills = home.boletos.filter((boleto) => boleto.status === "aberto" && !boleto.mine);
  const due = openBills.reduce((sum, boleto) => sum + boleto.amount, 0);
  const credit = home.cards.find((card) => card.kind === "credito");
  const show = (value: number) => (hidden ? "R$ •••••" : money(value));
  return (
    <>
      <div className="hello">
        <div style={{ display: "flex", gap: 12, alignItems: "center" }}>
          <span className="avatar-lg">{initials(session.name)}</span>
          <div>
            <h1>Olá, {session.name.split(" ")[0]}</h1>
            <p className="muted" style={{ margin: 0 }}>Vortex Bank · Ag 0001 · Cc {checking?.number}</p>
          </div>
        </div>
        <button className="button-ghost" type="button" onClick={() => setHidden((value) => !value)}>{hidden ? "Mostrar" : "Ocultar"}</button>
      </div>
      <section className="stats">
        <article className="panel">
          <div className="label">Saldo em conta corrente <span className="pill live">Ledger</span></div>
          <p className="amount">{show(checking?.balance ?? 0)}</p>
          <p className="muted">Partidas dobradas ativas · nenhum valor é real</p>
        </article>
        <article className="panel">
          <div className="label">Boletos em aberto <span className="pill">Simulado</span></div>
          <p className="amount due">{show(due)}</p>
          <p className="muted">{openBills.length} boletos a liquidar neste ambiente</p>
        </article>
      </section>
      <section className="quick">
        <Link to="/app/pix"><span className="qi">↗</span>Transferir Pix</Link>
        <Link to="/app/extrato"><span className="qi">≡</span>Extrato</Link>
        <Link to="/app/boletos"><span className="qi">▤</span>Pagar boleto</Link>
        <Link to="/app/investimentos"><span className="qi">%</span>Investimentos</Link>
        <Link to="/app/cartoes"><span className="qi">▭</span>Cartões</Link>
        <Link to="/app/comprovante/ultimo"><span className="qi">⌘</span>Comprovante</Link>
        <Link to="/app/perfil"><span className="qi">☺</span>Perfil</Link>
      </section>
      <section className="split">
        <article className="panel">
          <div className="label" style={{ display: "flex", justifyContent: "space-between" }}>Extrato recente <Link to="/app/extrato">Ver completo</Link></div>
          {home.recent.slice(0, 4).map((line, index) => (
            <Link className="tx" key={`${line.journalId}-${index}`} to={`/app/comprovante/${line.journalId}`}>
              <span>{line.description}<br /><small>{day(line.businessDate)}</small></span>
              <span className={line.direction === "debito" ? "debit" : "credit"}>{line.direction === "debito" ? "−" : "+"} {hidden ? "••••" : money(line.amount)}</span>
            </Link>
          ))}
        </article>
        <article className="panel">
          <div className="label">Cartão principal <span className="pill">Simulado</span></div>
          <div className="plastic" style={{ marginTop: 12 }}>
            <div className="row"><span>Vortex</span><span>{credit ? "Crédito" : "Débito"}</span></div>
            <div className="pan">•••• {credit?.pan.slice(-4) ?? "0000"}</div>
            <div className="row"><span>{session.name.toUpperCase()}</span><span>{credit?.openInvoiceDue ? `Vence ${day(credit.openInvoiceDue)}` : credit?.expiry}</span></div>
          </div>
          <p className="muted">Fatura {money(credit?.openInvoiceAmount ?? 0)} · usado {money(credit?.used ?? 0)}</p>
        </article>
      </section>
    </>
  );
}
