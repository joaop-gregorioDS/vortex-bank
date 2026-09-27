import { useEffect, useState, type FormEvent } from "react";
import { Link, NavLink, Navigate, Route, Routes, useLocation, useNavigate, useParams } from "react-router-dom";
import { bank, day, login, logout, money, refresh, register, token, type Home, type Line, type Receipt, type Session } from "./api";
import { saveStatementPdf } from "./statementPdf";

const demos = [
  ["Ana Ribeiro", "ana.ribeiro@vortexbank.demo", "Ana-demo-2026", "390.533.447-05"],
  ["Bruno Lima", "bruno.lima@vortexbank.demo", "Bruno-demo-2026", "529.982.247-25"],
];

function initials(name: string) {
  return name.split(" ").filter(Boolean).slice(0, 2).map((part) => part[0]?.toUpperCase()).join("");
}

function Shield({ light = false }: { light?: boolean }) {
  return (
    <svg viewBox="0 0 32 38" width="28" height="32" aria-hidden="true">
      <path d="M16 2.5L29 7.5V17.5C29 26.5 23 32.8 16 35.5C9 32.8 3 26.5 3 17.5V7.5L16 2.5Z" fill={light ? "#fff" : "#E11D48"} />
      <path d="M10 11.5L16 24.5L22 11.5H19L16 18L13 11.5H10Z" fill={light ? "#E11D48" : "#fff"} />
    </svg>
  );
}

export default function App() {
  const [session, setSession] = useState<Session | null>(null);
  const [ready, setReady] = useState(false);

  useEffect(() => {
    refresh().then(async (ok) => {
      if (ok) {
        const profile = await fetch("/api/auth/me", { headers: { Authorization: `Bearer ${token()}` } }).then((response) => response.json());
        setSession({ userId: profile.userId, name: profile.name, email: profile.email, cpf: profile.cpf, accessToken: token() ?? "" });
      }
      setReady(true);
    });
  }, []);

  if (!ready) return <p className="login-screen">Abrindo o simulador…</p>;

  return (
    <>
      <a className="skip-link" href="#conteudo">Pular para o conteúdo</a>
      <Routes>
        <Route path="/" element={<Landing />} />
        <Route path="/entrar" element={<Enter setSession={setSession} />} />
        <Route path="/cadastro" element={<Signup setSession={setSession} />} />
        <Route path="/app/*" element={session ? <Bank session={session} onLeave={() => setSession(null)} /> : <Navigate to="/entrar" replace />} />
      </Routes>
    </>
  );
}

function Landing() {
  return (
    <>
      <header className="site-bar">
        <Link className="site-brand" to="/"><Shield light /><span>VORTEX<small>SOFTWARE</small></span></Link>
        <nav className="site-nav">
          <a href="https://www.vortexsoftware.tech/">Home</a>
          <a href="https://www.vortexsoftware.tech/#servicos">Serviços</a>
          <Link to="/entrar">Vortex Bank</Link>
        </nav>
        <Link className="site-cta" to="/entrar">Entrar no laboratório →</Link>
      </header>
      <main id="conteudo" className="hero-vortex">
        <div className="hero-grid">
          <div>
            <p className="kicker">• ENGENHARIA DE SOFTWARE, PONTA A PONTA</p>
            <h1>Da ideia<br />à produção.</h1>
            <p>O Vortex Bank é o laboratório de varejo da Vortex Software. Conta, Pix, boleto, cartão e uma vitrine de investimentos. O dinheiro do ledger é fictício.</p>
            <div className="hero-actions">
              <Link className="site-cta" to="/entrar">Abrir a demonstração →</Link>
              <a href="http://localhost:8080/swagger">Esquemas para o avaliador</a>
            </div>
          </div>
          <aside className="arch" aria-label="O que o simulador cobre">
            <article><span>EXPERIÊNCIA<br /><strong>Web &amp; celular</strong></span><b>01</b></article>
            <article><span>ENGENHARIA<br /><strong>Ledger de partidas</strong></span><b>02</b></article>
            <article><span>INTEGRAÇÃO<br /><strong>Auth e transações</strong></span><b>03</b></article>
          </aside>
        </div>
      </main>
    </>
  );
}

function Enter({ setSession }: { setSession: (session: Session) => void }) {
  const navigate = useNavigate();
  const [error, setError] = useState("");

  async function enter(email: string, password: string) {
    setError("");
    try {
      const session = await login(email, password);
      setSession(session);
      navigate("/app");
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "Falha no acesso.");
    }
  }

  function enterByCpf(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    const digits = String(data.get("cpf")).replace(/\D/g, "");
    const person = demos.find((item) => item[3].replace(/\D/g, "") === digits);
    if (!person) {
      setError("CPF não encontrado neste simulador.");
      return;
    }
    void enter(person[1], String(data.get("password")));
  }

  return (
    <main id="conteudo" className="login-screen">
      <section className="login-card">
        <div className="mark-box"><Shield light /></div>
        <h1>Vortex<span>Bank</span></h1>
        <p className="sub">Web banking de varejo e ledger de partidas dobradas</p>
        <span className="badge">PORTFÓLIO / LABORATÓRIO DE DEMONSTRAÇÃO</span>
        <p className="muted" style={{ marginTop: 16 }}>Contas de um clique</p>
        <div className="one-click">
          {demos.map(([name, email, password, cpf]) => (
            <button key={cpf} className="persona" type="button" onClick={() => enter(email, password)}>
              <span className="avatar">{initials(name)}</span>
              <span><strong>{name}</strong><small>{cpf}</small></span>
            </button>
          ))}
        </div>
        <div className="rule">OU CREDENCIAIS MANUAIS</div>
        <form onSubmit={enterByCpf}>
          <label>CPF do titular<input name="cpf" inputMode="numeric" placeholder="000.000.000-00" required /></label>
          <label>Senha de acesso<input name="password" type="password" placeholder="••••••••" required /></label>
          {error && <p className="error">{error}</p>}
          <button className="button button-wide" type="submit">Acessar conta segura →</button>
        </form>
        <p className="fine">versão 1.0 · Vortex Software</p>
      </section>
    </main>
  );
}

function Signup({ setSession }: { setSession: (session: Session) => void }) {
  const navigate = useNavigate();
  const [error, setError] = useState("");
  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    try {
      const session = await register(String(data.get("name")), String(data.get("email")), String(data.get("cpf")), String(data.get("password")));
      setSession(session);
      navigate("/app");
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "Não foi possível cadastrar.");
    }
  }
  return (
    <main id="conteudo" className="login-screen">
      <section className="login-card">
        <div className="mark-box"><Shield light /></div>
        <h1>Nova <span>conta</span></h1>
        <p className="sub">Saldo inicial de R$ 2.500,00, só dentro do simulador</p>
        <form onSubmit={onSubmit}>
          <label>Nome<input name="name" required /></label>
          <label>E-mail<input name="email" type="email" required /></label>
          <label>CPF<input name="cpf" inputMode="numeric" required /></label>
          <label>Senha<input name="password" type="password" minLength={8} required /></label>
          {error && <p className="error">{error}</p>}
          <button className="button button-wide" type="submit">Criar conta</button>
        </form>
      </section>
    </main>
  );
}

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
  "/app/mais": "Mais áreas",
};

function Bank({ session, onLeave }: { session: Session; onLeave: () => void }) {
  const navigate = useNavigate();
  const location = useLocation();
  const [home, setHome] = useState<Home | null>(null);
  const [error, setError] = useState("");

  async function load() {
    setHome(await bank.home());
  }

  useEffect(() => { load().catch((caught) => setError(caught.message)); }, []);

  const fontSteps = [15, 17, 19];
  const [fontStep, setFontStep] = useState(() => Number(sessionStorage.getItem("vb-font") ?? 0));
  useEffect(() => {
    const safe = Number.isInteger(fontStep) && fontStep >= 0 && fontStep < fontSteps.length ? fontStep : 0;
    document.documentElement.style.fontSize = `${fontSteps[safe]}px`;
    sessionStorage.setItem("vb-font", String(safe));
    return () => { document.documentElement.style.fontSize = ""; };
  }, [fontStep]);

  const checking = home?.accounts.find((account) => account.kind === "corrente");

  return (
    <div className="app-shell">
      <aside className="sider">
        <Link className="sider-brand" to="/app"><span className="mark-box" style={{ width: 36, height: 36, borderRadius: 10, margin: 0 }}><Shield light /></span> Vortex Bank</Link>
        <nav aria-label="Conta">
          {nav.map(([to, label]) => <NavLink key={to} to={to} end={to === "/app"}>{label}</NavLink>)}
        </nav>
        <div className="sider-user">
          <span className="avatar">{initials(session.name)}</span>
          <span><strong>{session.name}</strong><br /><small className="muted">Ag 0001 · {checking?.number ?? "—"}</small></span>
        </div>
        <button className="button-ghost" onClick={() => { logout(); onLeave(); navigate("/"); }}>Sair</button>
      </aside>
      <div className="workspace">
        <header className="topbar">
          <span>{titles[location.pathname] ?? "Vortex Bank · Simulado"}</span>
          <div className="pills">
            <span className="pill live">Ledger conectado</span>
            <button className="pill" type="button" onClick={() => setFontStep((current) => (current + 1) % fontSteps.length)}>A+ Fonte</button>
            <a className="pill" href="/swagger" target="_blank" rel="noreferrer">Swagger</a>
          </div>
        </header>
        <main id="conteudo">
          {error && <p className="error">{error}</p>}
          <Routes>
            <Route index element={<Overview home={home} name={session.name} />} />
            <Route path="extrato" element={<Statement home={home} holder={session.name} cpf={session.cpf} />} />
            <Route path="pix" element={<Pix home={home} onDone={load} />} />
            <Route path="mais" element={<More />} />
            <Route path="boletos" element={<Boletos home={home} onDone={load} />} />
            <Route path="cartoes" element={<Cards home={home} onDone={load} />} />
            <Route path="investimentos" element={<Investments />} />
            <Route path="perfil" element={<Profile session={session} home={home} />} />
            <Route path="comprovante/:id" element={<ReceiptPage />} />
          </Routes>
        </main>
      </div>
      <nav className="bottom" aria-label="Atalhos">
        <NavLink to="/app" end>Início</NavLink>
        <NavLink to="/app/pix">Pix</NavLink>
        <NavLink to="/app/extrato">Extrato</NavLink>
        <NavLink to="/app/cartoes">Cartões</NavLink>
        <NavLink to="/app/mais">Mais</NavLink>
      </nav>
    </div>
  );
}

function Overview({ home, name }: { home: Home | null; name: string }) {
  const [hidden, setHidden] = useState(false);
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
          <span className="avatar-lg">{initials(name)}</span>
          <div>
            <h1>Olá, {name.split(" ")[0]}</h1>
            <p className="muted" style={{ margin: 0 }}>Vortex Bank · Ag 0001 · Cc {checking?.number}</p>
          </div>
        </div>
        <button className="button-ghost" onClick={() => setHidden((value) => !value)}>{hidden ? "Mostrar" : "Ocultar"}</button>
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
            <div className="row"><span>{name.toUpperCase()}</span><span>{credit?.openInvoiceDue ? `Vence ${day(credit.openInvoiceDue)}` : credit?.expiry}</span></div>
          </div>
          <p className="muted">Fatura {money(credit?.openInvoiceAmount ?? 0)} · usado {money(credit?.used ?? 0)}</p>
        </article>
      </section>
    </>
  );
}

const knownPeople: Record<string, string> = {
  "39053344705": "Ana Ribeiro",
  "52998224725": "Bruno Lima",
  "11144477735": "Carla Mendes",
};

function kindLabel(kind: string) {
  const names: Record<string, string> = {
    Seed: "Saldo inicial",
    Pix: "Pix",
    Ted: "TED",
    TedFee: "Tarifa TED",
    TedFeeReversal: "Estorno de tarifa",
    Boleto: "Pagamento de boleto",
    SavingsTransfer: "Transferência",
    SavingsYield: "Rendimento",
    DebitPurchase: "Compra no débito",
    CreditPurchase: "Compra no crédito",
    InvoicePayment: "Pagamento de fatura",
  };
  return names[kind] ?? kind;
}

function maskCpf(value: string) {
  const digits = value.replace(/\D/g, "");
  if (digits.length !== 11) return value;
  return `${digits.slice(0, 3)}.${digits.slice(3, 6)}.${digits.slice(6, 9)}-${digits.slice(9)}`;
}

function whenLabel(line: Line) {
  if (!line.createdAt) return day(line.businessDate);
  return new Date(line.createdAt).toLocaleString("pt-BR", {
    timeZone: "America/Sao_Paulo",
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
  });
}

const monthLong = ["janeiro", "fevereiro", "março", "abril", "maio", "junho", "julho", "agosto", "setembro", "outubro", "novembro", "dezembro"];
const monthShort = ["JAN", "FEV", "MAR", "ABR", "MAI", "JUN", "JUL", "AGO", "SET", "OUT", "NOV", "DEZ"];
const weekdays = ["domingo", "segunda", "terça", "quarta", "quinta", "sexta", "sábado"];

function dayTitle(iso: string) {
  const [year, month, date] = iso.split("-").map(Number);
  const weekday = weekdays[new Date(year, month - 1, date).getDay()];
  return `${date} de ${monthLong[month - 1]}, ${weekday}`;
}

function Statement({ home, holder, cpf }: { home: Home | null; holder: string; cpf: string }) {
  const [accountId, setAccountId] = useState("");
  const [lines, setLines] = useState<Line[]>([]);
  const [query, setQuery] = useState("");
  const [month, setMonth] = useState("");
  useEffect(() => {
    const checking = home?.accounts.find((account) => account.kind === "corrente");
    if (checking && !accountId) setAccountId(checking.id);
  }, [home, accountId]);
  useEffect(() => { if (accountId) bank.statement(accountId).then(setLines); }, [accountId]);
  const account = home?.accounts.find((item) => item.id === accountId);
  const ordered = [...lines].sort((a, b) => ((a.createdAt ?? a.businessDate) < (b.createdAt ?? b.businessDate) ? 1 : -1));
  const months = [...new Set(ordered.map((line) => line.businessDate.slice(0, 7)))].sort();
  const selected = month || months.at(-1) || "";
  const filtered = ordered.filter((line) => line.businessDate.startsWith(selected) && `${line.description} ${kindLabel(line.kind)}`.toLowerCase().includes(query.toLowerCase()));
  const groups: { date: string; balance: number; lines: Line[] }[] = [];
  let running = account?.balance ?? 0;
  for (const line of ordered) {
    const current = groups[groups.length - 1];
    if (!current || current.date !== line.businessDate) groups.push({ date: line.businessDate, balance: running, lines: [] });
    groups[groups.length - 1].lines.unshift(line);
    running += line.direction === "credito" ? -line.amount : line.amount;
  }
  const visible = groups.filter((group) => group.date.startsWith(selected)).map((group) => ({
    ...group,
    lines: group.lines.filter((line) => filtered.includes(line)),
  })).filter((group) => group.lines.length > 0);
  const credits = filtered.filter((line) => line.direction === "credito").reduce((sum, line) => sum + line.amount, 0);
  const debits = filtered.filter((line) => line.direction === "debito").reduce((sum, line) => sum + line.amount, 0);
  const issuedAt = new Date().toLocaleString("pt-BR", { timeZone: "America/Sao_Paulo" });
  const accountName = account?.kind === "corrente" ? "Conta corrente" : "Poupança";
  const year = selected.slice(0, 4);

  function downloadPdf() {
    if (!account) return;
    saveStatementPdf({
      holder,
      cpf: maskCpf(cpf),
      account: accountName,
      number: account.number,
      issuedAt,
      credits: `+ ${money(credits)}`,
      debits: `- ${money(debits)}`,
      groups: visible.map((group) => ({
        day: dayTitle(group.date),
        balance: money(group.balance),
        lines: group.lines.map((line) => ({
          when: whenLabel(line),
          title: `${kindLabel(line.kind)} - ${line.direction === "debito" ? "Enviado" : "Recebido"}`,
          detail: line.description,
          amount: `${line.direction === "debito" ? "-" : "+"} ${money(line.amount)}`,
          debit: line.direction === "debito",
        })),
      })),
    });
  }

  return (
    <section className="work">
      <article className="panel">
        <h2 className="page-title" style={{ fontSize: 28 }}>Extrato</h2>
        <p>Selecione o extrato que deseja detalhar.</p>
        <div className="chips" role="tablist">
          {(home?.accounts ?? []).map((item) => (
            <button key={item.id} type="button" className="chip" style={item.id === accountId ? { borderColor: "var(--primary)", color: "var(--primary)" } : undefined} onClick={() => { setAccountId(item.id); setMonth(""); }}>
              {item.kind === "corrente" ? "Conta corrente" : "Poupança"}
            </button>
          ))}
        </div>
        <div className="field-row">
          <div className="chips" style={{ margin: 0 }}>
            <span className="pill">{year || "—"}</span>
            {months.map((item) => (
              <button key={item} type="button" className="chip" style={item === selected ? { borderColor: "var(--primary)", color: "var(--primary)" } : undefined} onClick={() => setMonth(item)}>
                {monthShort[Number(item.slice(5)) - 1]}
              </button>
            ))}
          </div>
          <button className="button" type="button" onClick={downloadPdf}>Salvar PDF</button>
        </div>
        <label>Filtrar por
          <input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Filtrar por..." />
        </label>
        {visible.map((group) => (
          <div key={group.date}>
            <div className="day-bar">{dayTitle(group.date)}</div>
            <div className="stmt-line"><span>Saldo do dia</span><strong>{money(group.balance)}</strong></div>
            {group.lines.map((line, index) => (
              <Link className="stmt-line" key={`${line.journalId}-${index}`} to={`/app/comprovante/${line.journalId}`}>
                <span>
                  <strong>{kindLabel(line.kind)} · {line.direction === "debito" ? "enviado" : "recebido"}</strong>
                  <small>{whenLabel(line)} · {line.description}</small>
                </span>
                <strong className={line.direction === "debito" ? "debit" : "credit"}>{line.direction === "debito" ? "−" : "+"} {money(line.amount)}</strong>
              </Link>
            ))}
          </div>
        ))}
        {visible.length === 0 && <p className="muted">Nenhum lançamento neste mês.</p>}
        <p style={{ textAlign: "center" }}><span className="credit">Entradas + {money(credits)}</span> · <span className="debit">Saídas − {money(debits)}</span></p>
      </article>
      <aside className="stack">
        <article className="panel">
          <strong>Saldos</strong>
          <div className="limit-line"><span>Saldo</span><strong>{account ? money(account.balance) : "—"}</strong></div>
          <div className="limit-line"><span>{accountName}</span><strong>{account ? money(account.balance) : "—"}</strong></div>
          <div className="limit-line"><span>Saldo disponível</span><strong>{account ? money(account.balance) : "—"}</strong></div>
          <p className="muted">{holder}<br />CPF {maskCpf(cpf)} · Ag. 0001 · Cc. {account?.number ?? "—"}</p>
        </article>
        <article className="panel">
          <strong>Lançamentos futuros</strong>
          <p className="amount" style={{ fontSize: 28 }}>{money(0)}</p>
        </article>
        <article className="panel">
          <strong>Mais opções</strong>
          <button className="hub-card" type="button" onClick={downloadPdf}><span className="qi">↓</span><span><strong>Salvar extrato em PDF</strong><p>O arquivo sai agrupado por dia, com saldo do dia, descrição e valor.</p></span></button>
        </article>
      </aside>
    </section>
  );
}

function Pix({ home, onDone }: { home: Home | null; onDone: () => Promise<void> }) {
  const [view, setView] = useState<"menu" | "send" | "keys" | "limits" | "fake">("menu");
  const [fakeTitle, setFakeTitle] = useState("");
  const [key, setKey] = useState("52998224725");
  const [amount, setAmount] = useState("1.00");
  const [message, setMessage] = useState("");
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
    try {
      const receipt = await bank.pix(digits || key.trim(), Number(amount)) as Receipt;
      setMessage(`PIX enviado. ${receipt.authentication}`);
      await onDone();
    } catch (caught) {
      setMessage(caught instanceof Error ? caught.message : "Falha no PIX.");
    }
  }

  return (
    <>
      <header className="area-head">
        <h2>Central Pix</h2>
        <p className="muted">Pix interno entre clientes do Vortex Bank. QR Code, copia e cola e MED são simulação de tela e não gravam no ledger.</p>
      </header>
      {view === "menu" && (
        <>
          <p className="hub-label">PAGAR</p>
          <div className="hub-grid">
            <button className="hub-card" type="button" onClick={() => setView("send")}><span className="qi">↗</span><span><strong>Fazer um Pix</strong><p>Transfere no ledger interno por CPF. Debita a origem e credita o destino.</p></span></button>
            <button className="hub-card" type="button" onClick={() => openFake("Ler QR Code")}><span className="qi">▣</span><span><strong>Ler QR Code</strong><p>Simulação. Não lê câmera nem paga um código de fora.</p></span></button>
            <button className="hub-card" type="button" onClick={() => openFake("Pix Copia e Cola")}><span className="qi">⌘</span><span><strong>Pix Copia e Cola</strong><p>Simulação. O código colado não liquida no ledger.</p></span></button>
            <button className="hub-card" type="button" onClick={() => openFake("Pix de presente")}><span className="qi">✦</span><span><strong>Pix de presente <span className="pill">Simulado</span></strong><p>Cartão comemorativo de demonstração. Não envia valor.</p></span></button>
          </div>
          <p className="hub-label">RECEBER</p>
          <div className="hub-grid">
            <button className="hub-card" type="button" onClick={() => openFake("Criar QR Code")}><span className="qi">▣</span><span><strong>Criar QR Code</strong><p>Simulação de cobrança. Não gera um código pagável.</p></span></button>
            <button className="hub-card" type="button" onClick={() => setView("keys")}><span className="qi">◉</span><span><strong>Minhas chaves Pix</strong><p>A chave de CPF desta conta, para outro cliente do laboratório enviar.</p></span></button>
          </div>
          <p className="hub-label">CONSULTAR</p>
          <div className="hub-list">
            <Link className="hub-card" to="/app/extrato"><span className="qi">≡</span><span><strong>Extrato Pix</strong><p>Lançamentos e comprovantes que de fato passaram pelo ledger.</p></span></Link>
            <button className="hub-card" type="button" onClick={() => setView("limits")}><span className="qi">▤</span><span><strong>Meus limites Pix</strong><p>Limite diário de R$ 20.000,00 neste simulador. Tarifa zero.</p></span></button>
            <button className="hub-card warn" type="button" onClick={() => openFake("Informar golpe")}><span className="qi">!</span><span><strong>Informar golpe ou fraude <span className="pill">Simulado</span></strong><p>Não existe devolução especial aqui. Nenhum estorno é disparado.</p></span></button>
          </div>
        </>
      )}
      {view === "send" && (
        <section className="work">
          <form className="panel" onSubmit={onSubmit}>
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
            {message && <p className={message.startsWith("PIX enviado") ? "credit" : "error"}>{message}</p>}
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
            <button type="button" className="chip" onClick={() => { navigator.clipboard.writeText(ownKey); setCopied(true); }}>{copied ? "Copiada" : "Copiar"}</button>
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
          <p className="note">Esta opção existe para a central parecer completa. Ela não lê QR, não cola payload e não estorna valor. O saldo permanece o do ledger.</p>
        </article>
      )}
    </>
  );
}

function Profile({ session, home }: { session: Session; home: Home | null }) {
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
          <button className="button button-wide" type="button" onClick={() => setNotice("Nome, CPF e e-mail já são os da conta. Telefone e endereço não são gravados.")}>Salvar alterações cadastrais</button>
          {notice && <p className="note">{notice}</p>}
        </article>
        <div className="stack">
          <article className="panel">
            <strong>Segurança</strong>
            <div className="key-box" style={{ marginTop: 12 }}><span><strong>Biometria</strong><br /><small className="muted">Vitrine. Não autentica compra.</small></span><input type="checkbox" checked={bio} onChange={() => setBio((value) => !value)} aria-label="Biometria simulada" /></div>
            <div className="key-box"><span><strong>Token</strong><br /><small className="muted">Não há segundo fator nesta versão.</small></span><span className="pill">Simulado</span></div>
            <button className="button-ghost" type="button" style={{ width: "100%", marginTop: 8 }} onClick={() => setNotice("A senha dos titulares de demonstração não é alterada por esta tela.")}>Alterar senha de acesso</button>
          </article>
          <article className="panel">
            <strong>Dispositivos</strong>
            <div className="limit-line"><span>Este navegador<br /><small className="muted">Sessão atual</small></span><span className="pill live">Ativo</span></div>
            <p className="muted">Não há outro aparelho conectado de verdade.</p>
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

function More() {
  return (
    <section className="quick">
      <Link to="/app/boletos"><span className="qi">▤</span>Boletos</Link>
      <Link to="/app/cartoes"><span className="qi">▭</span>Cartões</Link>
      <Link to="/app/investimentos"><span className="qi">%</span>Investimentos</Link>
    </section>
  );
}

const autoDebit = [
  ["Enel Energia", "Débito ilustrativo todo dia 15"],
  ["Sabesp Água", "Débito ilustrativo todo dia 18"],
  ["Fibra Internet", "Débito ilustrativo todo dia 22"],
];

function Boletos({ home, onDone }: { home: Home | null; onDone: () => Promise<void> }) {
  const [message, setMessage] = useState("");
  async function pay(line: string) {
    try {
      await bank.payBoleto(line);
      setMessage("Boleto pago. O valor saiu da conta corrente.");
      await onDone();
    } catch (caught) {
      setMessage(caught instanceof Error ? caught.message : "Não pagou.");
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
        <button type="button" onClick={() => setMessage("A agenda DDA é vitrine. Só os boletos da lista abaixo podem ser pagos.")}><span className="qi">▦</span>Agenda DDA</button>
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
            <span style={{ textAlign: "right" }}><strong>{money(boleto.amount)}</strong><br /><button className="chip" type="button" onClick={() => pay(boleto.line)}>Pagar</button></span>
          </div>
        ))}
        {open.length === 0 && <p className="muted">Nenhum boleto em aberto.</p>}
        {message && <p>{message}</p>}
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
        <p className="muted">O ledger não aplica esse teto. Ele só recusa quando o saldo da corrente não cobre o boleto.</p>
      </article>
    </div>
  );
}

function Cards({ home, onDone }: { home: Home | null; onDone: () => Promise<void> }) {
  const [message, setMessage] = useState("");
  const [showCvv, setShowCvv] = useState(false);
  const credit = home?.cards.find((card) => card.kind === "credito");
  const debit = home?.cards.find((card) => card.kind === "debito");
  const used = credit?.used ?? 0;
  const limit = credit?.limit ?? 0;
  const ratio = limit > 0 ? Math.min(100, (used / limit) * 100) : 0;

  async function payInvoice() {
    if (!credit?.openInvoiceId) return;
    try {
      await bank.payInvoice(credit.openInvoiceId);
      setMessage("Fatura paga com a conta corrente.");
      await onDone();
    } catch (caught) {
      setMessage(caught instanceof Error ? caught.message : "Não foi possível pagar a fatura.");
    }
  }

  async function buy(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    try {
      await bank.buy(String(data.get("cardId")), String(data.get("merchant")), Number(data.get("amount")));
      setMessage("Compra lançada no ledger.");
      await onDone();
    } catch (caught) {
      setMessage(caught instanceof Error ? caught.message : "Compra recusada.");
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
            {credit?.openInvoiceId ? <button className="button" type="button" onClick={payInvoice}>Pagar fatura</button> : <span className="pill">Sem fatura aberta</span>}
          </div>
          <div className="card-actions">
            <button className="button-ghost" type="button" onClick={() => setMessage("Ajustar limite é vitrine. O limite deste cartão continua R$ 5.000,00.")}>Ajustar limite</button>
            <button className="button-ghost" type="button" onClick={() => setMessage("Bloquear cartão é vitrine. Nenhuma compra foi impedida no ledger.")}>Bloquear cartão</button>
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
            <button className="button-ghost" type="button" onClick={() => setMessage("Gerar novo cartão é vitrine. O número deste plástico não muda.")}>Gerar novo cartão</button>
          </div>
        </article>
      </section>
      <section className="info-grid">
        <article className="panel"><strong>Conciliação</strong><p className="muted">Exportar OFX é vitrine. O extrato oficial é o da tela Extrato.</p></article>
        <article className="panel"><strong>Seguro</strong><p className="muted">Não há apólice. O cartão só existe dentro do simulador.</p></article>
        <article className="panel"><strong>Controle de limites</strong><p className="muted">O crédito recusa compra acima de R$ 5.000,00. Isso o ledger aplica de verdade.</p></article>
      </section>
      <form className="panel" style={{ marginTop: 16 }} onSubmit={buy}>
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
        {message && <p>{message}</p>}
      </form>
    </>
  );
}

const products = [
  ["CDB Liquidez Diária", "104% do CDI · resgate ilustrativo em D+0", "FGC", "Aplicação mínima: R$ 100,00"],
  ["LCA Sustentabilidade Agro", "98% do CDI · isento de IR na vitrine", "Isento", "Vencimento ilustrativo: 12 meses"],
  ["Tesouro Selic 2029", "Selic + 0,15% ao ano", "Tesouro", "Aplicação mínima: R$ 150,00"],
  ["Fundo Vortex Tech", "Rentabilidade ilustrativa em 12 meses: + 24,8%", "Fundo", "Série fictícia, sem custódia"],
];

function Investments() {
  const [aporte, setAporte] = useState(10000);
  const [notice, setNotice] = useState("");
  const noApply = () => setNotice("Este módulo não aplica nem resgata. O saldo da conta corrente permanece o do ledger.");
  return (
    <>
      <header className="area-head" style={{ display: "flex", justifyContent: "space-between", gap: 16, alignItems: "end" }}>
        <div>
          <h2>Vortex Invest</h2>
          <p className="muted">Módulo didático. Simulação de CDB a 104% do CDI. Não aplica saldo em conta.</p>
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

function ReceiptPage() {
  const { id } = useParams();
  const [receipt, setReceipt] = useState<Receipt | null>(null);
  const [missing, setMissing] = useState(false);
  useEffect(() => {
    if (!id || id === "ultimo") {
      setMissing(id === "ultimo");
      return;
    }
    bank.receipt(id).then(setReceipt).catch(() => setMissing(true));
  }, [id]);
  if (id === "ultimo") return <p className="panel">Abra um lançamento do extrato para ver o comprovante.</p>;
  if (missing) return <p className="panel">Comprovante não encontrado.</p>;
  if (!receipt) return <p>Abrindo comprovante…</p>;
  return (
    <article className="panel form-card" style={{ margin: "0 auto" }}>
      <div className="field-row">
        <span className="kicker" style={{ color: "var(--primary)" }}>COMPROVANTE</span>
        <span className="pill live">Liquidado no ledger</span>
      </div>
      <h2 className="page-title">{receipt.description}</h2>
      <p className="amount">{money(receipt.amount)}</p>
      <div className="limit-line"><span>Dia útil</span><strong>{day(receipt.businessDate)}</strong></div>
      <div className="limit-line"><span>Tipo</span><strong>{receipt.kind}</strong></div>
      <div className="limit-line"><span>Autenticação</span><strong>{receipt.authentication}</strong></div>
      <p className="note">Documento de demonstração da Vortex Software. Não é comprovante da rede real. A liquidação ocorreu no livro-razão do Bankcore.</p>
    </article>
  );
}
