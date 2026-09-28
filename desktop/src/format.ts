import type { Line } from "./api";

export function money(value: number) {
  return new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL" }).format(value);
}

export function parseAmount(value: string) {
  const text = value.trim();
  if (text.includes(",") && text.includes(".")) return Number(text.replace(/\./g, "").replace(",", "."));
  if (text.includes(",")) return Number(text.replace(",", "."));
  return Number(text);
}

export function day(value: string) {
  const [year, month, date] = value.split("-");
  if (!year || !month || !date) return value;
  return `${date}/${month}/${year}`;
}

export function initials(name: string) {
  return name
    .split(" ")
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase())
    .join("");
}

export function maskCpf(value: string) {
  const digits = value.replace(/\D/g, "");
  if (digits.length !== 11) return value;
  return `${digits.slice(0, 3)}.${digits.slice(3, 6)}.${digits.slice(6, 9)}-${digits.slice(9)}`;
}

export function kindLabel(kind: string) {
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

export function whenLabel(line: Line) {
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

export const monthLong = ["janeiro", "fevereiro", "março", "abril", "maio", "junho", "julho", "agosto", "setembro", "outubro", "novembro", "dezembro"];
export const monthShort = ["JAN", "FEV", "MAR", "ABR", "MAI", "JUN", "JUL", "AGO", "SET", "OUT", "NOV", "DEZ"];
const weekdays = ["domingo", "segunda", "terça", "quarta", "quinta", "sexta", "sábado"];

export function dayTitle(iso: string) {
  const [year, month, date] = iso.split("-").map(Number);
  const weekday = weekdays[new Date(year, month - 1, date).getDay()];
  return `${date} de ${monthLong[month - 1]}, ${weekday}`;
}
