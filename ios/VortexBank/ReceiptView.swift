import SwiftUI

struct ReceiptView: View {
    var store: BankStore
    var journalId: String
    @Environment(\.dismiss) private var dismiss
    @Environment(\.fontStep) private var fontStep
    @State private var receipt: Receipt?
    @State private var loading = true

    var body: some View {
        NavigationStack {
            ScrollView {
                VStack(alignment: .leading, spacing: 16) {
                    if loading {
                        ProgressView("Carregando comprovante")
                            .font(VortexFont.font(fontStep))
                            .frame(maxWidth: .infinity)
                            .padding(.top, 40)
                    } else if let receipt {
                        VStack(alignment: .leading, spacing: 8) {
                            Text(receipt.entry.title)
                                .font(VortexFont.font(fontStep, extra: 4, weight: .bold))
                                .foregroundStyle(Color.vortexText)
                            Text(BankFormat.signedCurrency(amount: receipt.entry.amount, credit: receipt.entry.credit))
                                .font(VortexFont.font(fontStep, extra: 10, weight: .bold))
                                .foregroundStyle(receipt.entry.credit ? Color.vortexCredit : Color.vortexAction)
                            Text(BankFormat.dateTime(receipt.entry.createdAt))
                                .font(VortexFont.font(fontStep))
                                .foregroundStyle(Color.vortexSecondaryText)
                            if !receipt.entry.detail.isEmpty {
                                Text(receipt.entry.detail)
                                    .font(VortexFont.font(fontStep))
                                    .foregroundStyle(Color.vortexText)
                            }
                        }
                        .vortexCard()
                        if !receipt.extras.isEmpty {
                            VStack(alignment: .leading, spacing: 8) {
                                ForEach(receipt.extras) { line in
                                    VStack(alignment: .leading, spacing: 2) {
                                        Text(line.label)
                                            .font(VortexFont.font(fontStep, extra: -2))
                                            .foregroundStyle(Color.vortexSecondaryText)
                                        Text(line.value)
                                            .font(VortexFont.font(fontStep, weight: .semibold))
                                            .foregroundStyle(Color.vortexText)
                                    }
                                }
                            }
                            .vortexCard()
                        }
                    } else {
                        Text("Comprovante indisponível.")
                            .font(VortexFont.font(fontStep))
                            .foregroundStyle(Color.vortexSecondaryText)
                    }
                }
                .padding(16)
            }
            .background(Color.vortexBackground)
            .navigationTitle("Comprovante")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) {
                    Button("Fechar") { dismiss() }
                }
            }
        }
        .task {
            receipt = await store.receipt(journalId: journalId)
            loading = false
        }
    }
}
