import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { statement, type Line } from "../api";
import { dayTitle, kindLabel, maskCpf, money, monthShort, whenLabel } from "../format";
import { saveStatementPdf } from "../statementPdf";
import { useBank } from "./Shell";

export function Statement() {
  const { home, session } = useBank();
  const [accountId, setAccountId] = useState("");
  const [lines, setLines] = useState<Line[]>([]);
  const [query, setQuery] = useState("");
  const [month, setMonth] = useState("");

  useEffect(() => {
    const checking = home?.accounts.find((account) => account.kind === "corrente");
    if (checking && !accountId) setAccountId(checking.id);
  }, [home, accountId]);

  useEffect(() => {
    if (!accountId) return;
    let alive = true;
    statement(accountId).then((found) => {
      if (alive) setLines(found);
    }).catch(() => {
      if (alive) setLines([]);
    });
    return () => {
      alive = false;
    };
  }, [accountId]);

  const account = home?.accounts.find((item) => item.id === accountId);
  const ordered = [...lines].sort((a, b) => ((a.createdAt ?? a.businessDate) < (b.createdAt ?? b.businessDate) ? 1 : -1));
  const months = [...new Set(ordered.map((line) => line.businessDate.slice(0, 7)))].sort();
  const selected = month || months[months.length - 1] || "";
  const filtered = ordered.filter((line) => line.businessDate.startsWith(selected) && `${line.description} ${kindLabel(line.kind)}`.toLowerCase().includes(query.toLowerCase()));
  const groups: { date: string; balance: number; lines: Line[] }[] = [];
  let running = account?.balance ?? 0;
  for (const line of ordered) {
    const current = groups[groups.length - 1];
    if (!current || current.date !== line.businessDate) groups.push({ date: line.businessDate, balance: running, lines: [] });
    groups[groups.length - 1].lines.unshift(line);
    running += line.direction === "credito" ? -line.amount : line.amount;
  }
  const visible = groups
    .filter((group) => group.date.startsWith(selected))
    .map((group) => ({ ...group, lines: group.lines.filter((line) => filtered.includes(line)) }))
    .filter((group) => group.lines.length > 0);
  const credits = filtered.filter((line) => line.direction === "credito").reduce((sum, line) => sum + line.amount, 0);
  const debits = filtered.filter((line) => line.direction === "debito").reduce((sum, line) => sum + line.amount, 0);
  const issuedAt = new Date().toLocaleString("pt-BR", { timeZone: "America/Sao_Paulo" });
  const accountName = account?.kind === "corrente" ? "Conta corrente" : "Poupança";
  const year = selected.slice(0, 4);

  function downloadPdf() {
    if (!account) return;
    saveStatementPdf({
      holder: session.name,
      cpf: maskCpf(session.cpf),
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
        <h2 className="page-title">Extrato</h2>
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
          <p className="muted">{session.name}<br />CPF {maskCpf(session.cpf)} · Ag. 0001 · Cc. {account?.number ?? "—"}</p>
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
