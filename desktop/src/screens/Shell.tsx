import { useEffect, useState } from "react";
import { NavLink, Outlet, useLocation, useOutletContext } from "react-router-dom";
import { openUrl } from "@tauri-apps/plugin-opener";
import { explain, home, ledgerStatus, logout, sessionEnded, type Home, type Session } from "../api";
import { initials } from "../format";
import { Shield } from "../mark";

const nav = [
  ["/app", "Início"],
  ["/app/pix", "Central Pix"],
  ["/app/extrato", "Extrato"],
  ["/app/boletos", "Central de Pagamentos"],
  ["/app/cartoes", "Central de Cartões"],
  ["/app/investimentos", "Vortex Invest"],
  ["/app/perfil", "Meu perfil e ajustes"],
];

const titles: Record<string, string> = {
  "/app": "Início · Dashboard",
  "/app/pix": "Início / Central Pix",
  "/app/extrato": "Extrato · Ledger",
  "/app/boletos": "Início / Central de Pagamentos",
  "/app/cartoes": "Gestão de cartões",
  "/app/investimentos": "Início / Vortex Invest",
  "/app/perfil": "Início / Meu perfil e ajustes",
};

const fontSteps = [15, 17, 19];

export type BankContext = {
  session: Session;
  home: Home | null;
  reload: () => Promise<void>;
};

export function useBank() {
  return useOutletContext<BankContext>();
}

export function Shell({ session, onLeave }: { session: Session; onLeave: () => void }) {
  const location = useLocation();
  const [panel, setPanel] = useState<Home | null>(null);
  const [error, setError] = useState("");
  const [live, setLive] = useState(false);
  const [fontStep, setFontStep] = useState(() => {
    const stored = Number(localStorage.getItem("vb-font") ?? 0);
    return Number.isInteger(stored) && stored >= 0 && stored < fontSteps.length ? stored : 0;
  });

  async function load() {
    try {
      setPanel(await home());
      setError("");
    } catch (caught) {
      if (sessionEnded(caught)) {
        onLeave();
        return;
      }
      setError(explain(caught));
    }
  }

  useEffect(() => {
    void load();
  }, []);

  useEffect(() => {
    const safe = Number.isInteger(fontStep) && fontStep >= 0 && fontStep < fontSteps.length ? fontStep : 0;
    document.documentElement.style.fontSize = `${fontSteps[safe]}px`;
    localStorage.setItem("vb-font", String(safe));
    return () => {
      document.documentElement.style.fontSize = "";
    };
  }, [fontStep]);

  useEffect(() => {
    let alive = true;
    async function tick() {
      const ok = await ledgerStatus().catch(() => false);
      if (alive) setLive(ok);
    }
    void tick();
    const id = window.setInterval(() => void tick(), 15000);
    return () => {
      alive = false;
      window.clearInterval(id);
    };
  }, []);

  const checking = panel?.accounts.find((account) => account.kind === "corrente");
  const context: BankContext = { session, home: panel, reload: load };

  async function leave() {
    await logout().catch(() => undefined);
    onLeave();
  }

  async function swagger() {
    try {
      await openUrl("http://127.0.0.1:8080/swagger");
    } catch (caught) {
      setError(explain(caught));
    }
  }

  return (
    <div className="app-frame">
      <p className="sim-banner">Ambiente simulado. Nenhum valor é real.</p>
      <div className="app-shell">
        <aside className="sider">
          <NavLink className="sider-brand" to="/app" end>
            <span className="mark-box" style={{ width: 36, height: 36, borderRadius: 10, margin: 0 }}><Shield light /></span>
            Vortex Bank
          </NavLink>
          <nav aria-label="Conta">
            {nav.map(([to, label]) => (
              <NavLink key={to} to={to} end={to === "/app"}>{label}</NavLink>
            ))}
          </nav>
          <div className="sider-user">
            <span className="avatar">{initials(session.name)}</span>
            <span><strong>{session.name}</strong><br /><small className="muted">Ag 0001 · {checking?.number ?? "—"}</small></span>
          </div>
          <button className="button-ghost" type="button" onClick={() => void leave()}>Sair</button>
        </aside>
        <div className="workspace">
          <header className="topbar">
            <span>{titles[location.pathname] ?? "Vortex Bank · Simulado"}</span>
            <div className="pills">
              <span className={live ? "pill live" : "pill"}>{live ? "Ledger conectado" : "Ledger indisponível"}</span>
              <button className="pill" type="button" onClick={() => setFontStep((current) => (current + 1) % fontSteps.length)} aria-label={`Tamanho da fonte ${fontSteps[fontStep] ?? 15} pixels`}>A+ Fonte</button>
              <button className="pill" type="button" onClick={() => void swagger()}>Swagger</button>
            </div>
          </header>
          <main id="conteudo">
            <a className="skip-link" href="#conteudo">Pular para o conteúdo</a>
            {error && <p className="error">{error}</p>}
            <Outlet context={context} />
          </main>
        </div>
      </div>
    </div>
  );
}
