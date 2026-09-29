import SwiftUI
import UIKit

struct StatementView: View {
    var store: BankStore
    @Environment(\.fontStep) private var fontStep
    @State private var kind: AccountKind = .corrente
    @State private var month: MonthKey?
    @State private var filter = ""
    @State private var receiptID: String?
    @State private var shareURL: URL?
    @State private var exportEmpty = false

    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 14) {
                SectionLabel(text: "Extrato")
                Picker("Conta", selection: $kind) {
                    Text("Conta corrente").tag(AccountKind.corrente)
                    Text("Poupança").tag(AccountKind.poupanca)
                }
                .pickerStyle(.segmented)
                if let account {
                    Text("Agência \(VortexCopy.agency) · Conta \(account.number.isEmpty ? "—" : account.number)")
                        .font(VortexFont.font(fontStep, extra: -1))
                        .foregroundStyle(Color.vortexSecondaryText)
                    months
                    TextField("Filtrar", text: $filter)
                        .font(VortexFont.font(fontStep))
                        .padding(12)
                        .background(Color.white, in: RoundedRectangle(cornerRadius: 12, style: .continuous))
                    Button("Gerar PDF") { exportPDF(account) }
                        .font(VortexFont.font(fontStep, weight: .bold))
                        .foregroundStyle(.white)
                        .padding(.horizontal, 14)
                        .padding(.vertical, 10)
                        .background(Color.vortexAction, in: Capsule())
                    if sections.isEmpty {
                        Text("Nenhum lançamento neste recorte.")
                            .font(VortexFont.font(fontStep))
                            .foregroundStyle(Color.vortexSecondaryText)
                            .vortexCard()
                    } else {
                        ForEach(sections) { section in
                            VStack(alignment: .leading, spacing: 4) {
                                HStack(alignment: .firstTextBaseline) {
                                    Text(BankFormat.day(section.day))
                                        .font(VortexFont.font(fontStep, weight: .bold))
                                        .foregroundStyle(Color.vortexText)
                                    Spacer()
                                    Text(section.balance.map { "Saldo do dia \(BankFormat.currency($0))" } ?? "Saldo do dia indisponível")
                                        .font(VortexFont.font(fontStep, extra: -2, weight: .semibold))
                                        .foregroundStyle(Color.vortexSecondaryText)
                                        .multilineTextAlignment(.trailing)
                                }
                                ForEach(section.entries) { entry in
                                    Button {
                                        receiptID = entry.id
                                    } label: {
                                        LedgerRow(entry: entry)
                                    }
                                    .buttonStyle(.plain)
                                }
                            }
                            .vortexCard()
                        }
                        let totals = StatementMath.totals(sections)
                        HStack {
                            Text("Entradas \(BankFormat.currency(totals.credits))")
                                .foregroundStyle(Color.vortexCredit)
                            Spacer()
                            Text("Saídas \(BankFormat.currency(totals.debits))")
                                .foregroundStyle(Color.vortexAction)
                        }
                        .font(VortexFont.font(fontStep, weight: .bold))
                        .vortexCard()
                    }
                } else {
                    Text(kind == .corrente ? "Não há conta corrente nesta sessão." : "Não há conta poupança nesta sessão.")
                        .font(VortexFont.font(fontStep))
                        .foregroundStyle(Color.vortexSecondaryText)
                        .vortexCard()
                }
            }
            .padding(16)
        }
        .refreshable { await load() }
        .task(id: kind) { await load() }
        .sheet(item: receiptBinding) { route in
            ReceiptView(store: store, journalId: route.id)
        }
        .sheet(isPresented: sharePresented) {
            if let shareURL {
                ShareSheet(items: [shareURL])
            }
        }
        .alert("Nada para exportar", isPresented: $exportEmpty) {
            Button("OK", role: .cancel) {}
        } message: {
            Text("Este recorte não tem lançamentos.")
        }
    }

    private var account: BankAccount? {
        kind == .poupanca ? store.savings : store.checking
    }

    private var entries: [LedgerEntry] {
        guard let account else { return [] }
        return store.statements[account.id] ?? []
    }

    private var activeMonth: MonthKey? {
        let options = StatementMath.months(in: entries)
        if let month, options.contains(month) { return month }
        return options.first
    }

    private var sections: [DaySection] {
        StatementMath.sections(
            entries: entries,
            closingBalance: account?.balance,
            month: activeMonth,
            text: filter
        )
    }

    private var months: some View {
        ScrollView(.horizontal, showsIndicators: false) {
            HStack(spacing: 8) {
                ForEach(StatementMath.months(in: entries)) { item in
                    Button {
                        month = item
                    } label: {
                        Text(BankFormat.month(item))
                            .font(VortexFont.font(fontStep, extra: -1, weight: .semibold))
                            .foregroundStyle(item == activeMonth ? Color.white : Color.vortexText)
                            .padding(.horizontal, 12)
                            .padding(.vertical, 8)
                            .background(item == activeMonth ? Color.vortexAction : Color.white, in: Capsule())
                    }
                    .buttonStyle(.plain)
                }
            }
        }
    }

    private var receiptBinding: Binding<JournalRoute?> {
        Binding(get: { receiptID.map(JournalRoute.init(id:)) }, set: { receiptID = $0?.id })
    }

    private var sharePresented: Binding<Bool> {
        Binding(get: { shareURL != nil }, set: { if !$0 { shareURL = nil } })
    }

    private func load() async {
        guard let account else { return }
        await store.loadStatement(accountId: account.id)
    }

    private func exportPDF(_ account: BankAccount) {
        guard !sections.isEmpty else {
            exportEmpty = true
            return
        }
        let data = StatementPDF.render(
            holderName: store.user?.name ?? "",
            cpf: store.user?.cpf ?? "",
            accountNumber: account.number,
            accountTitle: account.kind.title,
            sections: sections
        )
        let url = FileManager.default.temporaryDirectory.appendingPathComponent("Extrato-Vortex-Bank.pdf")
        do {
            try data.write(to: url, options: .atomic)
            shareURL = url
        } catch {
            store.errorMessage = "Não foi possível gerar o PDF."
        }
    }
}

struct ShareSheet: UIViewControllerRepresentable {
    var items: [Any]

    func makeUIViewController(context: Context) -> UIActivityViewController {
        UIActivityViewController(activityItems: items, applicationActivities: nil)
    }

    func updateUIViewController(_ controller: UIActivityViewController, context: Context) {}
}
