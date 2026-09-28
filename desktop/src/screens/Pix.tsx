import { useState, type FormEvent } from "react";
import { Link } from "react-router-dom";
import { explain, sendPix } from "../api";
import { money, parseAmount } from "../format";
import { useBank } from "./Shell";

const knownPeople: Record<string, string> = {
  "39053344705": "Ana Ribeiro",
  "52998224725": "Bruno Lima",
  "11144477735": "Carla Mendes",
};

export function Pix() {
  const { home, reload } = useBank();
  const [view, setView] = useState<"menu" | "send" | "keys" | "limits" | "fake">("menu");
  const [fakeTitle, setFakeTitle] = useState("");
  const [key, setKey] = useState("52998224725");
  const [amount, setAmount] = useState("1.00");
  const [message, setMessage] = useState("");
  const [ok, setOk] = useState(false);
  const [copied, setCopied] = useState(false);
  const checking = home?.accounts.find((account) => account.kind === "corrente");
  const ownKey = home?.pixKeys[0]?.value ?? "";
  const digits = key.replace(/\D/g, "");
  const recipient = knownPeople[digits];

  function openFake(title: string) {
    setFakeTitle(title);
    setView("fake");
  }

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const value = parseAmount(amount);
    if (!Number.isFinite(value) || value <= 0) {
      setOk(false);
      setMessage("Informe um valor maior que zero.");
      return;
    }
    try {
      const proof = await sendPix(digits || key.trim(), value);
      setOk(true);
      setMessage(`Pix enviado. ${proof.authentication}`);
      await reload();
    } catch (caught) {
      setOk(false);
      setMessage(explain(caught));
    }
  }

  return (
    <>
      <header className="area-head">
        <h2>Central Pix</h2>
        <p className="muted">Pix interno entre clientes do Vortex Bank. QR Code, copia e cola, presente e golpe são vitrine: não gravam no ledger.</p>
      </header>
      {view === "menu" && (
        <>
          <p className="hub-label">PAGAR</p>
          <div className="hub-grid">
            <button className="hub-card" type="button" onClick={() => setView("send")}><span className="qi">↗</span><span><strong>Fazer um Pix</strong><p>Transfere no ledger interno por CPF. Debita a origem e credita o destino.</p></span></button>
            <button className="hub-card" type="button" onClick={() => openFake("Ler QR Code")}><span className="qi">▣</span><span><strong>Ler QR Code</strong><p>Vitrine. Não lê câmera nem paga um código de fora.</p></span></button>
            <button className="hub-card" type="button" onClick={() => openFake("Pix Copia e Cola")}><span className="qi">⌘</span><span><strong>Pix Copia e Cola</strong><p>Vitrine. O código colado não liquida no ledger.</p></span></button>
            <button className="hub-card" type="button" onClick={() => openFake("Pix de presente")}><span className="qi">✦</span><span><strong>Pix de presente <span className="pill">Simulado</span></strong><p>Vitrine. Cartão comemorativo de demonstração. Não envia valor.</p></span></button>
          </div>
          <p className="hub-label">RECEBER</p>
          <div className="hub-grid">
            <button className="hub-card" type="button" onClick={() => openFake("Criar QR Code")}><span className="qi">▣</span><span><strong>Criar QR Code</strong><p>Vitrine de cobrança. Não gera um código pagável.</p></span></button>
            <button className="hub-card" type="button" onClick={() => setView("keys")}><span className="qi">◉</span><span><strong>Minhas chaves Pix</strong><p>A chave de CPF desta conta, para outro cliente do laboratório enviar.</p></span></button>
          </div>
          <p className="hub-label">CONSULTAR</p>
          <div className="hub-list">
            <Link className="hub-card" to="/app/extrato"><span className="qi">≡</span><span><strong>Extrato Pix</strong><p>Lançamentos e comprovantes que de fato passaram pelo ledger.</p></span></Link>
            <button className="hub-card" type="button" onClick={() => setView("limits")}><span className="qi">▤</span><span><strong>Meus limites Pix</strong><p>Limite diário de R$ 20.000,00 neste simulador. Tarifa zero.</p></span></button>
            <button className="hub-card warn" type="button" onClick={() => openFake("Informar golpe")}><span className="qi">!</span><span><strong>Informar golpe ou fraude <span className="pill">Simulado</span></strong><p>Vitrine. Não existe devolução especial aqui. Nenhum estorno é disparado.</p></span></button>
          </div>
        </>
      )}
      {view === "send" && (
        <section className="work">
          <form className="panel" onSubmit={(event) => void onSubmit(event)}>
            <button className="chip" type="button" onClick={() => setView("menu")}>Voltar à Central</button>
            <div className="field-row" style={{ marginTop: 12 }}>
              <span className="label">Saldo disponível para Pix</span>
              <span className="pill live">Liquidação imediata</span>
            </div>
            <p className="amount" style={{ fontSize: 32 }}>{checking ? money(checking.balance) : "—"}</p>
            <label>Chave Pix do destinatário
              <input value={key} onChange={(event) => setKey(event.target.value)} required />
            </label>
            {recipient && (
              <div className="identified">
                <span><small className="muted">DESTINATÁRIO</small><br /><strong>{recipient}</strong><br /><small className="muted">Vortex Bank · agência 0001</small></span>
                <span className="pill live">No simulador</span>
              </div>
            )}
            <label>Valor da transferência
              <input value={amount} onChange={(event) => setAmount(event.target.value)} inputMode="decimal" required />
            </label>
            <div className="chips">
              {[1, 10, 50, 100].map((step) => (
                <button key={step} type="button" className="chip" onClick={() => setAmount((current) => (Number(current || 0) + step).toFixed(2))}>+ {money(step)}</button>
              ))}
            </div>
            {message && <p className={ok ? "credit" : "error"}>{message}</p>}
            <button className="button button-wide" type="submit">Confirmar e transferir Pix</button>
          </form>
          <article className="panel">
            <div className="label">O que esta tela faz</div>
            <p className="note">Só “Fazer um Pix” grava partidas. As outras opções da central são vitrine e não movem saldo.</p>
          </article>
        </section>
      )}
      {view === "keys" && (
        <article className="panel form-card">
          <button className="chip" type="button" onClick={() => setView("menu")}>Voltar à Central</button>
          <div className="field-row" style={{ marginTop: 12 }}>
            <span className="label">Sua chave Pix</span>
            <span className="pill">CPF</span>
          </div>
          <div className="key-box">
            <span><small className="muted">CHAVE PRIMÁRIA</small><br /><strong>{ownKey || "—"}</strong></span>
            <button type="button" className="chip" onClick={() => { void navigator.clipboard.writeText(ownKey); setCopied(true); }}>{copied ? "Copiada" : "Copiar"}</button>
          </div>
        </article>
      )}
      {view === "limits" && (
        <article className="panel form-card">
          <button className="chip" type="button" onClick={() => setView("menu")}>Voltar à Central</button>
          <div className="limit-line"><span>Limite diário</span><strong>R$ 20.000,00</strong></div>
          <div className="limit-line"><span>Tarifa</span><strong>R$ 0,00</strong></div>
          <p className="note">Acima desse valor no mesmo dia útil, o ledger recusa o Pix. Não há limite noturno separado.</p>
        </article>
      )}
      {view === "fake" && (
        <article className="panel form-card">
          <button className="chip" type="button" onClick={() => setView("menu")}>Voltar à Central</button>
          <h2 className="page-title">{fakeTitle}</h2>
          <span className="pill">Simulado</span>
          <p className="note">Esta opção é vitrine. Ela não lê QR, não cola payload e não estorna valor. O saldo permanece o do ledger.</p>
        </article>
      )}
    </>
  );
}
