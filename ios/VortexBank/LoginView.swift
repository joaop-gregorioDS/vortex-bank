import SwiftUI

struct LoginView: View {
    var store: BankStore
    @Environment(\.fontStep) private var fontStep
    @State private var cpf = ""
    @State private var password = ""
    @FocusState private var focused: Bool

    var body: some View {
        NavigationStack {
            login
        }
    }

    private var login: some View {
        ScrollView {
            VStack(spacing: 20) {
                DemoBanner()
                VStack(spacing: 18) {
                    Image(systemName: "shield.fill")
                        .font(.system(size: 28, weight: .semibold))
                        .foregroundStyle(.white)
                        .frame(width: 64, height: 64)
                        .background(Color.vortexAction, in: Circle())
                    HStack(spacing: 0) {
                        Text("Vortex")
                            .foregroundStyle(Color.vortexText)
                        Text("Bank")
                            .foregroundStyle(Color.vortexAction)
                    }
                    .font(VortexFont.font(fontStep, extra: 14, weight: .bold))
                    Text("Entre com um titular ou com CPF e senha.")
                        .font(VortexFont.font(fontStep, extra: -1))
                        .foregroundStyle(Color.vortexSecondaryText)
                        .multilineTextAlignment(.center)
                    ForEach(DemoDirectory.all) { holder in
                        Button {
                            focused = false
                            Task { await store.signIn(holder: holder) }
                        } label: {
                            HStack(spacing: 12) {
                                Text(holder.initials)
                                    .font(VortexFont.font(fontStep, weight: .bold))
                                    .foregroundStyle(.white)
                                    .frame(width: 40, height: 40)
                                    .background(Color.vortexAction, in: Circle())
                                VStack(alignment: .leading, spacing: 2) {
                                    Text(holder.name)
                                        .font(VortexFont.font(fontStep, weight: .semibold))
                                        .foregroundStyle(Color.vortexText)
                                    Text(holder.cpf)
                                        .font(VortexFont.font(fontStep, extra: -2))
                                        .foregroundStyle(Color.vortexSecondaryText)
                                }
                                Spacer()
                                Image(systemName: "chevron.right")
                                    .foregroundStyle(Color.vortexSecondaryText)
                            }
                        }
                        .buttonStyle(.plain)
                        .disabled(store.isActing)
                    }
                    Text("ou informe CPF e senha")
                        .font(VortexFont.font(fontStep, extra: -2))
                        .foregroundStyle(Color.vortexSecondaryText)
                    FieldLabel(title: "CPF", text: $cpf, keyboard: .numberPad)
                        .focused($focused)
                        .onChange(of: cpf) { _, newValue in
                            let masked = BankFormat.maskCPF(newValue)
                            if masked != newValue { cpf = masked }
                        }
                    FieldLabel(title: "Senha", text: $password, secure: true)
                        .focused($focused)
                    FilledButton(title: "Entrar", busy: store.isActing) {
                        focused = false
                        Task { await store.signIn(cpf: cpf, password: password) }
                    }
                }
                .vortexCard()
                .padding(.horizontal, 20)
            }
            .padding(.bottom, 28)
        }
        .scrollDismissesKeyboard(.interactively)
        .background(Color.vortexBackground)
        .toolbar {
            ToolbarItemGroup(placement: .keyboard) {
                Spacer()
                Button("OK") { focused = false }
            }
        }
        .toolbar(.hidden, for: .navigationBar)
    }
}
