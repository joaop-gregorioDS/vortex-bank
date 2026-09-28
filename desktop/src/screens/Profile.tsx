import { useState } from "react";
import { initials, maskCpf } from "../format";
import { useBank } from "./Shell";

export function Profile() {
  const { session, home } = useBank();
  const checking = home?.accounts.find((account) => account.kind === "corrente");
  const [notice, setNotice] = useState("");
  const [bio, setBio] = useState(true);
  return (
    <>
      <article className="panel" style={{ display: "flex", justifyContent: "space-between", gap: 16, alignItems: "center" }}>
        <div style={{ display: "flex", gap: 14, alignItems: "center" }}>
          <span className="avatar-lg" style={{ width: 64, height: 64, fontSize: 20 }}>{initials(session.name)}</span>
          <div>
            <h2 style={{ margin: 0 }}>{session.name} <span className="pill live">Titular</span></h2>
            <p className="muted" style={{ margin: "4px 0" }}>CPF {maskCpf(session.cpf)}</p>
            <p className="muted" style={{ margin: 0 }}>Ag. 0001 · Cc. {checking?.number ?? "—"} · Vortex Bank</p>
          </div>
        </div>
        <div style={{ textAlign: "right" }}>
          <small className="muted">STATUS DA CONTA</small>
          <p style={{ margin: 0 }} className="credit">Ativa neste simulador</p>
        </div>
      </article>
      <section className="split" style={{ marginTop: 16 }}>
        <article className="panel">
          <div className="field-row"><strong>Dados cadastrais do titular</strong><span className="pill">Somente leitura</span></div>
          <label>Nome completo<input value={session.name} readOnly /></label>
          <div className="card-actions">
            <label>CPF do titular<input value={maskCpf(session.cpf)} readOnly /></label>
            <label>Telefone <span className="pill">Simulado</span><input value="(11) 90000-0000" readOnly /></label>
          </div>
          <label>E-mail<input value={session.email} readOnly /></label>
          <label>Endereço <span className="pill">Simulado</span><input value="Não informado neste laboratório" readOnly /></label>
          <button className="button button-wide" type="button" onClick={() => setNotice("Nome, CPF e e-mail já são os da conta. Telefone e endereço são vitrine e não são gravados.")}>Salvar alterações cadastrais</button>
          {notice && <p className="note">{notice}</p>}
        </article>
        <div className="stack">
          <article className="panel">
            <strong>Segurança</strong>
            <div className="key-box" style={{ marginTop: 12 }}><span><strong>Biometria</strong><br /><small className="muted">Vitrine. Não autentica compra.</small></span><input type="checkbox" checked={bio} onChange={() => setBio((value) => !value)} aria-label="Biometria simulada" /></div>
            <div className="key-box"><span><strong>Token</strong><br /><small className="muted">Vitrine. Não há segundo fator nesta versão.</small></span><span className="pill">Simulado</span></div>
            <button className="button-ghost" type="button" style={{ width: "100%", marginTop: 8 }} onClick={() => setNotice("A senha dos titulares de demonstração não é alterada por esta tela.")}>Alterar senha de acesso</button>
          </article>
          <article className="panel">
            <strong>Dispositivos</strong>
            <div className="limit-line"><span>Este computador<br /><small className="muted">Sessão atual</small></span><span className="pill live">Ativo</span></div>
            <p className="muted">Vitrine. Não há outro aparelho conectado de verdade.</p>
          </article>
        </div>
      </section>
      <article className="panel" style={{ marginTop: 16 }}>
        <div className="field-row"><strong>Dados da empresa</strong><span className="pill">Simulado</span></div>
        <p className="muted">A vitrine mostra a Vortex Software. Não é o cadastro do titular e não vai para o ledger.</p>
        <div className="info-grid">
          <div className="stat-box"><small className="muted">RAZÃO SOCIAL</small><strong style={{ fontSize: 16 }}>Vortex Software</strong></div>
          <div className="stat-box"><small className="muted">NOME FANTASIA</small><strong style={{ fontSize: 16 }}>Vortex Software</strong></div>
          <div className="stat-box"><small className="muted">REGIME</small><strong style={{ fontSize: 16 }}>Ilustrativo</strong></div>
        </div>
        <p className="muted">Atividade ilustrativa: desenvolvimento de programas de computador sob encomenda.</p>
      </article>
    </>
  );
}
