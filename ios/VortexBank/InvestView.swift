import SwiftUI

struct InvestView: View {
    @Environment(\.fontStep) private var fontStep
    @State private var amount = ""
    @State private var vitrine: String?

    private let products: [(String, String)] = [
        ("Liquidez diária simulada", "Resgate ilustrativo. Não movimenta a conta."),
        ("CDB simulado", "Referência de 104% do CDI. Não é aplicação real."),
        ("Tesouro simulado", "Cenário fixo de 12 meses, igual para qualquer cliente.")
    ]

    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 16) {
                SectionLabel(text: "Vortex Invest")
                Text("Números de cenário. Investir e novo aporte não alteram o saldo.")
                    .font(VortexFont.font(fontStep))
                    .foregroundStyle(Color.vortexSecondaryText)
                VStack(alignment: .leading, spacing: 8) {
                    Text("Patrimônio ilustrativo")
                        .font(VortexFont.font(fontStep, extra: -1, weight: .semibold))
                    Text(BankFormat.currency(InvestScenario.patrimony))
                        .font(VortexFont.font(fontStep, extra: 12, weight: .bold))
                    Text("Referência \(InvestScenario.cdiReference)")
                        .font(VortexFont.font(fontStep))
                    Text("Liquidez \(BankFormat.currency(InvestScenario.liquidity))")
                        .font(VortexFont.font(fontStep, weight: .semibold))
                }
                .foregroundStyle(.white)
                .padding(18)
                .frame(maxWidth: .infinity, alignment: .leading)
                .background(LinearGradient.vortexBrand, in: RoundedRectangle(cornerRadius: 18, style: .continuous))
                VStack(alignment: .leading, spacing: 12) {
                    Text("Produtos")
                        .font(VortexFont.font(fontStep, weight: .bold))
                        .foregroundStyle(Color.vortexText)
                    ForEach(products, id: \.0) { product in
                        VStack(alignment: .leading, spacing: 2) {
                            Text(product.0)
                                .font(VortexFont.font(fontStep, weight: .semibold))
                                .foregroundStyle(Color.vortexText)
                            Text(product.1)
                                .font(VortexFont.font(fontStep, extra: -1))
                                .foregroundStyle(Color.vortexSecondaryText)
                        }
                    }
                }
                .vortexCard()
                VStack(alignment: .leading, spacing: 12) {
                    Text("Simulador de 12 meses")
                        .font(VortexFont.font(fontStep, weight: .bold))
                        .foregroundStyle(Color.vortexText)
                    FieldLabel(title: "Aporte", text: $amount, keyboard: .decimalPad)
                    if let value = Money.parseUser(amount) {
                        result("Em 12 meses", InvestScenario.twelveMonths(value))
                        result("Poupança antiga no mesmo prazo", InvestScenario.oldSavings(value))
                        result("Diferença ilustrativa", InvestScenario.twelveMonths(value) - InvestScenario.oldSavings(value))
                    } else {
                        Text("Informe um aporte maior que zero. Em 12 meses o cenário rende o aporte vezes 1,1144. A poupança antiga rende o aporte vezes 1,0617.")
                            .font(VortexFont.font(fontStep, extra: -1))
                            .foregroundStyle(Color.vortexSecondaryText)
                    }
                }
                .vortexCard()
                FilledButton(title: "Investir") { vitrine = "Investir é vitrine. Não altera o saldo." }
                FilledButton(title: "Novo aporte") { vitrine = "Novo aporte é vitrine. Não altera o saldo." }
            }
            .padding(16)
        }
        .alert("Vitrine", isPresented: vitrinePresented) {
            Button("OK", role: .cancel) { vitrine = nil }
        } message: {
            Text(vitrine ?? "")
        }
    }

    private func result(_ title: String, _ value: Decimal) -> some View {
        HStack {
            Text(title)
                .foregroundStyle(Color.vortexSecondaryText)
            Spacer()
            Text(BankFormat.currency(value))
                .foregroundStyle(Color.vortexText)
        }
        .font(VortexFont.font(fontStep, weight: .semibold))
    }

    private var vitrinePresented: Binding<Bool> {
        Binding(get: { vitrine != nil }, set: { if !$0 { vitrine = nil } })
    }
}
