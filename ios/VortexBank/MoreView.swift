import SwiftUI

struct MoreView: View {
    @Bindable var store: BankStore
    @Environment(\.fontStep) private var fontStep

    var body: some View {
        NavigationStack(path: $store.morePath) {
            ScrollView {
                VStack(alignment: .leading, spacing: 12) {
                    SectionLabel(text: "Mais")
                    link("Central de Pagamentos", "barcode", .payments)
                    link("Vortex Invest", "chart.line.uptrend.xyaxis", .invest)
                    link("Meu perfil e ajustes", "person.crop.circle", .profile)
                    SairButton(busy: store.isActing) {
                        Task { await store.logout() }
                    }
                    .padding(.top, 8)
                }
                .padding(16)
            }
            .background(Color.vortexBackground)
            .navigationDestination(for: MoreRoute.self) { route in
                destination(route)
                    .navigationBarTitleDisplayMode(.inline)
            }
        }
    }

    private func link(_ title: String, _ symbol: String, _ route: MoreRoute) -> some View {
        NavigationLink(value: route) {
            HStack(spacing: 12) {
                Image(systemName: symbol)
                    .foregroundStyle(Color.vortexAction)
                    .frame(width: 28)
                Text(title)
                    .font(VortexFont.font(fontStep, weight: .semibold))
                    .foregroundStyle(Color.vortexText)
                Spacer()
                Image(systemName: "chevron.right")
                    .foregroundStyle(Color.vortexSecondaryText)
            }
            .vortexCard()
        }
        .buttonStyle(.plain)
    }

    @ViewBuilder
    private func destination(_ route: MoreRoute) -> some View {
        switch route {
        case .payments: PaymentsView(store: store)
        case .invest: InvestView()
        case .profile: ProfileView(store: store)
        }
    }
}
