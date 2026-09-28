import { useEffect, useState } from "react";
import { Navigate, Route, Routes } from "react-router-dom";
import { currentSession, type Session } from "./api";
import { Cards } from "./screens/Cards";
import { Invest } from "./screens/Invest";
import { Login } from "./screens/Login";
import { Overview } from "./screens/Overview";
import { Payments } from "./screens/Payments";
import { Pix } from "./screens/Pix";
import { Profile } from "./screens/Profile";
import { ReceiptPage } from "./screens/Receipt";
import { Shell } from "./screens/Shell";
import { Statement } from "./screens/Statement";

export default function App() {
  const [session, setSession] = useState<Session | null>(null);
  const [ready, setReady] = useState(false);

  useEffect(() => {
    currentSession()
      .then((found) => setSession(found))
      .catch(() => setSession(null))
      .finally(() => setReady(true));
  }, []);

  if (!ready) {
    return (
      <div className="app-frame">
        <p className="sim-banner">Ambiente simulado. Nenhum valor é real.</p>
        <p className="login-screen">Abrindo o simulador…</p>
      </div>
    );
  }

  return (
    <Routes>
      <Route path="/" element={session ? <Navigate to="/app" replace /> : <Login onEnter={setSession} />} />
      <Route path="/app" element={session ? <Shell session={session} onLeave={() => setSession(null)} /> : <Navigate to="/" replace />}>
        <Route index element={<Overview />} />
        <Route path="extrato" element={<Statement />} />
        <Route path="pix" element={<Pix />} />
        <Route path="boletos" element={<Payments />} />
        <Route path="cartoes" element={<Cards />} />
        <Route path="investimentos" element={<Invest />} />
        <Route path="perfil" element={<Profile />} />
        <Route path="comprovante/:id" element={<ReceiptPage />} />
      </Route>
    </Routes>
  );
}
