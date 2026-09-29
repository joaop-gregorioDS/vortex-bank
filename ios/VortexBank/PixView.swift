import SwiftUI
import UIKit

struct PixView: View {
    var store: BankStore
    @Environment(\.fontStep) private var fontStep
    @State private var sendPresented = false
    @State private var vitrine: VitrineNote?

    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 18) {
                SectionLabel(text: "Central Pix")
                block("Pagar") {
                    FilledButton(title: "Fazer um Pix", busy: false) { sendPresented = true }
                    vitrineRow("QR Code", "Leitura de QR Code é vitrine. Não grava no ledger.")
                    vitrineRow("Copia e cola", "Copia e cola é vitrine. Não grava no ledger.")
                    vitrineRow("Presente", "Pix presente é vitrine. Não grava no ledger.")
                }
                block("Receber") {
                    if store.home?.pixKeys.isEmpty != false {
                        Text("Nenhuma chave Pix veio da API.")
                            .font(VortexFont.font(fontStep))
                            .foregroundStyle(Color.vortexSecondaryText)
                    } else {
                        ForEach(store.home?.pixKeys ?? []) { key in
                            HStack {
                                VStack(alignment: .leading, spacing: 2) {
                                    Text(key.type)
                                        .font(VortexFont.font(fontStep, extra: -2, weight: .semibold))
                                        .foregroundStyle(Color.vortexSecondaryText)
                                    Text(key.value)
                                        .font(VortexFont.font(fontStep, weight: .semibold))
                                        .foregroundStyle(Color.vortexText)
                                }
                                Spacer()
                                Button("Copiar") {
                                    UIPasteboard.general.string = key.value
                                    store.notice = "Chave copiada. Isso não grava no ledger."
                                }
                                .font(VortexFont.font(fontStep, extra: -1, weight: .bold))
                                .foregroundStyle(Color.vortexAction)
                            }
                        }
                    }
                    vitrineRow("QR Code para receber", "Gerar QR Code é vitrine. Não grava no ledger.")
                }
                block("Consultar") {
                    let pixEntries = pixMovements
                    if pixEntries.isEmpty {
                        Text("Nenhum Pix recente neste extrato.")
                            .font(VortexFont.font(fontStep))
                            .foregroundStyle(Color.vortexSecondaryText)
                    } else {
                        ForEach(pixEntries.prefix(8)) { entry in
                            LedgerRow(entry: entry)
                        }
                    }
                    Button("Atualizar movimentos") {
                        guard let account = store.checking else { return }
                        Task { await store.loadStatement(accountId: account.id) }
                    }
                    .font(VortexFont.font(fontStep, weight: .bold))
                    .foregroundStyle(Color.vortexAction)
                    vitrineRow("Golpe", "Aviso de golpe é vitrine. Não grava no ledger.")
                }
            }
            .padding(16)
        }
        .sheet(isPresented: $sendPresented) {
            PixSendSheet(store: store)
        }
        .alert("Vitrine", isPresented: vitrinePresented) {
            Button("OK", role: .cancel) { vitrine = nil }
        } message: {
            Text(vitrine?.message ?? "")
        }
        .task {
            if let account = store.checking, store.statements[account.id] == nil {
                await store.loadStatement(accountId: account.id)
            }
        }
    }

    private var pixMovements: [LedgerEntry] {
        guard let id = store.checking?.id else { return [] }
        return (store.statements[id] ?? []).filter { entry in
            "\(entry.rail) \(entry.title) \(entry.detail)".bankFolded.contains("pix")
        }.sorted { $0.createdAt > $1.createdAt }
    }

    private func block(_ title: String, @ViewBuilder content: () -> some View) -> some View {
        VStack(alignment: .leading, spacing: 12) {
            Text(title)
                .font(VortexFont.font(fontStep, extra: 1, weight: .bold))
                .foregroundStyle(Color.vortexText)
            content()
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
        }
        .buttonStyle(.plain)
    }

    private var vitrinePresented: Binding<Bool> {
        Binding(get: { vitrine != nil }, set: { if !$0 { vitrine = nil } })
    }
}

struct VitrineNote: Identifiable {
    var title: String
    var message: String
    var id: String { title }
}

struct PixSendSheet: View {
    var store: BankStore
    @Environment(\.dismiss) private var dismiss
    @Environment(\.fontStep) private var fontStep
    @State private var key = ""
    @State private var amount = ""
    @State private var confirm = false

    var body: some View {
        NavigationStack {
            ScrollView {
                VStack(alignment: .leading, spacing: 16) {
                    Text("Só esta ação grava no ledger. A chave de CPF segue com 11 dígitos.")
                        .font(VortexFont.font(fontStep))
                        .foregroundStyle(Color.vortexSecondaryText)
                    FieldLabel(title: "Chave Pix", text: $key)
                    FieldLabel(title: "Valor", text: $amount, keyboard: .decimalPad)
                    FilledButton(title: "Enviar", busy: store.isActing) { confirm = true }
                }
                .padding(16)
            }
            .background(Color.vortexBackground)
            .navigationTitle("Fazer um Pix")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) {
                    Button("Fechar") { dismiss() }
                }
            }
            .confirmationDialog("Enviar este Pix?", isPresented: $confirm, titleVisibility: .visible) {
                Button("Enviar") {
                    Task {
                        let sent = await store.sendPix(key: key, amountText: amount)
                        if sent { dismiss() }
                    }
                }
                Button("Cancelar", role: .cancel) {}
            }
        }
    }
}
