import { useState } from "react";
import { money } from "../format";

const products = [
  ["CDB Liquidez Diária", "104% do CDI · resgate ilustrativo em D+0", "FGC", "Aplicação mínima: R$ 100,00"],
  ["LCA Sustentabilidade Agro", "98% do CDI · isento de IR na vitrine", "Isento", "Vencimento ilustrativo: 12 meses"],
  ["Tesouro Selic 2029", "Selic + 0,15% ao ano", "Tesouro", "Aplicação mínima: R$ 150,00"],
  ["Fundo Vortex Tech", "Rentabilidade ilustrativa em 12 meses: + 24,8%", "Fundo", "Série fictícia, sem custódia"],
];

export function Invest() {
  const [aporte, setAporte] = useState(10000);
  const [notice, setNotice] = useState("");
  const noApply = () => setNotice("Este módulo não aplica nem resgata. O saldo da conta corrente permanece o do ledger.");
  return (
    <>
      <header className="area-head" style={{ display: "flex", justifyContent: "space-between", gap: 16, alignItems: "end" }}>
        <div>
          <h2>Vortex Invest</h2>
          <p className="muted">Módulo didático. Números de cenário, iguais para qualquer cliente. Não aplica saldo em conta.</p>
        </div>
        <button className="button" type="button" onClick={noApply}>Novo aporte</button>
      </header>
      <article className="panel">
        <div className="stat3" style={{ marginTop: 0 }}>
          <div className="stat-box"><small className="muted">PATRIMÔNIO ILUSTRATIVO</small><strong className="credit">R$ 45.890,20</strong><small className="credit">+ R$ 480,20 neste cenário</small></div>
          <div className="stat-box"><small className="muted">REFERÊNCIA</small><strong>104% do CDI</strong><small className="muted">Taxa de vitrine</small></div>
          <div className="stat-box"><small className="muted">LIQUIDEZ IMEDIATA (D+0)</small><strong style={{ color: "var(--primary)" }}>R$ 25.000,00</strong><small className="muted">Resgate ilustrativo</small></div>
        </div>
        <div className="field-row" style={{ marginTop: 16 }}><span className="muted">Alocação da carteira</span><span>65% renda fixa · 20% Tesouro · 15% fundos</span></div>
        <div className="alloc"><span style={{ width: "65%", background: "#fb7185" }} /><span style={{ width: "20%", background: "#e11d48" }} /><span style={{ width: "15%", background: "#9f1239" }} /></div>
      </article>
      <h3 style={{ margin: "22px 0 10px" }}>Produtos de renda fixa e títulos</h3>
      <section className="products">
        {products.map(([name, detail, tag, extra]) => (
          <article key={name} className="panel">
            <div className="field-row"><strong>{name}</strong><span className="pill">{tag}</span></div>
            <p className="muted">{detail}</p>
            <div className="field-row"><span className="muted">{extra}</span><button className="button" type="button" onClick={noApply}>Investir</button></div>
          </article>
        ))}
      </section>
      <article className="panel" style={{ marginTop: 16 }}>
        <strong>Simulador de rendimento em 12 meses</strong>
        <div className="field-row" style={{ marginTop: 12 }}><span>Valor do aporte simulado</span><strong>{money(aporte)}</strong></div>
        <input type="range" min={1000} max={50000} step={500} value={aporte} onChange={(event) => setAporte(Number(event.target.value))} style={{ width: "100%", accentColor: "#e11d48" }} />
        <div className="products">
          <div className="stat-box"><small className="muted">Neste cenário (104% do CDI)</small><strong className="credit">{money(aporte * 1.1144)}</strong></div>
          <div className="stat-box"><small className="muted">Na poupança antiga, só para comparar</small><strong>{money(aporte * 1.0617)}</strong></div>
        </div>
        {notice && <p className="note">{notice}</p>}
      </article>
    </>
  );
}
