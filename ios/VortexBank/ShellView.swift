import SwiftUI

struct ShellView: View {
    @Bindable var store: BankStore
    @Environment(\.openURL) private var openURL
    @Environment(\.fontStep) private var fontStep

    var body: some View {
        VStack(spacing: 0) {
            chrome
            DemoBanner()
            Group {
                switch store.tab {
                case .home: HomeView(store: store)
                case .pix: PixView(store: store)
                case .statement: StatementView(store: store)
                case .cards: CardsView(store: store)
                case .more: MoreView(store: store)
                }
            }
            .frame(maxWidth: .infinity, maxHeight: .infinity)
        }
        .background(Color.vortexBackground)
        .safeAreaInset(edge: .bottom, spacing: 0) {
            VortexTabBar(selection: $store.tab)
        }
        .task { await store.refreshLedger() }
    }

    private var chrome: some View {
        HStack(spacing: 8) {
            Button {
                Task { await store.refreshLedger() }
            } label: {
                HStack(spacing: 6) {
                    Circle()
                        .fill(ledgerColor)
                        .frame(width: 8, height: 8)
                    Text(store.ledger.title)
                        .font(VortexFont.font(fontStep, extra: -2, weight: .semibold))
                        .lineLimit(1)
                }
            }
            .buttonStyle(.plain)
            .foregroundStyle(Color.vortexText)
            .accessibilityLabel(store.ledger.title)
            Spacer(minLength: 4)
            Button {
                store.cycleFont()
            } label: {
                Text("A+ Fonte")
                    .font(VortexFont.font(fontStep, extra: -2, weight: .bold))
                    .padding(.horizontal, 10)
                    .padding(.vertical, 6)
                    .background(Color.white, in: Capsule())
            }
            .buttonStyle(.plain)
            .foregroundStyle(Color.vortexAction)
            .accessibilityLabel("A+ Fonte")
            Button {
                openURL(BankAPI.swagger)
            } label: {
                Text("Swagger")
                    .font(VortexFont.font(fontStep, extra: -2, weight: .bold))
                    .padding(.horizontal, 10)
                    .padding(.vertical, 6)
                    .background(Color.white, in: Capsule())
            }
            .buttonStyle(.plain)
            .foregroundStyle(Color.vortexAction)
            .accessibilityLabel("Swagger")
        }
        .padding(.horizontal, 16)
        .padding(.vertical, 10)
        .background(Color.vortexBackground)
    }

    private var ledgerColor: Color {
        switch store.ledger {
        case .connected: .vortexCredit
        case .unavailable: .vortexAction
        case .unknown: .vortexSecondaryText
        }
    }
}
