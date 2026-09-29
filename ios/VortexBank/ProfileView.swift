import SwiftUI

struct ProfileView: View {
    var store: BankStore
    @Environment(\.fontStep) private var fontStep
    @State private var vitrine: String?

    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 16) {
                SectionLabel(text: "Meu perfil e ajustes")
                VStack(alignment: .leading, spacing: 10) {
                    row("Nome", store.user?.name ?? "—")
                    row("CPF", BankFormat.cpf(store.user?.cpf ?? ""))
                    row("E-mail", store.user?.email ?? "—")
                    row("Agência", VortexCopy.agency)
                    row("Conta", store.checking?.number.isEmpty == false ? store.checking!.number : "—")
                    if let expires = store.user?.accessExpiresUtc {
                        row("Acesso até", BankFormat.dateTime(expires))
                    }
                }
                .vortexCard()
                Text("Os itens abaixo são vitrine. Não vêm da API e não são gravados.")
                    .font(VortexFont.font(fontStep))
                    .foregroundStyle(Color.vortexSecondaryText)
                showcase("Telefone")
                showcase("Endereço")
                showcase("Biometria")
                showcase("Token")
                showcase("Troca de senha")
                showcase("Outros dispositivos")
                showcase("Dados da empresa")
            }
            .padding(16)
        }
        .alert("Vitrine", isPresented: vitrinePresented) {
            Button("OK", role: .cancel) { vitrine = nil }
        } message: {
            Text(vitrine ?? "")
        }
    }

    private func row(_ title: String, _ value: String) -> some View {
        VStack(alignment: .leading, spacing: 2) {
            Text(title)
                .font(VortexFont.font(fontStep, extra: -2))
                .foregroundStyle(Color.vortexSecondaryText)
            Text(value.isEmpty ? "—" : value)
                .font(VortexFont.font(fontStep, weight: .semibold))
                .foregroundStyle(Color.vortexText)
        }
    }

    private func showcase(_ title: String) -> some View {
        Button {
            vitrine = "\(title) é vitrine. Não vem da API e não é gravado."
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
