import SwiftUI

enum VortexCopy {
    static let simulated = "Ambiente simulado. Nenhum valor é real."
    static let pdfFooter = "Documento de demonstração. Não é extrato de instituição real."
    static let agency = "0001"
}

enum FontStep: Equatable {
    case compact, regular, large

    var size: CGFloat {
        switch self {
        case .compact: 15
        case .regular: 17
        case .large: 19
        }
    }

    var next: FontStep {
        switch self {
        case .compact: .regular
        case .regular: .large
        case .large: .compact
        }
    }
}

private struct FontStepKey: EnvironmentKey {
    static let defaultValue = FontStep.compact
}

extension EnvironmentValues {
    var fontStep: FontStep {
        get { self[FontStepKey.self] }
        set { self[FontStepKey.self] = newValue }
    }
}

enum VortexFont {
    static func font(_ step: FontStep, extra: CGFloat = 0, weight: Font.Weight = .regular) -> Font {
        Font.custom("Manrope", size: max(12, step.size + extra)).weight(weight)
    }
}

extension Color {
    static let vortexAction = Color(red: 225 / 255, green: 29 / 255, blue: 72 / 255)
    static let vortexPressed = Color(red: 190 / 255, green: 18 / 255, blue: 60 / 255)
    static let vortexText = Color(red: 28 / 255, green: 20 / 255, blue: 24 / 255)
    static let vortexSecondaryText = Color(red: 109 / 255, green: 97 / 255, blue: 104 / 255)
    static let vortexBackground = Color(red: 247 / 255, green: 242 / 255, blue: 244 / 255)
    static let vortexCredit = Color(red: 21 / 255, green: 122 / 255, blue: 69 / 255)
    static let vortexPink = Color(red: 251 / 255, green: 113 / 255, blue: 133 / 255)
    static let vortexDeep = Color(red: 159 / 255, green: 18 / 255, blue: 57 / 255)
    static let vortexLine = Color(red: 28 / 255, green: 20 / 255, blue: 24 / 255).opacity(0.08)
}

extension LinearGradient {
    static let vortexBrand = LinearGradient(
        colors: [.vortexPink, .vortexAction, .vortexDeep],
        startPoint: .topLeading,
        endPoint: .bottomTrailing
    )
}

enum AppTab: String, CaseIterable, Identifiable {
    case home, pix, statement, cards, more

    var id: String { rawValue }

    var title: String {
        switch self {
        case .home: "Início"
        case .pix: "Pix"
        case .statement: "Extrato"
        case .cards: "Cartões"
        case .more: "Mais"
        }
    }

    var symbol: String {
        switch self {
        case .home: "house.fill"
        case .pix: "paperplane.fill"
        case .statement: "list.bullet.rectangle.fill"
        case .cards: "creditcard.fill"
        case .more: "ellipsis"
        }
    }
}

enum MoreRoute: Hashable {
    case payments, invest, profile
}

enum LedgerLink {
    case unknown, connected, unavailable

    var title: String {
        switch self {
        case .unknown: "Ledger"
        case .connected: "Ledger conectado"
        case .unavailable: "Ledger indisponível"
        }
    }
}

enum CardMerchant: String, CaseIterable, Identifiable {
    case mercado = "Mercado"
    case combustivel = "Combustível"
    case farmacia = "Farmácia"
    case streaming = "Streaming"

    var id: String { rawValue }
}
