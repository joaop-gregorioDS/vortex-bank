import { jsPDF } from "jspdf";

export function saveStatementPdf(input: {
  holder: string;
  cpf: string;
  account: string;
  number: string;
  issuedAt: string;
  groups: { day: string; balance: string; lines: { when: string; title: string; detail: string; amount: string; debit: boolean }[] }[];
  credits: string;
  debits: string;
}) {
  const doc = new jsPDF();
  const pageWidth = 210;
  let y = 18;

  doc.setFont("helvetica", "bold");
  doc.setFontSize(16);
  doc.setTextColor(28, 20, 24);
  doc.text("Vortex Bank", 14, y);
  doc.setFontSize(9);
  doc.setFont("helvetica", "normal");
  doc.setTextColor(120, 90, 100);
  doc.text(input.holder, pageWidth - 14, y, { align: "right" });
  y += 5;
  doc.setFontSize(8);
  doc.text("EXTRATO DA CONTA", 14, y);
  doc.text(`CPF ${input.cpf}`, pageWidth - 14, y, { align: "right" });
  y += 4;
  doc.text(`Ag. 0001  ·  Cc. ${input.number}  ·  ${input.account}`, pageWidth - 14, y, { align: "right" });
  y += 4;
  doc.text(`Emitido em ${input.issuedAt}`, pageWidth - 14, y, { align: "right" });
  y += 4;
  doc.setDrawColor(225, 29, 72);
  doc.setLineWidth(0.4);
  doc.line(14, y, pageWidth - 14, y);
  y += 8;

  const ensure = (need: number) => {
    if (y + need <= 280) return;
    doc.addPage();
    y = 18;
  };

  for (const group of input.groups) {
    ensure(22);
    doc.setFillColor(244, 238, 240);
    doc.rect(14, y - 4, pageWidth - 28, 8, "F");
    doc.setFont("helvetica", "bold");
    doc.setFontSize(10);
    doc.setTextColor(28, 20, 24);
    doc.text(group.day, 16, y + 1);
    y += 10;
    doc.setFont("helvetica", "normal");
    doc.text("Saldo do dia", 16, y);
    doc.text(group.balance, pageWidth - 14, y, { align: "right" });
    y += 6;
    for (const line of group.lines) {
      ensure(12);
      doc.setTextColor(28, 20, 24);
      doc.text(line.title.slice(0, 62), 16, y);
      doc.setTextColor(line.debit ? 190 : 21, line.debit ? 18 : 122, line.debit ? 60 : 69);
      doc.text(line.amount, pageWidth - 14, y, { align: "right" });
      y += 4;
      doc.setTextColor(120, 90, 100);
      doc.setFontSize(8);
      doc.text(`${line.when}  ${line.detail}`.slice(0, 90), 16, y);
      doc.setFontSize(10);
      y += 6;
    }
  }

  y += 4;
  doc.setTextColor(28, 20, 24);
  doc.setFont("helvetica", "bold");
  doc.text(`Entradas ${input.credits}    Saidas ${input.debits}`, pageWidth / 2, y, { align: "center" });
  y += 10;
  doc.setFont("helvetica", "normal");
  doc.setFontSize(8);
  doc.setTextColor(120, 90, 100);
  const note = "Vortex Software · documento de demonstracao. Nao e extrato de instituicao real. Os lancamentos refletem o livro-razao do Bankcore.";
  doc.text(note, 14, y, { maxWidth: pageWidth - 28 });
  doc.save(`extrato-vortex-${input.number.replace(/\W/g, "")}.pdf`);
}
