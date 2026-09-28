import { invoke } from "@tauri-apps/api/core";

export type Session = {
  userId: string;
  name: string;
  email: string;
  cpf: string;
};

export type Account = { id: string; kind: string; agency: string; number: string; balance: number };
export type Card = {
  id: string;
  kind: string;
  pan: string;
  holder: string;
  expiry: string;
  cvv: string;
  limit: number | null;
  used: number | null;
  openInvoiceId: string | null;
  openInvoiceAmount: number | null;
  openInvoiceDue: string | null;
};
export type PixKey = { id: string; kind: string; value: string };
export type Boleto = {
  id: string;
  line: string;
  beneficiary: string;
  amount: number;
  dueDate: string;
  status: string;
  external: boolean;
  mine: boolean;
};
export type Line = {
  journalId: string;
  businessDate: string;
  createdAt?: string;
  kind: string;
  direction: string;
  amount: number;
  description: string;
};
export type Receipt = {
  journalId: string;
  businessDate: string;
  kind: string;
  amount: number;
  description: string;
  authentication: string;
};
export type Home = {
  businessDate: string;
  accounts: Account[];
  cards: Card[];
  pixKeys: PixKey[];
  boletos: Boleto[];
  recent: Line[];
};

export const holders = [
  { name: "Ana Ribeiro", email: "ana.ribeiro@vortexbank.demo", password: "Ana-demo-2026", cpf: "390.533.447-05" },
  { name: "Bruno Lima", email: "bruno.lima@vortexbank.demo", password: "Bruno-demo-2026", cpf: "529.982.247-25" },
];

export function explain(caught: unknown) {
  if (typeof caught === "string") return caught;
  if (caught instanceof Error) return caught.message;
  return "Não foi possível concluir.";
}

export function sessionEnded(caught: unknown) {
  const text = explain(caught);
  return text.includes("Entre de novo") || text.includes("Sessão expirada");
}

export function currentSession() {
  return invoke<Session | null>("current_session");
}

export function login(email: string, password: string) {
  return invoke<Session>("login", { email, password });
}

export function logout() {
  return invoke<void>("logout");
}

export function ledgerStatus() {
  return invoke<boolean>("ledger_status");
}

export function home() {
  return invoke<Home>("home");
}

export function statement(accountId: string) {
  return invoke<Line[]>("statement", { accountId });
}

export function receipt(journalId: string) {
  return invoke<Receipt>("receipt", { journalId });
}

export function sendPix(key: string, amount: number) {
  return invoke<Receipt>("pix", { key, amount });
}

export function payBoleto(line: string) {
  return invoke<Receipt>("pay_boleto", { line });
}

export function payInvoice(invoiceId: string) {
  return invoke<Receipt>("pay_invoice", { invoiceId });
}

export function purchase(cardId: string, merchant: string, amount: number) {
  return invoke<Receipt>("purchase", { cardId, merchant, amount });
}
