import SwiftUI

extension View {
    func vortexCard() -> some View {
        padding(16)
            .frame(maxWidth: .infinity, alignment: .leading)
            .background(Color.white, in: RoundedRectangle(cornerRadius: 18, style: .continuous))
            .overlay(
                RoundedRectangle(cornerRadius: 18, style: .continuous)
                    .stroke(Color.vortexLine, lineWidth: 1)
            )
    }
}

struct DemoBanner: View {
    @Environment(\.fontStep) private var fontStep

    var body: some View {
        Text(VortexCopy.simulated)
            .font(VortexFont.font(fontStep, extra: -2, weight: .semibold))
            .foregroundStyle(Color.vortexDeep)
            .frame(maxWidth: .infinity)
            .padding(.vertical, 8)
            .padding(.horizontal, 12)
            .background(Color.vortexPink.opacity(0.28))
            .accessibilityAddTraits(.isHeader)
    }
}

struct FilledButton: View {
    var title: String
    var busy: Bool = false
    var action: () -> Void
    @Environment(\.fontStep) private var fontStep

    var body: some View {
        Button(action: action) {
            HStack(spacing: 8) {
                if busy { ProgressView().tint(.white) }
                Text(title)
                    .font(VortexFont.font(fontStep, weight: .bold))
            }
            .frame(maxWidth: .infinity)
            .padding(.vertical, 14)
        }
        .buttonStyle(FilledActionStyle())
        .disabled(busy)
    }
}

struct FilledActionStyle: ButtonStyle {
    func makeBody(configuration: Configuration) -> some View {
        configuration.label
            .foregroundStyle(.white)
            .background(
                configuration.isPressed ? Color.vortexPressed : Color.vortexAction,
                in: RoundedRectangle(cornerRadius: 14, style: .continuous)
            )
    }
}

struct SairButton: View {
    var busy: Bool
    var action: () -> Void
    @Environment(\.fontStep) private var fontStep

    var body: some View {
        Button(action: action) {
            Text("Sair")
                .font(VortexFont.font(fontStep, weight: .bold))
                .frame(maxWidth: .infinity)
                .padding(.vertical, 14)
        }
        .buttonStyle(SairStyle())
        .disabled(busy)
        .accessibilityLabel("Sair")
    }
}

struct SairStyle: ButtonStyle {
    func makeBody(configuration: Configuration) -> some View {
        configuration.label
            .foregroundStyle(.white)
            .background(
                configuration.isPressed ? Color.vortexPressed : Color.vortexAction,
                in: RoundedRectangle(cornerRadius: 14, style: .continuous)
            )
    }
}

struct VortexTabBar: View {
    @Binding var selection: AppTab
    @Environment(\.fontStep) private var fontStep

    var body: some View {
        HStack(spacing: 4) {
            ForEach(AppTab.allCases) { tab in
                Button {
                    selection = tab
                } label: {
                    VStack(spacing: 3) {
                        Image(systemName: tab.symbol)
                            .font(.system(size: 16, weight: .semibold))
                        Text(tab.title)
                            .font(VortexFont.font(fontStep, extra: -2, weight: .semibold))
                            .lineLimit(1)
                            .minimumScaleFactor(0.7)
                    }
                    .frame(maxWidth: .infinity)
                    .padding(.vertical, 8)
                }
                .buttonStyle(TabItemStyle(selected: selection == tab))
                .accessibilityLabel(tab.title)
                .accessibilityAddTraits(selection == tab ? .isSelected : [])
            }
        }
        .padding(.horizontal, 8)
        .padding(.top, 6)
        .padding(.bottom, 6)
        .background(Color.white)
        .overlay(alignment: .top) {
            Rectangle().fill(Color.vortexLine).frame(height: 1)
        }
    }
}

struct TabItemStyle: ButtonStyle {
    var selected: Bool

    func makeBody(configuration: Configuration) -> some View {
        configuration.label
            .foregroundStyle((selected || configuration.isPressed) ? Color.white : Color.vortexSecondaryText)
            .background(
                (selected || configuration.isPressed) ? Color.vortexAction : Color.clear,
                in: RoundedRectangle(cornerRadius: 12, style: .continuous)
            )
    }
}

struct LedgerRow: View {
    var entry: LedgerEntry
    @Environment(\.fontStep) private var fontStep

    var body: some View {
        HStack(alignment: .top, spacing: 12) {
            VStack(alignment: .leading, spacing: 3) {
                Text(entry.title)
                    .font(VortexFont.font(fontStep, weight: .semibold))
                    .foregroundStyle(Color.vortexText)
                Text(entry.subtitle)
                    .font(VortexFont.font(fontStep, extra: -2))
                    .foregroundStyle(Color.vortexSecondaryText)
            }
            Spacer(minLength: 8)
            Text(BankFormat.signedCurrency(amount: entry.amount, credit: entry.credit))
                .font(VortexFont.font(fontStep, weight: .bold))
                .foregroundStyle(entry.credit ? Color.vortexCredit : Color.vortexAction)
                .multilineTextAlignment(.trailing)
        }
        .padding(.vertical, 8)
    }
}

struct SectionLabel: View {
    var text: String
    @Environment(\.fontStep) private var fontStep

    var body: some View {
        Text(text)
            .font(VortexFont.font(fontStep, extra: 2, weight: .bold))
            .foregroundStyle(Color.vortexText)
    }
}

struct VitrineChip: View {
    @Environment(\.fontStep) private var fontStep

    var body: some View {
        Text("Vitrine")
            .font(VortexFont.font(fontStep, extra: -3, weight: .bold))
            .foregroundStyle(Color.vortexAction)
            .padding(.horizontal, 8)
            .padding(.vertical, 3)
            .background(Color.vortexPink.opacity(0.25), in: Capsule())
    }
}

struct FieldLabel: View {
    var title: String
    var text: Binding<String>
    var secure: Bool = false
    var keyboard: UIKeyboardType = .default
    @Environment(\.fontStep) private var fontStep

    var body: some View {
        VStack(alignment: .leading, spacing: 6) {
            Text(title)
                .font(VortexFont.font(fontStep, extra: -2, weight: .semibold))
                .foregroundStyle(Color.vortexSecondaryText)
            Group {
                if secure {
                    SecureField("", text: text)
                } else {
                    TextField("", text: text)
                        .keyboardType(keyboard)
                }
            }
            .font(VortexFont.font(fontStep))
            .foregroundStyle(Color.vortexText)
            .padding(12)
            .background(Color.vortexBackground, in: RoundedRectangle(cornerRadius: 12, style: .continuous))
        }
    }
}
