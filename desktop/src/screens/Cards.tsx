import { useState, type FormEvent } from "react";
import { explain, payInvoice, purchase } from "../api";
import { day, money, parseAmount } from "../format";
import { useBank } from "./Shell";

export function Cards() {
  const { home, reload } = useBank();
  const [message, setMessage] = useState("");
  const [ok, setOk] = useState(false);
  const [showCvv, setShowCvv] = useState(false);
  const credit = home?.cards.find((card) => card.kind === "credito");
  const debit = home?.cards.find((card) => card.kind === "debito");
  const used = credit?.used ?? 0;
  const limit = credit?.limit ?? 0;
  const ratio = limit > 0 ? Math.min(100, (used / limit) * 100) : 0;

  async function pay() {
    if (!credit?.openInvoiceId) return;
    try {
      await payInvoice(credit.openInvoiceId);
      setOk(true);
      setMessage("Fatura paga com a conta corrente.");
      await reload();
    } catch (caught) {
      setOk(false);
      setMessage(explain(caught));
    }
  }

  async function buy(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    const value = parseAmount(String(data.get("amount")));
    if (!Number.isFinite(value) || value <= 0) {
      setOk(false);
      setMessage("Informe um valor maior que zero.");
      return;
    }
    try {
      await purchase(String(data.get("cardId")), String(data.get("merchant")), value);
      setOk(true);
      setMessage("Compra lançada no ledger.");
      await reload();
    } catch (caught) {
      setOk(false);
      setMessage(explain(caught));
    }
  }

  return (
    <>
      <header className="area-head" style={{ display: "flex", justifyContent: "space-between", gap: 16, alignItems: "end" }}>
        <div>
          <h2>Gestão de cartões</h2>
          <p className="muted">Plásticos do laboratório. Pagar fatura e lançar compra gravam no ledger. O restante é vitrine.</p>
        </div>
        <span className="pill">Módulo simulado</span>
      </header>
      <section className="card-grid">
        <article className="panel">
          <div className="field-row">
            <strong>Cartão principal</strong>
            <span className="pill live">Ativo</span>
          </div>
          <div className="plastic" style={{ margin: "12px 0" }}>
            <div className="row"><span>Vortex</span><span>Crédito</span></div>
            <div className="pan">•••• {credit?.pan.slice(-4) ?? "0000"}</div>
            <div className="row"><span>{credit?.holder ?? "—"}</span><span>{credit?.openInvoiceDue ? `Vence ${day(credit.openInvoiceDue)}` : credit?.expiry}</span></div>
          </div>
          <div className="field-row"><span className="muted">Limite utilizado</span><strong>{money(used)} de {money(limit)}</strong></div>
          <div className="bar"><span style={{ width: `${ratio}%` }} /></div>
          <div className="invoice-strip">
            <span><small className="muted">FATURA</small><br /><strong className="due">{money(credit?.openInvoiceAmount ?? 0)}</strong></span>
            {credit?.openInvoiceId ? <button className="button" type="button" onClick={() => void pay()}>Pagar fatura</button> : <span className="pill">Sem fatura aberta</span>}
          </div>
          <div className="card-actions">
            <button className="button-ghost" type="button" onClick={() => { setOk(false); setMessage("Ajustar limite é vitrine. O limite deste cartão não muda."); }}>Ajustar limite</button>
            <button className="button-ghost" type="button" onClick={() => { setOk(false); setMessage("Bloquear cartão é vitrine. Nenhuma compra foi impedida no ledger."); }}>Bloquear cartão</button>
          </div>
        </article>
        <article className="panel">
          <div className="field-row">
            <strong>Cartão de débito</strong>
            <span className="pill">Conta corrente</span>
          </div>
          <div className="plastic virtual" style={{ margin: "12px 0" }}>
            <div className="row"><span>Vortex</span><span>Débito</span></div>
            <div className="pan">•••• {debit?.pan.slice(-4) ?? "0000"}</div>
            <div className="row"><span>{debit?.holder ?? "—"}</span><span>{showCvv ? `CVV ${debit?.cvv}` : "CVV oculto"}</span></div>
          </div>
          <div className="field-row"><span className="muted">Débito na conta</span><strong>Saldo da corrente</strong></div>
          <div className="bar"><span style={{ width: "18%" }} /></div>
          <div className="invoice-strip">
            <span><small className="muted">COMPRAS NO DÉBITO</small><br /><strong>Saem da corrente</strong></span>
            <span className="pill">Na hora</span>
          </div>
          <div className="card-actions">
            <button className="button-ghost" type="button" onClick={() => setShowCvv((value) => !value)}>{showCvv ? "Ocultar CVV" : "Ver dados e CVV"}</button>
            <button className="button-ghost" type="button" onClick={() => { setOk(false); setMessage("Gerar novo cartão é vitrine. O número deste plástico não muda."); }}>Gerar novo cartão</button>
          </div>
        </article>
      </section>
      <section className="info-grid">
        <article className="panel"><strong>Conciliação</strong><p className="muted">Exportar OFX é vitrine. O extrato oficial é o da tela Extrato.</p></article>
        <article className="panel"><strong>Seguro</strong><p className="muted">Não há apólice. O cartão só existe dentro do simulador.</p></article>
        <article className="panel"><strong>Controle de limites</strong><p className="muted">O crédito recusa compra acima do limite. Isso o ledger aplica de verdade.</p></article>
      </section>
      <form className="panel" style={{ marginTop: 16 }} onSubmit={(event) => void buy(event)}>
        <div className="field-row"><strong>Nova compra no ledger</strong><span className="pill live">Grava partida</span></div>
        <div className="card-actions" style={{ marginTop: 12 }}>
          <label>Cartão
            <select name="cardId">{home?.cards.map((card) => <option key={card.id} value={card.id}>{card.kind} · {card.pan.slice(-4)}</option>)}</select>
          </label>
          <label>Comerciante
            <select name="merchant"><option>Mercado</option><option>Combustível</option><option>Farmácia</option><option>Streaming</option></select>
          </label>
        </div>
        <label>Valor<input name="amount" inputMode="decimal" required /></label>
        <button className="button" type="submit">Lançar compra</button>
        {message && <p className={ok ? "credit" : undefined}>{message}</p>}
      </form>
    </>
  );
}
