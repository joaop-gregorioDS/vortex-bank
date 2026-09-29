import SwiftUI

struct PaymentsView: View {
    var store: BankStore
    @Environment(\.fontStep) private var fontStep
    @State private var paying: Boleto?
    @State private var confirmPay = false
    @State private var vitrine: VitrineNote?

    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 16) {
                SectionLabel(text: "Central de Pagamentos")
                HStack {
                    metric("Saldo", BankFormat.currency(store.checking?.balance ?? 0))
                    metric("Em aberto", BankFormat.currency(store.openBoletosTotal))
                }
                let open = (store.home?.boletos ?? []).filter(\.isOpenBill)
                if open.isEmpty {
                    Text("Nenhum boleto em aberto.")
                        .font(VortexFont.font(fontStep))
                        .foregroundStyle(Color.vortexSecondaryText)
                        .vortexCard()
                } else {
                    ForEach(open) { boleto in
                        VStack(alignment: .leading, spacing: 8) {
                            Text(boleto.title)
                                .font(VortexFont.font(fontStep, weight: .bold))
                                .foregroundStyle(Color.vortexText)
                            Text(BankFormat.currency(boleto.amount))
                                .font(VortexFont.font(fontStep, extra: 4, weight: .bold))
                                .foregroundStyle(Color.vortexAction)
                            if let due = boleto.due {
                                Text("Vencimento \(BankFormat.day(due))")
                                    .font(VortexFont.font(fontStep, extra: -1))
                                    .foregroundStyle(Color.vortexSecondaryText)
                            }
                            Text(boleto.line)
                                .font(VortexFont.font(fontStep, extra: -2))
                                .foregroundStyle(Color.vortexSecondaryText)
                            FilledButton(title: "Pagar", busy: store.isActing) {
                                paying = boleto
                                confirmPay = true
                            }
                        }
                        .vortexCard()
                    }
                }
                vitrineRow("Débito automático", "Débito automático é vitrine. Não grava no ledger.")
                vitrineRow("Teto de R$ 50.000,00", "O teto de R$ 50.000,00 é vitrine. Não grava no ledger.")
            }
            .padding(16)
        }
        .refreshable { await store.reload() }
        .confirmationDialog("Pagar este boleto?", isPresented: $confirmPay, titleVisibility: .visible) {
            Button("Pagar") {
                guard let paying else { return }
                Task { await store.payBoleto(paying) }
            }
            Button("Cancelar", role: .cancel) {}
        }
        .alert("Vitrine", isPresented: vitrinePresented) {
            Button("OK", role: .cancel) { vitrine = nil }
        } message: {
            Text(vitrine?.message ?? "")
        }
    }

    private func metric(_ title: String, _ value: String) -> some View {
        VStack(alignment: .leading, spacing: 4) {
            Text(title)
                .font(VortexFont.font(fontStep, extra: -2))
                .foregroundStyle(Color.vortexSecondaryText)
            Text(value)
                .font(VortexFont.font(fontStep, extra: 2, weight: .bold))
                .foregroundStyle(Color.vortexText)
        }
        .vortexCard()
    }

    private func vitrineRow(_ title: String, _ message: String) -> some View {
        Button {
            vitrine = VitrineNote(title: title, message: message)
        } label: {
            HStack {
                Text(title)
                    .font(VortexFont.font(fontStep, weight: .semibold))
                    .foregroundStyle(Color.vortexText)
                Spacer()
                VitrineChip()
            }
            .vortexCard()
        }
        .buttonStyle(.plain)
    }

    private var vitrinePresented: Binding<Bool> {
        Binding(get: { vitrine != nil }, set: { if !$0 { vitrine = nil } })
    }
}
