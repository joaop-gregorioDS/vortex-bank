import SwiftUI

@main
struct VortexBankApp: App {
    var body: some Scene {
        WindowGroup {
            RootView()
        }
    }
}

struct RootView: View {
    @State private var store = BankStore()

    var body: some View {
        Group {
            if store.user == nil {
                LoginView(store: store)
            } else {
                ShellView(store: store)
            }
        }
        .environment(\.fontStep, store.fontStep)
        .preferredColorScheme(.light)
        .tint(.vortexAction)
        .background(Color.vortexBackground.ignoresSafeArea())
        .alert("Aviso", isPresented: errorPresented) {
            Button("OK", role: .cancel) { store.errorMessage = nil }
        } message: {
            Text(store.errorMessage ?? "")
        }
        .alert("Pronto", isPresented: noticePresented) {
            Button("OK", role: .cancel) { store.notice = nil }
        } message: {
            Text(store.notice ?? "")
        }
    }

    private var errorPresented: Binding<Bool> {
        Binding(
            get: { store.errorMessage != nil },
            set: { if !$0 { store.errorMessage = nil } }
        )
    }

    private var noticePresented: Binding<Bool> {
        Binding(
            get: { store.notice != nil && store.errorMessage == nil },
            set: { if !$0 { store.notice = nil } }
        )
    }
}
