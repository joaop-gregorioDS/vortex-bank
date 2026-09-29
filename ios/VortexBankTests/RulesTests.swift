import UIKit
import XCTest
@testable import VortexBank

final class RulesTests: XCTestCase {
    func testDemoHoldersAreOnlyAnaAndBruno() {
        XCTAssertEqual(DemoDirectory.all.map(\.name), ["Ana Ribeiro", "Bruno Lima"])
        XCTAssertEqual(DemoDirectory.holder(cpf: "390.533.447-05")?.email, "ana.ribeiro@vortexbank.demo")
        XCTAssertEqual(DemoDirectory.holder(cpf: "52998224725")?.email, "bruno.lima@vortexbank.demo")
        XCTAssertNil(DemoDirectory.holder(cpf: "111.111.111-11"))
    }

    func testPixKeySendsCpfAsElevenDigits() {
        XCTAssertEqual(PixKeyNormalizer.forAPI("390.533.447-05"), "39053344705")
        XCTAssertEqual(PixKeyNormalizer.forAPI("  39053344705 "), "39053344705")
        XCTAssertEqual(PixKeyNormalizer.forAPI("ana.ribeiro@vortexbank.demo"), "ana.ribeiro@vortexbank.demo")
        XCTAssertEqual(PixKeyNormalizer.forAPI("+55 11 98888-7777"), "+55 11 98888-7777")
    }

    func testMoneyParsingAndJSON() {
        XCTAssertEqual(Money.parseUser("10,5"), Decimal(string: "10.50"))
        XCTAssertEqual(Money.parseUser("1.234,56"), Decimal(string: "1234.56"))
        XCTAssertEqual(Money.parseUser("10.50"), Decimal(string: "10.50"))
        XCTAssertNil(Money.parseUser("0"))
        XCTAssertNil(Money.parseUser("10,555"))
        XCTAssertEqual(Money.jsonNumber(Decimal(string: "10.5")!), "10.50")
    }

    func testInvestScenarioFactors() {
        let aporte = Decimal(string: "100")!
        XCTAssertEqual(InvestScenario.twelveMonths(aporte), Decimal(string: "111.44"))
        XCTAssertEqual(InvestScenario.oldSavings(aporte), Decimal(string: "106.17"))
        XCTAssertEqual(InvestScenario.patrimony, Decimal(string: "45890.20"))
        XCTAssertEqual(InvestScenario.liquidity, Decimal(string: "25000.00"))
        XCTAssertEqual(InvestScenario.cdiReference, "104% do CDI")
    }

    func testRefreshCookieIgnoresPath() throws {
        let refresh = try XCTUnwrap(HTTPCookie(properties: [
            .name: "bankcore_refresh",
            .value: "token-1",
            .domain: "127.0.0.1",
            .path: "/api/auth"
        ]))
        let other = try XCTUnwrap(HTTPCookie(properties: [
            .name: "session",
            .value: "no",
            .domain: "127.0.0.1",
            .path: "/"
        ]))
        XCTAssertEqual(RefreshCookie.header(from: [other, refresh]), "bankcore_refresh=token-1")
    }

    func testCloudEndpointsAndUrlBuilding() {
        XCTAssertEqual(BankAPI.baseURL.absoluteString, "https://bank.vortexsoftware.tech")
        XCTAssertEqual(BankAPI.auth.absoluteString, "https://bank.vortexsoftware.tech/api/auth")
        XCTAssertEqual(BankAPI.transactions.absoluteString, "https://bank.vortexsoftware.tech/api/transactions")
        XCTAssertEqual(BankAPI.swagger.absoluteString, "https://bank.vortexsoftware.tech/swagger")

        XCTAssertEqual(BankAPI.buildURL(base: BankAPI.auth, path: "/login").absoluteString, "https://bank.vortexsoftware.tech/api/auth/login")
        XCTAssertEqual(BankAPI.buildURL(base: BankAPI.auth, path: "/refresh").absoluteString, "https://bank.vortexsoftware.tech/api/auth/refresh")
        XCTAssertEqual(BankAPI.buildURL(base: BankAPI.auth, path: "/logout").absoluteString, "https://bank.vortexsoftware.tech/api/auth/logout")

        XCTAssertEqual(BankAPI.buildURL(base: BankAPI.transactions, path: "/home").absoluteString, "https://bank.vortexsoftware.tech/api/transactions/home")
        XCTAssertEqual(BankAPI.buildURL(base: BankAPI.transactions, path: "/me/provision").absoluteString, "https://bank.vortexsoftware.tech/api/transactions/me/provision")
        XCTAssertEqual(BankAPI.buildURL(base: BankAPI.transactions, path: "/statement").absoluteString, "https://bank.vortexsoftware.tech/api/transactions/statement")
        XCTAssertEqual(BankAPI.buildURL(base: BankAPI.transactions, path: "/pix").absoluteString, "https://bank.vortexsoftware.tech/api/transactions/pix")
        XCTAssertEqual(BankAPI.buildURL(base: BankAPI.transactions, path: "/boletos/pay").absoluteString, "https://bank.vortexsoftware.tech/api/transactions/boletos/pay")
        XCTAssertEqual(BankAPI.buildURL(base: BankAPI.transactions, path: "/cards/invoices/inv-123/pay").absoluteString, "https://bank.vortexsoftware.tech/api/transactions/cards/invoices/inv-123/pay")
        XCTAssertEqual(BankAPI.buildURL(base: BankAPI.transactions, path: "/cards/purchases").absoluteString, "https://bank.vortexsoftware.tech/api/transactions/cards/purchases")
        XCTAssertEqual(BankAPI.buildURL(base: BankAPI.transactions, path: "/health").absoluteString, "https://bank.vortexsoftware.tech/api/transactions/health")
    }

    func testCreatedAtUsesSaoPaulo() throws {
        let date = try XCTUnwrap(BankDateParser.parse("2026-09-27T13:47:13.941516Z"))
        let parts = BankCalendar.calendar.dateComponents([.year, .month, .day, .hour, .minute], from: date)
        XCTAssertEqual(parts.year, 2026)
        XCTAssertEqual(parts.month, 9)
        XCTAssertEqual(parts.day, 27)
        XCTAssertEqual(parts.hour, 10)
        XCTAssertEqual(parts.minute, 47)
        XCTAssertEqual(BankFormat.time(date), "10:47")
    }

    func testHomeIgnoresTedsAndSumsOpenBoletos() throws {
        let json = """
        {
          "accounts": [
            {"id": "acc-c", "kind": "corrente", "number": "12345-6", "balance": 250.5},
            {"id": "acc-p", "kind": "poupanca", "number": "999", "balance": 10}
          ],
          "cards": [
            {"id": "card-1", "kind": "credito", "last4": "4242", "limit": 5000, "available": 4200, "cvv": "123",
             "invoice": {"id": "inv-1", "amount": 180.4, "status": "aberta"}}
          ],
          "pixKeys": [{"id": "k1", "type": "cpf", "key": "39053344705"}],
          "boletos": [
            {"id": "b1", "line": "23790", "amount": 80, "status": "aberto", "mine": false, "title": "Energia"},
            {"id": "b2", "line": "23791", "amount": 40, "status": "aberto", "mine": true, "title": "Meu"},
            {"id": "b3", "line": "23792", "amount": 15, "status": "pago", "mine": false, "title": "Pago"}
          ],
          "teds": [
            {"journalId": "ted-1", "createdAt": "2026-09-27T13:47:13Z", "title": "TED interna", "amount": 9999, "direction": "debit"}
          ],
          "entries": [
            {"journalId": "j1", "createdAt": "2026-09-27T13:47:13.941516Z", "title": "Pix", "description": "Ana", "amount": 20, "direction": "debit"}
          ]
        }
        """.data(using: .utf8)!
        let home = try HomeSnapshot.parse(json)
        XCTAssertEqual(home.accounts.map(\.kind), [.corrente, .poupanca])
        XCTAssertEqual(home.cards.first?.invoiceId, "inv-1")
        XCTAssertEqual(home.pixKeys.first?.value, "39053344705")
        XCTAssertEqual(HomeMath.openBoletosTotal(home.boletos), 80)
        XCTAssertEqual(home.recent.map(\.id), ["j1"])
        XCTAssertFalse(home.recent[0].credit)
    }

    func testStatementDayOrderBalanceAndFilter() throws {
        let entries = [
            line("a", "2026-09-27T15:00:00Z", 100, true, nil, "manha"),
            line("b", "2026-09-27T18:00:00Z", 40, false, nil, "tarde"),
            line("c", "2026-09-28T12:00:00Z", 10, false, nil, "dia")
        ]
        let sections = StatementMath.sections(entries: entries, closingBalance: 50, month: nil, text: "")
        XCTAssertEqual(sections.count, 2)
        let newestDay = BankCalendar.calendar.dateComponents([.day], from: sections[0].day).day
        XCTAssertEqual(newestDay, 28)
        XCTAssertEqual(sections[0].balance, 50)
        XCTAssertEqual(sections[0].entries.map(\.id), ["c"])
        XCTAssertEqual(sections[1].balance, 60)
        XCTAssertEqual(sections[1].entries.map(\.id), ["a", "b"])

        let filtered = StatementMath.sections(entries: entries, closingBalance: 50, month: nil, text: "manha")
        XCTAssertEqual(filtered.count, 1)
        XCTAssertEqual(filtered[0].entries.map(\.id), ["a"])
        XCTAssertEqual(filtered[0].balance, 60)
    }

    func testStatementKeepsBalanceFromTheAPI() {
        let entries = [
            line("old", "2026-09-28T01:30:00Z", 10, true, 10, "antes"),
            line("new", "2026-09-28T04:00:00Z", 5, true, 15, "depois")
        ]
        let sections = StatementMath.sections(entries: entries, closingBalance: 999, month: nil, text: "")
        XCTAssertEqual(sections.count, 2)
        XCTAssertEqual(BankCalendar.calendar.component(.day, from: sections[0].day), 28)
        XCTAssertEqual(sections[0].balance, 15)
        XCTAssertEqual(BankCalendar.calendar.component(.day, from: sections[1].day), 27)
        XCTAssertEqual(sections[1].balance, 10)
    }

    func testMonthRecorte() {
        let entries = [
            line("a", "2026-09-27T15:00:00Z", 10, true, 10, "set"),
            line("b", "2026-08-10T15:00:00Z", 8, false, 2, "ago")
        ]
        let september = StatementMath.sections(
            entries: entries,
            closingBalance: nil,
            month: MonthKey(year: 2026, month: 9),
            text: ""
        )
        XCTAssertEqual(september.flatMap(\.entries).map(\.id), ["a"])
        XCTAssertEqual(StatementMath.months(in: entries).map(\.month), [9, 8])
    }

    func testManropeIsAvailableInTheApp() {
        XCTAssertNotNil(UIFont(name: "Manrope-Regular", size: 17))
    }

    private func line(_ id: String, _ iso: String, _ amount: Decimal, _ credit: Bool, _ balance: Decimal?, _ detail: String) -> LedgerEntry {
        LedgerEntry(
            id: id,
            createdAt: BankDateParser.parse(iso)!,
            title: "Pix",
            detail: detail,
            amount: amount,
            credit: credit,
            balanceAfter: balance,
            rail: "pix"
        )
    }
}
