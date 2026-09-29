import Foundation

struct JSONValue {
    let raw: Any

    static func parse(_ data: Data) throws -> JSONValue {
        guard !data.isEmpty else { return JSONValue(raw: [String: Any]()) }
        let raw = try JSONSerialization.jsonObject(with: data, options: [.fragmentsAllowed])
        return JSONValue(raw: raw)
    }

    var object: [String: Any]? { raw as? [String: Any] }
    var array: [Any]? { raw as? [Any] }

    func value(_ keys: String...) -> JSONValue? {
        find(keys)
    }

    private func find(_ keys: [String]) -> JSONValue? {
        guard let object else { return nil }
        for key in keys {
            if let found = object.first(where: { $0.key.caseInsensitiveCompare(key) == .orderedSame }) {
                return JSONValue(raw: found.value)
            }
        }
        return nil
    }

    func string(_ keys: String...) -> String? {
        text(in: keys)
    }

    private func text(in keys: [String]) -> String? {
        guard let found = find(keys) else { return nil }
        if let text = found.raw as? String { return text }
        if let number = found.raw as? NSNumber {
            if CFGetTypeID(number) == CFBooleanGetTypeID() { return nil }
            return number.stringValue
        }
        return nil
    }

    func bool(_ keys: String...) -> Bool? {
        guard let found = find(keys) else { return nil }
        if let number = found.raw as? NSNumber, CFGetTypeID(number) == CFBooleanGetTypeID() {
            return number.boolValue
        }
        if let text = found.raw as? String {
            switch text.bankFolded {
            case "true", "1", "sim": return true
            case "false", "0", "nao": return false
            default: return nil
            }
        }
        return nil
    }

    func decimal(_ keys: String...) -> Decimal? {
        guard let found = find(keys) else { return nil }
        return Self.decimal(from: found.raw)
    }

    func date(_ keys: String...) -> Date? {
        guard let text = text(in: keys) else { return nil }
        return BankDateParser.parse(text)
    }

    func firstArray(_ keys: String...) -> [JSONValue] {
        for key in keys {
            if let found = value(key), let array = found.array {
                return array.map(JSONValue.init(raw:))
            }
        }
        return []
    }

    func preferredObject() -> JSONValue {
        let markers = ["accounts", "contas", "cards", "cartoes", "boletos", "entries", "lancamentos", "pixkeys", "chavespix"]
        if markers.contains(where: { value($0) != nil }) { return self }
        for key in ["data", "home", "payload", "result"] {
            if let inner = value(key), inner.object != nil {
                return inner.preferredObject()
            }
        }
        return self
    }

    private static func decimal(from raw: Any) -> Decimal? {
        let parsed: Decimal?
        if let text = raw as? String {
            parsed = Decimal(string: text.replacingOccurrences(of: ",", with: "."), locale: Locale(identifier: "en_US_POSIX"))
        } else if let number = raw as? NSNumber {
            if CFGetTypeID(number) == CFBooleanGetTypeID() { return nil }
            parsed = Decimal(string: number.stringValue, locale: Locale(identifier: "en_US_POSIX")) ?? number.decimalValue
        } else {
            parsed = nil
        }
        return parsed.map(Money.rounded)
    }
}

enum BankError: LocalizedError {
    case message(String)
    case unauthorized

    var errorDescription: String? {
        switch self {
        case .message(let text): text
        case .unauthorized: "Sessão expirada. Entre de novo."
        }
    }
}

struct UserProfile: Equatable {
    var userId: String
    var name: String
    var email: String
    var cpf: String
    var accessExpiresUtc: Date?
}

struct Holder: Identifiable, Equatable {
    var id: String { email }
    var name: String
    var cpf: String
    var email: String
    var password: String

    var initials: String {
        let parts = name.split(separator: " ")
        let letters = parts.prefix(2).compactMap(\.first)
        return String(letters).uppercased()
    }
}

enum DemoDirectory {
    static let ana = Holder(
        name: "Ana Ribeiro",
        cpf: "390.533.447-05",
        email: "ana.ribeiro@vortexbank.demo",
        password: "Ana-demo-2026"
    )
    static let bruno = Holder(
        name: "Bruno Lima",
        cpf: "529.982.247-25",
        email: "bruno.lima@vortexbank.demo",
        password: "Bruno-demo-2026"
    )
    static let all = [ana, bruno]

    static func holder(cpf: String) -> Holder? {
        let digits = cpf.filter(\.isNumber)
        return all.first { $0.cpf.filter(\.isNumber) == digits }
    }
}

enum AccountKind: String, Equatable {
    case corrente, poupanca, other

    static func parse(_ raw: String?) -> AccountKind {
        let text = raw?.bankFolded ?? ""
        if text.contains("poup") || text.contains("saving") { return .poupanca }
        if text.contains("corr") || text.contains("check") { return .corrente }
        return .other
    }

    var title: String {
        switch self {
        case .corrente: "Conta corrente"
        case .poupanca: "Poupança"
        case .other: "Conta"
        }
    }
}

struct BankAccount: Identifiable, Equatable {
    var id: String
    var kind: AccountKind
    var number: String
    var balance: Decimal

    static func parse(_ json: JSONValue) -> BankAccount? {
        let kind = AccountKind.parse(json.string("kind", "type", "tipo", "product"))
        let number = json.string("number", "accountNumber", "numero", "conta") ?? ""
        let balance = json.decimal("balance", "saldo", "currentBalance", "availableBalance") ?? 0
        let id = json.string("id", "accountId") ?? "\(kind.rawValue)-\(number)"
        guard !id.isEmpty else { return nil }
        return BankAccount(id: id, kind: kind, number: number, balance: balance)
    }
}

enum CardProduct: Equatable {
    case credit, debit, unknown

    var title: String {
        switch self {
        case .credit: "Crédito"
        case .debit: "Débito"
        case .unknown: "Cartão"
        }
    }
}

struct BankCard: Identifiable, Equatable {
    var id: String
    var product: CardProduct
    var label: String
    var last4: String
    var limit: Decimal?
    var available: Decimal?
    var cvv: String?
    var invoiceId: String?
    var invoiceAmount: Decimal?
    var invoiceDue: Date?
    var invoiceStatus: String?

    static func parse(_ json: JSONValue) -> BankCard? {
        let kindText = (json.string("kind", "type", "product", "modalidade") ?? "").bankFolded
        let product: CardProduct
        if kindText.contains("deb") {
            product = .debit
        } else if kindText.contains("cred") || json.value("invoice", "fatura") != nil || json.decimal("limit", "creditLimit", "limite") != nil {
            product = .credit
        } else {
            product = .unknown
        }
        let pan = json.string("last4", "lastFour", "final", "pan", "masked") ?? ""
        let digits = pan.filter(\.isNumber)
        let last4 = digits.count >= 4 ? String(digits.suffix(4)) : pan
        let id = json.string("id", "cardId") ?? "card-\(last4)"
        var card = BankCard(
            id: id,
            product: product,
            label: json.string("brand", "bandeira", "label", "name", "nome") ?? product.title,
            last4: last4,
            limit: json.decimal("limit", "creditLimit", "limite"),
            available: json.decimal("available", "availableLimit", "disponivel"),
            cvv: json.string("cvv", "cvc", "securityCode"),
            invoiceId: json.string("invoiceId"),
            invoiceAmount: json.decimal("invoiceAmount"),
            invoiceDue: json.date("invoiceDue", "dueDate"),
            invoiceStatus: json.string("invoiceStatus")
        )
        if let invoice = json.value("invoice", "fatura"), invoice.object != nil {
            card.invoiceId = invoice.string("id", "invoiceId") ?? card.invoiceId
            card.invoiceAmount = invoice.decimal("amount", "total", "value", "valor") ?? card.invoiceAmount
            card.invoiceDue = invoice.date("due", "dueDate", "vencimento") ?? card.invoiceDue
            card.invoiceStatus = invoice.string("status") ?? card.invoiceStatus
        }
        return card
    }
}

struct PixKeyItem: Identifiable, Equatable {
    var id: String
    var type: String
    var value: String

    static func parse(_ json: JSONValue) -> PixKeyItem? {
        let value = json.string("key", "value", "chave") ?? ""
        guard !value.isEmpty else { return nil }
        return PixKeyItem(
            id: json.string("id") ?? value,
            type: json.string("type", "kind", "tipo") ?? "Chave",
            value: value
        )
    }
}

struct Boleto: Identifiable, Equatable {
    var id: String
    var line: String
    var amount: Decimal
    var due: Date?
    var status: String
    var mine: Bool
    var title: String

    var isOpenBill: Bool { status.bankFolded == "aberto" && !mine }

    static func parse(_ json: JSONValue) -> Boleto? {
        let line = json.string("line", "linha", "digitableLine", "linhaDigitavel", "barcode", "code") ?? ""
        let amount = json.decimal("amount", "value", "valor") ?? 0
        let id = json.string("id", "boletoId") ?? (line.isEmpty ? UUID().uuidString : line)
        return Boleto(
            id: id,
            line: line,
            amount: amount,
            due: json.date("due", "dueDate", "vencimento"),
            status: json.string("status") ?? "",
            mine: json.bool("mine") ?? false,
            title: json.string("title", "beneficiary", "payee", "name", "description", "descricao") ?? "Boleto"
        )
    }
}

struct LedgerEntry: Identifiable, Equatable {
    var id: String
    var createdAt: Date
    var title: String
    var detail: String
    var amount: Decimal
    var credit: Bool
    var balanceAfter: Decimal?
    var rail: String

    var signed: Decimal { credit ? amount : -amount }

    var subtitle: String {
        let clock = BankFormat.time(createdAt)
        if detail.isEmpty || detail == title { return clock }
        return "\(clock) · \(detail)"
    }

    static func parse(_ json: JSONValue) -> LedgerEntry? {
        guard let createdAt = json.date("createdAt", "created_at", "occurredAt", "postedAt", "timestamp", "date") else {
            return nil
        }
        let explicitAmount = json.decimal("amount", "value", "valor", "valueAmount")
        let running = json.decimal("balanceAfter", "balance_after", "runningBalance", "saldoApos", "postBalance")
        let looseBalance = json.decimal("balance", "saldo")
        guard let rawAmount = explicitAmount ?? (running == nil ? looseBalance : nil) else { return nil }
        let balanceAfter = running ?? (explicitAmount != nil ? looseBalance : nil)
        let direction = json.string("direction", "nature", "dc", "sinal", "flow") ?? json.string("type", "kind")
        let credit: Bool
        if let flag = json.bool("credit", "isCredit") {
            credit = flag
        } else if let direction {
            let folded = direction.bankFolded
            if folded.contains("cred") || folded.contains("entrada") || folded == "c" || folded == "in" {
                credit = true
            } else if folded.contains("deb") || folded.contains("saida") || folded == "d" || folded == "out" {
                credit = false
            } else {
                credit = rawAmount >= 0
            }
        } else {
            credit = rawAmount >= 0
        }
        let title = json.string("title", "titulo", "summary", "historico", "history")
            ?? json.string("type", "kind")
            ?? "Lançamento"
        var detail = json.string("description", "descricao", "detail", "memo", "counterparty", "merchant") ?? ""
        if detail == title { detail = "" }
        let id = json.string("journalId", "journal_id", "id") ?? "\(createdAt.timeIntervalSince1970)-\(title)"
        return LedgerEntry(
            id: id,
            createdAt: createdAt,
            title: title,
            detail: detail,
            amount: abs(rawAmount),
            credit: credit,
            balanceAfter: balanceAfter,
            rail: json.string("rail", "category", "type", "kind", "modalidade") ?? ""
        )
    }
}

struct ReceiptLine: Identifiable, Equatable {
    var label: String
    var value: String
    var id: String { "\(label)-\(value)" }
}

struct Receipt: Equatable {
    var entry: LedgerEntry
    var extras: [ReceiptLine]
}

struct HomeSnapshot: Equatable {
    var accounts: [BankAccount]
    var cards: [BankCard]
    var pixKeys: [PixKeyItem]
    var boletos: [Boleto]
    var recent: [LedgerEntry]

    static func parse(_ data: Data) throws -> HomeSnapshot {
        let root = try JSONValue.parse(data).preferredObject()
        return HomeSnapshot(
            accounts: root.firstArray("accounts", "contas").compactMap(BankAccount.parse),
            cards: root.firstArray("cards", "cartoes", "cartões").compactMap(BankCard.parse),
            pixKeys: root.firstArray("pixKeys", "chavesPix", "chaves", "keys").compactMap(PixKeyItem.parse),
            boletos: root.firstArray("boletos").compactMap(Boleto.parse),
            recent: Self.entries(in: root)
        )
    }

    static func entries(in json: JSONValue) -> [LedgerEntry] {
        json.firstArray(
            "entries", "lancamentos", "recent", "recentEntries", "recentTransactions", "movements", "transactions", "statement"
        ).compactMap(LedgerEntry.parse)
    }

    static func statement(from data: Data) throws -> [LedgerEntry] {
        let root = try JSONValue.parse(data)
        if let array = root.array {
            return array.map(JSONValue.init(raw:)).compactMap(LedgerEntry.parse)
        }
        return entries(in: root.preferredObject())
    }
}

enum HomeMath {
    static func openBoletosTotal(_ boletos: [Boleto]) -> Decimal {
        boletos.reduce(Decimal(0)) { partial, boleto in
            boleto.isOpenBill ? partial + boleto.amount : partial
        }
    }
}

enum ReceiptParser {
    static func parse(_ data: Data) throws -> Receipt {
        let root = try JSONValue.parse(data).preferredObject()
        let body = nested(root) ?? root
        guard let entry = LedgerEntry.parse(body) else {
            throw BankError.message("A API não devolveu um comprovante legível.")
        }
        return Receipt(entry: entry, extras: extras(in: body))
    }

    private static func nested(_ root: JSONValue) -> JSONValue? {
        for key in ["receipt", "comprovante", "entry", "lancamento"] {
            if let inner = root.value(key), inner.object != nil, LedgerEntry.parse(inner) != nil {
                return inner
            }
        }
        return nil
    }

    private static func extras(in json: JSONValue) -> [ReceiptLine] {
        guard let object = json.object else { return [] }
        let hidden: Set<String> = [
            "title", "titulo", "summary", "historico", "history", "description", "descricao", "detail", "memo",
            "amount", "value", "valor", "createdat", "created_at", "occurredat", "postedat", "timestamp", "date",
            "direction", "credit", "iscredit", "balance", "saldo", "balanceafter", "journalid", "id", "type", "kind"
        ]
        let labels = [
            "endtoend": "Identificador",
            "e2e": "Identificador",
            "authentication": "Autenticação",
            "authcode": "Autenticação",
            "key": "Chave",
            "counterparty": "Contraparte",
            "merchant": "Estabelecimento",
            "status": "Situação"
        ]
        return object.keys.sorted().compactMap { key in
            guard !hidden.contains(key.bankFolded) else { return nil }
            let shown: String
            if let text = object[key] as? String, !text.isEmpty {
                shown = text
            } else if let number = object[key] as? NSNumber, CFGetTypeID(number) != CFBooleanGetTypeID() {
                shown = number.stringValue
            } else {
                return nil
            }
            return ReceiptLine(label: labels[key.bankFolded] ?? key, value: shown)
        }
    }
}

enum InvestScenario {
    static let patrimony = Decimal(string: "45890.20")!
    static let liquidity = Decimal(string: "25000.00")!
    static let cdiReference = "104% do CDI"
    static let twelveFactor = Decimal(string: "1.1144")!
    static let savingsFactor = Decimal(string: "1.0617")!

    static func twelveMonths(_ amount: Decimal) -> Decimal {
        Money.rounded(amount * twelveFactor)
    }

    static func oldSavings(_ amount: Decimal) -> Decimal {
        Money.rounded(amount * savingsFactor)
    }
}
