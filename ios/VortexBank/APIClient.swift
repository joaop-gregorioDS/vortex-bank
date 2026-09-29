import Foundation

enum BankAPI {
    static let baseURL = URL(string: "https://bank.vortexsoftware.tech")!
    static let auth = URL(string: "https://bank.vortexsoftware.tech/api/auth")!
    static let transactions = URL(string: "https://bank.vortexsoftware.tech/api/transactions")!
    static let swagger = URL(string: "https://bank.vortexsoftware.tech/swagger")!

    static func buildURL(base: URL, path: String) -> URL {
        var components = URLComponents(url: base, resolvingAgainstBaseURL: false)
        let basePath = (components?.path ?? "").trimmingCharacters(in: CharacterSet(charactersIn: "/"))
        let subPath = path.trimmingCharacters(in: CharacterSet(charactersIn: "/"))
        if basePath.isEmpty {
            components?.path = "/" + subPath
        } else if subPath.isEmpty {
            components?.path = "/" + basePath
        } else {
            components?.path = "/" + basePath + "/" + subPath
        }
        return components?.url ?? base.appendingPathComponent(path)
    }
}

enum RefreshCookie {
    static let name = "bankcore_refresh"

    static func header(from cookies: [HTTPCookie]) -> String? {
        guard let cookie = cookies.first(where: { $0.name == name }), !cookie.value.isEmpty else { return nil }
        return "\(name)=\(cookie.value)"
    }
}

actor APIClient {
    private let session: URLSession
    private var accessToken: String?
    private var refreshValue: String?
    private var refreshing = false
    private var refreshWaiters: [CheckedContinuation<Void, Error>] = []

    init() {
        let configuration = URLSessionConfiguration.default
        configuration.httpCookieStorage = HTTPCookieStorage.shared
        configuration.httpCookieAcceptPolicy = .always
        configuration.httpShouldSetCookies = true
        configuration.timeoutIntervalForRequest = 20
        session = URLSession(configuration: configuration)
    }

    func login(email: String, password: String) async throws -> UserProfile {
        let body = JSONText.object([
            ("email", JSONText.string(email)),
            ("password", JSONText.string(password))
        ])
        let data: Data
        do {
            data = try await execute(
                base: BankAPI.auth,
                path: "/login",
                method: "POST",
                body: body,
                authorize: false,
                sendRefreshCookie: false
            )
        } catch BankError.unauthorized {
            throw BankError.message("CPF ou senha não conferem.")
        }
        let object = try JSONValue.parse(data)
        guard let token = object.string("accessToken", "access_token", "token"), !token.isEmpty else {
            throw BankError.message("A resposta de login não trouxe o token de acesso.")
        }
        accessToken = token
        guard refreshCookieHeader() != nil else {
            throw BankError.message("O login não devolveu o cookie bankcore_refresh.")
        }
        return UserProfile(
            userId: object.string("userId", "user_id", "id") ?? "",
            name: object.string("name", "nome") ?? "",
            email: object.string("email") ?? email,
            cpf: object.string("cpf") ?? "",
            accessExpiresUtc: object.date("accessExpiresUtc", "accessExpires", "expiresAt")
        )
    }

    func provision(name: String, cpf: String) async throws {
        let body = JSONText.object([
            ("name", JSONText.string(name)),
            ("cpf", JSONText.string(cpf))
        ])
        _ = try await send(
            base: BankAPI.transactions,
            path: "/me/provision",
            method: "POST",
            body: body,
            idempotencyKey: nil
        )
    }

    func home() async throws -> HomeSnapshot {
        let data = try await send(base: BankAPI.transactions, path: "/home", method: "GET", body: nil, idempotencyKey: nil)
        return try HomeSnapshot.parse(data)
    }

    func statement(accountId: String) async throws -> [LedgerEntry] {
        let data = try await send(
            base: BankAPI.transactions,
            path: "/statement",
            method: "GET",
            query: [URLQueryItem(name: "accountId", value: accountId)],
            body: nil,
            idempotencyKey: nil
        )
        return try HomeSnapshot.statement(from: data)
    }

    func receipt(journalId: String) async throws -> Receipt {
        let data = try await send(
            base: BankAPI.transactions,
            path: "/receipts/\(pathComponent(journalId))",
            method: "GET",
            body: nil,
            idempotencyKey: nil
        )
        return try ReceiptParser.parse(data)
    }

    func pix(key: String, amount: Decimal, idempotencyKey: String) async throws {
        let body = JSONText.object([
            ("key", JSONText.string(PixKeyNormalizer.forAPI(key))),
            ("amount", Money.jsonNumber(amount))
        ])
        _ = try await send(
            base: BankAPI.transactions,
            path: "/pix",
            method: "POST",
            body: body,
            idempotencyKey: idempotencyKey
        )
    }

    func payBoleto(line: String, idempotencyKey: String) async throws {
        let body = JSONText.object([
            ("line", JSONText.string(line))
        ])
        _ = try await send(
            base: BankAPI.transactions,
            path: "/boletos/pay",
            method: "POST",
            body: body,
            idempotencyKey: idempotencyKey
        )
    }

    func payInvoice(invoiceId: String, idempotencyKey: String) async throws {
        _ = try await send(
            base: BankAPI.transactions,
            path: "/cards/invoices/\(pathComponent(invoiceId))/pay",
            method: "POST",
            body: Data("{}".utf8),
            idempotencyKey: idempotencyKey
        )
    }

    func purchase(cardId: String, merchant: String, amount: Decimal, idempotencyKey: String) async throws {
        guard CardMerchant.allCases.contains(where: { $0.rawValue == merchant }) else {
            throw BankError.message("Estabelecimento não aceito.")
        }
        let body = JSONText.object([
            ("cardId", JSONText.string(cardId)),
            ("merchant", JSONText.string(merchant)),
            ("amount", Money.jsonNumber(amount))
        ])
        _ = try await send(
            base: BankAPI.transactions,
            path: "/cards/purchases",
            method: "POST",
            body: body,
            idempotencyKey: idempotencyKey
        )
    }

    func ledgerReachable() async -> Bool {
        do {
            let data = try await execute(
                base: BankAPI.transactions,
                path: "/health",
                method: "GET",
                body: nil,
                authorize: false,
                sendRefreshCookie: false
            )
            return (try? JSONValue.parse(data).string("status"))?.bankFolded == "ok"
        } catch {
            return false
        }
    }

    func logout() async {
        let body = Data("{}".utf8)
        _ = try? await execute(
            base: BankAPI.auth,
            path: "/logout",
            method: "POST",
            body: body,
            authorize: false,
            sendRefreshCookie: true
        )
        clearSession()
    }

    func clearSession() {
        accessToken = nil
        refreshValue = nil
        HTTPCookieStorage.shared.cookies?
            .filter { $0.name == RefreshCookie.name }
            .forEach { HTTPCookieStorage.shared.deleteCookie($0) }
    }

    private func send(
        base: URL,
        path: String,
        method: String,
        query: [URLQueryItem] = [],
        body: Data?,
        idempotencyKey: String?
    ) async throws -> Data {
        do {
            return try await execute(
                base: base,
                path: path,
                method: method,
                query: query,
                body: body,
                authorize: true,
                sendRefreshCookie: false,
                idempotencyKey: idempotencyKey
            )
        } catch BankError.unauthorized {
            try await refreshOnce()
            return try await execute(
                base: base,
                path: path,
                method: method,
                query: query,
                body: body,
                authorize: true,
                sendRefreshCookie: false,
                idempotencyKey: idempotencyKey
            )
        }
    }

    private func refreshOnce() async throws {
        if refreshing {
            try await withCheckedThrowingContinuation { continuation in
                refreshWaiters.append(continuation)
            }
            return
        }
        refreshing = true
        do {
            try await performRefresh()
            refreshing = false
            let waiters = refreshWaiters
            refreshWaiters.removeAll()
            waiters.forEach { $0.resume() }
        } catch {
            refreshing = false
            let waiters = refreshWaiters
            refreshWaiters.removeAll()
            waiters.forEach { $0.resume(throwing: error) }
            throw error
        }
    }

    private func performRefresh() async throws {
        let data = try await execute(
            base: BankAPI.auth,
            path: "/refresh",
            method: "POST",
            body: Data("{}".utf8),
            authorize: false,
            sendRefreshCookie: true
        )
        let object = try JSONValue.parse(data)
        guard let token = object.string("accessToken", "access_token", "token"), !token.isEmpty else {
            throw BankError.message("Não foi possível renovar a sessão.")
        }
        accessToken = token
    }

    /// The refresh cookie is issued with path `/api/auth`, so URLSession will not attach it to `/refresh` or `/logout`.
    /// Read it by name and send the `Cookie` header ourselves. The same key is reused when a money call is retried.
    private func execute(
        base: URL,
        path: String,
        method: String,
        query: [URLQueryItem] = [],
        body: Data?,
        authorize: Bool,
        sendRefreshCookie: Bool,
        idempotencyKey: String? = nil
    ) async throws -> Data {
        let urlWithBase = BankAPI.buildURL(base: base, path: path)
        var components = URLComponents(url: urlWithBase, resolvingAgainstBaseURL: false)
        if !query.isEmpty { components?.queryItems = query }
        guard let url = components?.url else { throw BankError.message("Endereço inválido.") }
        var request = URLRequest(url: url)
        request.httpMethod = method
        request.setValue("application/json", forHTTPHeaderField: "Accept")
        if let body {
            request.httpBody = body
            request.setValue("application/json", forHTTPHeaderField: "Content-Type")
        }
        if authorize {
            guard let accessToken else { throw BankError.unauthorized }
            request.setValue("Bearer \(accessToken)", forHTTPHeaderField: "Authorization")
        }
        if sendRefreshCookie {
            guard let header = refreshCookieHeader() else {
                throw BankError.message("Cookie bankcore_refresh ausente. Entre de novo.")
            }
            request.setValue(header, forHTTPHeaderField: "Cookie")
        }
        if let idempotencyKey {
            request.setValue(idempotencyKey, forHTTPHeaderField: "Idempotency-Key")
        }
        let data: Data
        let response: URLResponse
        do {
            (data, response) = try await session.data(for: request)
        } catch is CancellationError {
            throw CancellationError()
        } catch let error as URLError where error.code == .cancelled {
            throw error
        } catch {
            throw BankError.message("Não foi possível conectar à API (\(url.host ?? "servidor")). Verifique sua conexão e tente novamente.")
        }
        guard let http = response as? HTTPURLResponse else {
            throw BankError.message("Resposta inválida da API.")
        }
        captureRefreshCookie(from: http, url: url)
        if http.statusCode == 401 { throw BankError.unauthorized }
        guard (200..<300).contains(http.statusCode) else {
            let message = (try? JSONValue.parse(data).string("detail", "message", "error", "title", "mensagem"))
                ?? "A API respondeu \(http.statusCode)."
            throw BankError.message(message)
        }
        return data
    }

    private func refreshCookieHeader() -> String? {
        if let refreshValue, !refreshValue.isEmpty {
            return "\(RefreshCookie.name)=\(refreshValue)"
        }
        return RefreshCookie.header(from: HTTPCookieStorage.shared.cookies ?? [])
    }

    private func captureRefreshCookie(from response: HTTPURLResponse, url: URL) {
        var headers: [String: String] = [:]
        for (key, value) in response.allHeaderFields {
            guard let name = key as? String, let text = value as? String else { continue }
            headers[name] = text
            if name.caseInsensitiveCompare("Set-Cookie") == .orderedSame {
                extractRefresh(from: text, url: url)
            }
        }
        let cookies = HTTPCookie.cookies(withResponseHeaderFields: headers, for: url)
        if let found = cookies.first(where: { $0.name == RefreshCookie.name }) {
            rememberRefresh(found.value, url: url)
        }
    }

    private func extractRefresh(from header: String, url: URL) {
        guard let range = header.range(of: "\(RefreshCookie.name)=") else { return }
        let rest = header[range.upperBound...]
        let value = rest.split(separator: ";", maxSplits: 1).first.map(String.init)?
            .trimmingCharacters(in: .whitespacesAndNewlines) ?? ""
        guard !value.isEmpty else { return }
        rememberRefresh(value, url: url)
    }

    private func rememberRefresh(_ value: String, url: URL) {
        refreshValue = value
        let host = url.host ?? BankAPI.baseURL.host ?? "bank.vortexsoftware.tech"
        guard let cookie = HTTPCookie(properties: [
            .name: RefreshCookie.name,
            .value: value,
            .domain: host,
            .path: "/"
        ]) else { return }
        HTTPCookieStorage.shared.setCookie(cookie)
    }

    private func pathComponent(_ value: String) -> String {
        value.addingPercentEncoding(withAllowedCharacters: .urlPathAllowed) ?? value
    }
}
