import Foundation
import Observation

@MainActor
@Observable
final class BankStore {
    private let api = APIClient()

    var user: UserProfile?
    var home: HomeSnapshot?
    var statements: [String: [LedgerEntry]] = [:]
    var tab: AppTab = .home
    var morePath: [MoreRoute] = []
    var fontStep: FontStep = .compact
    var ledger: LedgerLink = .unknown
    var isActing = false
    var errorMessage: String?
    var notice: String?

    var checking: BankAccount? { home?.accounts.first { $0.kind == .corrente } }
    var savings: BankAccount? { home?.accounts.first { $0.kind == .poupanca } }
    var openBoletosTotal: Decimal { HomeMath.openBoletosTotal(home?.boletos ?? []) }

    func signIn(holder: Holder) async {
        await signIn(email: holder.email, password: holder.password)
    }

    func signIn(cpf: String, password: String) async {
        guard let holder = DemoDirectory.holder(cpf: cpf) else {
            errorMessage = "CPF não reconhecido nesta demonstração."
            return
        }
        guard !password.isEmpty else {
            errorMessage = "Informe a senha."
            return
        }
        await signIn(email: holder.email, password: password)
    }

    func signIn(email: String, password: String) async {
        errorMessage = nil
        notice = nil
        isActing = true
        defer { isActing = false }
        do {
            let profile = try await api.login(email: email, password: password)
            user = profile
            do {
                try await api.provision(name: profile.name, cpf: profile.cpf)
            } catch {
                notice = error.localizedDescription
            }
            home = try await api.home()
            await refreshLedger()
        } catch {
            if user == nil {
                errorMessage = error.localizedDescription
            } else {
                errorMessage = error.localizedDescription
            }
        }
    }

    func logout() async {
        isActing = true
        defer { isActing = false }
        await api.logout()
        resetLocal()
    }

    func reload() async {
        guard user != nil else { return }
        do {
            try await reloadThrowing()
        } catch BankError.unauthorized {
            await forceLogout()
        } catch {
            errorMessage = error.localizedDescription
        }
    }

    func loadStatement(accountId: String) async {
        guard user != nil else { return }
        do {
            statements[accountId] = try await api.statement(accountId: accountId)
        } catch BankError.unauthorized {
            await forceLogout()
        } catch {
            errorMessage = error.localizedDescription
        }
    }

    func sendPix(key: String, amountText: String) async -> Bool {
        let normalized = PixKeyNormalizer.forAPI(key)
        guard !normalized.isEmpty else {
            errorMessage = "Informe a chave Pix."
            return false
        }
        guard let amount = Money.parseUser(amountText) else {
            errorMessage = "Informe um valor maior que zero, com no máximo duas casas."
            return false
        }
        return await money {
            try await api.pix(key: normalized, amount: amount, idempotencyKey: UUID().uuidString.lowercased())
            return "Pix enviado."
        }
    }

    func payBoleto(_ boleto: Boleto) async -> Bool {
        guard !boleto.line.isEmpty else {
            errorMessage = "Este boleto não tem linha digitável."
            return false
        }
        return await money {
            try await api.payBoleto(line: boleto.line, idempotencyKey: UUID().uuidString.lowercased())
            return "Boleto pago."
        }
    }

    func payInvoice(_ card: BankCard) async -> Bool {
        guard let invoiceId = card.invoiceId, !invoiceId.isEmpty else {
            errorMessage = "Esta fatura não tem identificador para pagamento."
            return false
        }
        return await money {
            try await api.payInvoice(invoiceId: invoiceId, idempotencyKey: UUID().uuidString.lowercased())
            return "Fatura paga."
        }
    }

    func purchase(card: BankCard, merchant: CardMerchant, amountText: String) async -> Bool {
        guard let amount = Money.parseUser(amountText) else {
            errorMessage = "Informe um valor maior que zero, com no máximo duas casas."
            return false
        }
        return await money {
            try await api.purchase(
                cardId: card.id,
                merchant: merchant.rawValue,
                amount: amount,
                idempotencyKey: UUID().uuidString.lowercased()
            )
            return "Compra lançada."
        }
    }

    func receipt(journalId: String) async -> Receipt? {
        do {
            return try await api.receipt(journalId: journalId)
        } catch BankError.unauthorized {
            await forceLogout()
            return nil
        } catch {
            errorMessage = error.localizedDescription
            return nil
        }
    }

    func refreshLedger() async {
        ledger = await api.ledgerReachable() ? .connected : .unavailable
    }

    func cycleFont() {
        fontStep = fontStep.next
    }

    func openPayments() {
        tab = .more
        morePath = [.payments]
    }

    private func money(_ work: () async throws -> String) async -> Bool {
        await perform {
            let message = try await work()
            try await reloadThrowing()
            notice = message
        }
    }

    @discardableResult
    private func perform(_ work: () async throws -> Void) async -> Bool {
        errorMessage = nil
        notice = nil
        isActing = true
        defer { isActing = false }
        do {
            try await work()
            return true
        } catch BankError.unauthorized {
            await forceLogout()
            return false
        } catch {
            errorMessage = error.localizedDescription
            return false
        }
    }

    private func reloadThrowing() async throws {
        home = try await api.home()
        for id in Array(statements.keys) {
            statements[id] = try await api.statement(accountId: id)
        }
        await refreshLedger()
    }

    private func forceLogout() async {
        await api.clearSession()
        resetLocal()
        errorMessage = BankError.unauthorized.localizedDescription
    }

    private func resetLocal() {
        user = nil
        home = nil
        statements = [:]
        tab = .home
        morePath = []
        ledger = .unknown
        notice = nil
    }
}
