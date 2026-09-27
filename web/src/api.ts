export type Session = {
  userId: string;
  name: string;
  email: string;
  cpf: string;
  accessToken: string;
};

let accessToken: string | null = null;

export function token() {
  return accessToken;
}

export function money(value: number) {
  return new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL" }).format(value);
}

export function day(value: string) {
  const [year, month, date] = value.split("-");
  return `${date}/${month}/${year}`;
}

async function call<T>(path: string, init: RequestInit = {}, auth = true): Promise<T> {
  const headers = new Headers(init.headers);
  if (auth && accessToken) headers.set("Authorization", `Bearer ${accessToken}`);
  if (init.body && !headers.has("Content-Type")) headers.set("Content-Type", "application/json");
  const response = await fetch(path, { ...init, headers, credentials: "include" });
  if (response.status === 401 && auth && !path.endsWith("/refresh")) {
    const renewed = await refresh();
    if (renewed) return call<T>(path, init, auth);
  }
  if (!response.ok) {
    const problem = await response.json().catch(() => ({ message: "Não foi possível concluir." }));
    throw new Error(problem.message ?? "Não foi possível concluir.");
  }
  if (response.status === 204) return undefined as T;
  return response.json() as Promise<T>;
}

export async function refresh() {
  const response = await fetch("/api/auth/refresh", { method: "POST", credentials: "include" });
  if (!response.ok) return false;
  const body = (await response.json()) as Session;
  accessToken = body.accessToken;
  return true;
}

export async function login(email: string, password: string) {
  const session = await call<Session>("/api/auth/login", {
    method: "POST",
    body: JSON.stringify({ email, password }),
  }, false);
  accessToken = session.accessToken;
  await call("/api/transactions/me/provision", {
    method: "POST",
    body: JSON.stringify({ name: session.name, cpf: session.cpf }),
  });
  return session;
}

export async function register(name: string, email: string, cpf: string, password: string) {
  const session = await call<Session>("/api/auth/register", {
    method: "POST",
    body: JSON.stringify({ name, email, cpf, password }),
  }, false);
  accessToken = session.accessToken;
  await call("/api/transactions/me/provision", {
    method: "POST",
    body: JSON.stringify({ name, cpf }),
  });
  return session;
}

export function logout() {
  accessToken = null;
  return fetch("/api/auth/logout", { method: "POST", credentials: "include" });
}

function idempotent(path: string, body: unknown) {
  return call(path, {
    method: "POST",
    headers: { "Idempotency-Key": crypto.randomUUID() },
    body: JSON.stringify(body),
  });
}

export const bank = {
  home: () => call<Home>("/api/transactions/home"),
  statement: (accountId: string) => call<Line[]>(`/api/transactions/statement?accountId=${accountId}`),
  receipt: (id: string) => call<Receipt>(`/api/transactions/receipts/${id}`),
  pix: (key: string, amount: number) => idempotent("/api/transactions/pix", { key, amount }),
  addKey: (kind: string, value: string) => call("/api/transactions/pix/keys", { method: "POST", body: JSON.stringify({ kind, value }) }),
  ted: (agency: string, number: string, amount: number) => idempotent("/api/transactions/ted", { agency, number, amount }),
  savings: (direction: string, amount: number) => idempotent("/api/transactions/savings", { direction, amount }),
  issueBoleto: (amount: number, dueDate: string, description: string) =>
    call("/api/transactions/boletos", { method: "POST", body: JSON.stringify({ amount, dueDate, description }) }),
  payBoleto: (line: string) => idempotent("/api/transactions/boletos/pay", { line }),
  buy: (cardId: string, merchant: string, amount: number) => idempotent("/api/transactions/cards/purchases", { cardId, merchant, amount }),
  payInvoice: (invoiceId: string) => idempotent(`/api/transactions/cards/invoices/${invoiceId}/pay`, {}),
  advance: () => call<Clock>("/api/transactions/clock/advance", { method: "POST" }),
};

export type Account = { id: string; kind: string; agency: string; number: string; balance: number };
export type Card = {
  id: string; kind: string; pan: string; holder: string; expiry: string; cvv: string;
  limit: number | null; used: number | null; openInvoiceId: string | null; openInvoiceAmount: number | null; openInvoiceDue: string | null;
};
export type PixKey = { id: string; kind: string; value: string };
export type Boleto = { id: string; line: string; beneficiary: string; amount: number; dueDate: string; status: string; external: boolean; mine: boolean };
export type Ted = { id: string; amount: number; fee: number; status: string; agency: string; number: string; scheduledFor: string };
export type Line = { journalId: string; businessDate: string; createdAt?: string; kind: string; direction: string; amount: number; description: string };
export type Receipt = { journalId: string; businessDate: string; kind: string; amount: number; description: string; authentication: string };
export type Clock = { businessDate: string };
export type Home = {
  businessDate: string;
  accounts: Account[];
  cards: Card[];
  pixKeys: PixKey[];
  boletos: Boleto[];
  teds: Ted[];
  recent: Line[];
};
