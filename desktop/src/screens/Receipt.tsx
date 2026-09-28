import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import { explain, receipt, type Receipt } from "../api";
import { day, money } from "../format";

export function ReceiptPage() {
  const { id } = useParams();
  const [proof, setProof] = useState<Receipt | null>(null);
  const [missing, setMissing] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    if (!id || id === "ultimo") {
      setMissing(id === "ultimo");
      setProof(null);
      return;
    }
    let alive = true;
    receipt(id).then((found) => {
      if (!alive) return;
      setProof(found);
      setMissing(false);
    }).catch((caught) => {
      if (!alive) return;
      setProof(null);
      setMissing(true);
      setError(explain(caught));
    });
    return () => {
      alive = false;
    };
  }, [id]);

  if (id === "ultimo") return <p className="panel">Abra um lançamento do extrato para ver o comprovante.</p>;
  if (missing) return <p className="panel">{error || "Comprovante não encontrado."}</p>;
  if (!proof) return <p>Abrindo comprovante…</p>;
  return (
    <article className="panel form-card" style={{ margin: "0 auto" }}>
      <div className="field-row">
        <span className="kicker" style={{ color: "var(--primary)" }}>COMPROVANTE</span>
        <span className="pill live">Liquidado no ledger</span>
      </div>
      <h2 className="page-title">{proof.description}</h2>
      <p className="amount">{money(proof.amount)}</p>
      <div className="limit-line"><span>Dia útil</span><strong>{day(proof.businessDate)}</strong></div>
      <div className="limit-line"><span>Tipo</span><strong>{proof.kind}</strong></div>
      <div className="limit-line"><span>Autenticação</span><strong>{proof.authentication}</strong></div>
      <p className="note">Documento de demonstração da Vortex Software. Não é comprovante de instituição real. A liquidação ocorreu no livro-razão do simulador.</p>
    </article>
  );
}
