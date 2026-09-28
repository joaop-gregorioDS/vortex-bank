import { useState } from "react";
import { Link } from "react-router-dom";
import { explain, payBoleto } from "../api";
import { day, money } from "../format";
import { useBank } from "./Shell";

const autoDebit = [
  ["Enel Energia", "Débito ilustrativo todo dia 15"],
  ["Sabesp Água", "Débito ilustrativo todo dia 18"],
  ["Fibra Internet", "Débito ilustrativo todo dia 22"],
];

export function Payments() {
  const { home, reload } = useBank();
  const [message, setMessage] = useState("");
  const [ok, setOk] = useState(false);

  async function pay(line: string) {
    try {
      await payBoleto(line);
      setOk(true);
      setMessage("Boleto pago. O valor saiu da conta corrente.");
      await reload();
    } catch (caught) {
      setOk(false);
      setMessage(explain(caught));
    }
  }

  const checking = home?.accounts.find((account) => account.kind === "corrente");
  const open = (home?.boletos ?? []).filter((boleto) => boleto.status === "aberto" && !boleto.mine);
  const total = open.reduce((sum, boleto) => sum + boleto.amount, 0);

  return (
    <div style={{ maxWidth: 860, margin: "0 auto" }}>
      <header className="area-head">
        <h2>Central de Pagamentos</h2>
        <p className="muted">Pagar um boleto desta lista grava no ledger. Débito automático e o limite de R$ 50.000,00 são vitrine.</p>
      </header>
      <article className="panel card-grid">
        <div>
          <div className="label">Saldo disponível</div>
          <p className="amount" style={{ fontSize: 32 }}>{checking ? money(checking.balance) : "—"}</p>
          <p className="muted">Conta corrente · agência 0001</p>
        </div>
        <div>
          <div className="label">Agendamentos em aberto</div>
          <p className="amount due" style={{ fontSize: 32 }}>− {money(total)}</p>
          <p className="muted">Boletos desta lista ainda não pagos</p>
        </div>
      </article>
      <section className="quick" style={{ marginTop: 16 }}>
        <a href="#boletos-abertos"><span className="qi">▤</span>Pagamento</a>
        <Link to="/app/pix"><span className="qi">↗</span>Pix</Link>
        <Link to="/app/cartoes"><span className="qi">▭</span>Pagar fatura</Link>
        <button type="button" onClick={() => { setOk(false); setMessage("A agenda DDA é vitrine. Só os boletos da lista abaixo podem ser pagos."); }}><span className="qi">▦</span>Agenda DDA</button>
      </section>
      <article className="panel" id="boletos-abertos">
        <div className="field-row">
          <strong>Boletos em aberto</strong>
          <span className="pill live">{open.length} para pagar</span>
        </div>
        <p className="muted">Emitidos dentro do Vortex Bank. A linha não quita boleto de fora.</p>
        {open.map((boleto) => (
          <div className="tx" key={boleto.id}>
            <span><strong>{boleto.beneficiary}</strong><br /><small>Vencimento {day(boleto.dueDate)}</small></span>
            <span style={{ textAlign: "right" }}><strong>{money(boleto.amount)}</strong><br /><button className="chip" type="button" onClick={() => void pay(boleto.line)}>Pagar</button></span>
          </div>
        ))}
        {open.length === 0 && <p className="muted">Nenhum boleto em aberto.</p>}
        {message && <p className={ok ? "credit" : undefined}>{message}</p>}
      </article>
      <article className="panel" style={{ marginTop: 16 }}>
        <div className="field-row"><strong>Contas em débito automático</strong><span className="pill">Simulado</span></div>
        <div className="info-grid">
          {autoDebit.map(([name, when]) => (
            <div key={name} className="key-box"><span><strong>{name}</strong><br /><small className="muted">{when}</small></span><span className="pill live">✓</span></div>
          ))}
        </div>
      </article>
      <article className="panel" style={{ marginTop: 16 }}>
        <div className="field-row"><strong>Limite de pagamento</strong><span className="pill">Simulado</span></div>
        <div className="limit-line"><span>Teto ilustrativo de boletos no dia</span><strong>R$ 50.000,00</strong></div>
        <div className="bar"><span style={{ width: "70%" }} /></div>
        <p className="muted">Vitrine. O ledger não aplica esse teto. Ele só recusa quando o saldo da corrente não cobre o boleto.</p>
      </article>
    </div>
  );
}
