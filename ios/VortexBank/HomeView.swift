import SwiftUI

struct HomeView: View {
    var store: BankStore
    @Environment(\.fontStep) private var fontStep
    @State private var receiptID: String?

    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 16) {
                VStack(alignment: .leading, spacing: 4) {
                    Text(BankFormat.greeting())
                        .font(VortexFont.font(fontStep))
                        .foregroundStyle(Color.vortexSecondaryText)
                    Text(BankFormat.firstName(store.user?.name ?? ""))
                        .font(VortexFont.font(fontStep, extra: 10, weight: .bold))
                        .foregroundStyle(Color.vortexText)
                }
                balanceCard
                boletoCard
                shortcuts
                if let card = featuredCard {
                    cardPreview(card)
                }
                recent
            }
            .padding(16)
        }
        .refreshable { await store.reload() }
        .sheet(item: receiptBinding) { route in
            ReceiptView(store: store, journalId: route.id)
        }
    }

    private var receiptBinding: Binding<JournalRoute?> {
        Binding(
            get: { receiptID.map(JournalRoute.init(id:)) },
            set: { receiptID = $0?.id }
        )
    }

    private var featuredCard: BankCard? {
        store.home?.cards.first { $0.product == .credit } ?? store.home?.cards.first
    }

    private var balanceCard: some View {
        VStack(alignment: .leading, spacing: 8) {
            Text("Conta corrente")
                .font(VortexFont.font(fontStep, extra: -1, weight: .semibold))
            Text("Agência \(VortexCopy.agency) · Conta \(store.checking?.number.isEmpty == false ? store.checking!.number : "—")")
                .font(VortexFont.font(fontStep, extra: -2))
                .opacity(0.9)
            Text(BankFormat.currency(store.checking?.balance ?? 0))
                .font(VortexFont.font(fontStep, extra: 16, weight: .bold))
                .padding(.top, 6)
        }
        .foregroundStyle(.white)
        .padding(18)
        .frame(maxWidth: .infinity, alignment: .leading)
        .background(LinearGradient.vortexBrand, in: RoundedRectangle(cornerRadius: 18, style: .continuous))
    }

    private var boletoCard: some View {
        VStack(alignment: .leading, spacing: 6) {
            Text("Boletos em aberto")
                .font(VortexFont.font(fontStep, extra: -1, weight: .semibold))
                .foregroundStyle(Color.vortexSecondaryText)
            Text(BankFormat.currency(store.openBoletosTotal))
                .font(VortexFont.font(fontStep, extra: 8, weight: .bold))
                .foregroundStyle(Color.vortexText)
            Button("Ver boletos") { store.openPayments() }
                .font(VortexFont.font(fontStep, weight: .bold))
                .foregroundStyle(Color.vortexAction)
        }
        .vortexCard()
    }

    private var shortcuts: some View {
        HStack(spacing: 10) {
            shortcut("Pix", "paperplane.fill") { store.tab = .pix }
            shortcut("Boletos", "barcode") { store.openPayments() }
            shortcut("Extrato", "list.bullet.rectangle.fill") { store.tab = .statement }
            shortcut("Cartões", "creditcard.fill") { store.tab = .cards }
        }
    }

    private func shortcut(_ title: String, _ symbol: String, action: @escaping () -> Void) -> some View {
        Button(action: action) {
            VStack(spacing: 8) {
                Image(systemName: symbol)
                    .font(.system(size: 18, weight: .semibold))
                Text(title)
                    .font(VortexFont.font(fontStep, extra: -2, weight: .semibold))
                    .lineLimit(1)
                    .minimumScaleFactor(0.7)
            }
            .foregroundStyle(Color.vortexAction)
            .frame(maxWidth: .infinity)
            .padding(.vertical, 12)
            .background(Color.white, in: RoundedRectangle(cornerRadius: 16, style: .continuous))
        }
        .buttonStyle(.plain)
    }

    private func cardPreview(_ card: BankCard) -> some View {
        VStack(alignment: .leading, spacing: 8) {
            HStack {
                Text(card.label)
                    .font(VortexFont.font(fontStep, weight: .bold))
                Spacer()
                Text(card.product.title)
                    .font(VortexFont.font(fontStep, extra: -2, weight: .semibold))
            }
            Text(card.last4.isEmpty ? "••••" : "•••• \(card.last4)")
                .font(VortexFont.font(fontStep, extra: 4, weight: .semibold))
            if let invoice = card.invoiceAmount {
                Text("Fatura \(BankFormat.currency(invoice))")
                    .font(VortexFont.font(fontStep, extra: -1))
                    .opacity(0.9)
            }
        }
        .foregroundStyle(.white)
        .padding(18)
        .frame(maxWidth: .infinity, alignment: .leading)
        .background(Color.vortexText, in: RoundedRectangle(cornerRadius: 18, style: .continuous))
    }

    private var recent: some View {
        VStack(alignment: .leading, spacing: 4) {
            SectionLabel(text: "Últimos lançamentos")
            if store.home?.recent.isEmpty != false {
                Text("Nenhum lançamento recente.")
                    .font(VortexFont.font(fontStep))
                    .foregroundStyle(Color.vortexSecondaryText)
                    .padding(.top, 8)
            } else {
                ForEach(store.home?.recent ?? []) { entry in
                    Button {
                        receiptID = entry.id
                    } label: {
                        LedgerRow(entry: entry)
                    }
                    .buttonStyle(.plain)
                }
            }
        }
        .vortexCard()
    }
}

struct JournalRoute: Identifiable, Hashable {
    var id: String
}
