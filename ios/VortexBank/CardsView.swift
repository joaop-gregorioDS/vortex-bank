import SwiftUI

struct CardsView: View {
    var store: BankStore
    @Environment(\.fontStep) private var fontStep
    @State private var revealedCVV: Set<String> = []
    @State private var purchaseCard: BankCard?
    @State private var paying: BankCard?
    @State private var confirmPay = false
    @State private var vitrine: VitrineNote?

    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 16) {
                SectionLabel(text: "Central de Cartões")
                let cards = store.home?.cards ?? []
                if cards.isEmpty {
                    Text("Nenhum cartão veio da API.")
                        .font(VortexFont.font(fontStep))
                        .foregroundStyle(Color.vortexSecondaryText)
                        .vortexCard()
                }
                ForEach(cards.filter { $0.product != .debit }) { card in
                    credit(card)
                }
                ForEach(cards.filter { $0.product == .debit }) { card in
                    debit(card)
                }
            }
            .padding(16)
        }
        .refreshable { await store.reload() }
        .sheet(item: $purchaseCard) { card in
            PurchaseSheet(store: store, card: card)
        }
        .confirmationDialog("Pagar esta fatura?", isPresented: $confirmPay, titleVisibility: .visible) {
            Button("Pagar fatura") {
                guard let paying else { return }
                Task { await store.payInvoice(paying) }
            }
            Button("Cancelar", role: .cancel) {}
        }
        .alert("Vitrine", isPresented: vitrinePresented) {
            Button("OK", role: .cancel) { vitrine = nil }
        } message: {
            Text(vitrine?.message ?? "")
        }
    }

    private func credit(_ card: BankCard) -> some View {
        VStack(alignment: .leading, spacing: 10) {
            Text("Crédito · \(card.label)")
                .font(VortexFont.font(fontStep, weight: .bold))
                .foregroundStyle(.white)
            Text(card.last4.isEmpty ? "••••" : "•••• \(card.last4)")
                .font(VortexFont.font(fontStep, extra: 6, weight: .bold))
                .foregroundStyle(.white)
            labeled("Limite", card.limit.map(BankFormat.currency) ?? "—")
            labeled("Disponível", card.available.map(BankFormat.currency) ?? "—")
            labeled("Fatura", card.invoiceAmount.map(BankFormat.currency) ?? "—")
            if let due = card.invoiceDue {
                labeled("Vencimento", BankFormat.day(due))
            }
            if let status = card.invoiceStatus, !status.isEmpty {
                labeled("Situação", status)
            }
            FilledButton(title: "Pagar fatura", busy: store.isActing) {
            paying = card
            confirmPay = true
        }
            FilledButton(title: "Lançar compra", busy: false) { purchaseCard = card }
            vitrineButton("Ajustar limite", "Ajustar limite é vitrine. Não grava no ledger e não altera o limite vindo da API.")
            vitrineButton("Bloquear", "Bloquear o cartão é vitrine. Não grava no ledger.")
            vitrineButton("Gerar outro cartão", "Gerar outro cartão é vitrine. Não grava no ledger.")
        }
        .padding(18)
        .frame(maxWidth: .infinity, alignment: .leading)
        .background(LinearGradient.vortexBrand, in: RoundedRectangle(cornerRadius: 18, style: .continuous))
    }

    private func debit(_ card: BankCard) -> some View {
        VStack(alignment: .leading, spacing: 10) {
            Text("Débito · \(card.label)")
                .font(VortexFont.font(fontStep, weight: .bold))
                .foregroundStyle(Color.vortexText)
            Text(card.last4.isEmpty ? "••••" : "•••• \(card.last4)")
                .font(VortexFont.font(fontStep, extra: 4, weight: .bold))
                .foregroundStyle(Color.vortexText)
            if revealedCVV.contains(card.id) {
                Text("CVV \(card.cvv ?? "não veio na resposta")")
                    .font(VortexFont.font(fontStep, weight: .semibold))
                    .foregroundStyle(Color.vortexText)
            }
            Button(revealedCVV.contains(card.id) ? "Ocultar CVV" : "Mostrar CVV") {
                if revealedCVV.contains(card.id) {
                    revealedCVV.remove(card.id)
                } else {
                    revealedCVV.insert(card.id)
                }
            }
            .font(VortexFont.font(fontStep, weight: .bold))
            .foregroundStyle(Color.vortexAction)
        }
        .vortexCard()
    }

    private func labeled(_ title: String, _ value: String) -> some View {
        HStack {
            Text(title)
            Spacer()
            Text(value).multilineTextAlignment(.trailing)
        }
        .font(VortexFont.font(fontStep, extra: -1))
        .foregroundStyle(.white.opacity(0.95))
    }

    private func vitrineButton(_ title: String, _ message: String) -> some View {
        Button {
            vitrine = VitrineNote(title: title, message: message)
        } label: {
            HStack {
                Text(title)
                    .font(VortexFont.font(fontStep, weight: .semibold))
                Spacer()
                VitrineChip()
            }
            .foregroundStyle(.white)
        }
        .buttonStyle(.plain)
    }

    private var vitrinePresented: Binding<Bool> {
        Binding(get: { vitrine != nil }, set: { if !$0 { vitrine = nil } })
    }
}

struct PurchaseSheet: View {
    var store: BankStore
    var card: BankCard
    @Environment(\.dismiss) private var dismiss
    @Environment(\.fontStep) private var fontStep
    @State private var merchant: CardMerchant = .mercado
    @State private var amount = ""
    @State private var confirm = false

    var body: some View {
        NavigationStack {
            ScrollView {
                VStack(alignment: .leading, spacing: 16) {
                    Text("A compra grava no ledger do cartão \(card.last4.isEmpty ? "" : "final \(card.last4)").")
                        .font(VortexFont.font(fontStep))
                        .foregroundStyle(Color.vortexSecondaryText)
                    Picker("Estabelecimento", selection: $merchant) {
                        ForEach(CardMerchant.allCases) { item in
                            Text(item.rawValue).tag(item)
                        }
                    }
                    .pickerStyle(.menu)
                    FieldLabel(title: "Valor", text: $amount, keyboard: .decimalPad)
                    FilledButton(title: "Lançar compra", busy: store.isActing) { confirm = true }
                }
                .padding(16)
            }
            .background(Color.vortexBackground)
            .navigationTitle("Compra no cartão")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) {
                    Button("Fechar") { dismiss() }
                }
            }
            .confirmationDialog("Lançar esta compra?", isPresented: $confirm, titleVisibility: .visible) {
                Button("Lançar") {
                    Task {
                        let done = await store.purchase(card: card, merchant: merchant, amountText: amount)
                        if done { dismiss() }
                    }
                }
                Button("Cancelar", role: .cancel) {}
            }
        }
    }
}
