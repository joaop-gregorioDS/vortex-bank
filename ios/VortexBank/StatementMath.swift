import Foundation

struct MonthKey: Hashable, Identifiable {
    var year: Int
    var month: Int
    var id: String { "\(year)-\(month)" }
}

struct DaySection: Identifiable, Equatable {
    var day: Date
    var balance: Decimal?
    var entries: [LedgerEntry]
    var id: Date { day }
}

enum StatementMath {
    static func withBalances(_ entries: [LedgerEntry], closingBalance: Decimal?) -> [LedgerEntry] {
        guard entries.allSatisfy({ $0.balanceAfter == nil }), let closingBalance else { return entries }
        var copy = entries
        let order = copy.indices.sorted { lhs, rhs in
            if copy[lhs].createdAt != copy[rhs].createdAt { return copy[lhs].createdAt > copy[rhs].createdAt }
            return copy[lhs].id > copy[rhs].id
        }
        var running = closingBalance
        for index in order {
            copy[index].balanceAfter = running
            running -= copy[index].signed
        }
        return copy
    }

    static func months(in entries: [LedgerEntry], calendar: Calendar = BankCalendar.calendar) -> [MonthKey] {
        var seen = Set<MonthKey>()
        for entry in entries {
            let parts = calendar.dateComponents([.year, .month], from: entry.createdAt)
            if let year = parts.year, let month = parts.month {
                seen.insert(MonthKey(year: year, month: month))
            }
        }
        return seen.sorted { lhs, rhs in
            if lhs.year != rhs.year { return lhs.year > rhs.year }
            return lhs.month > rhs.month
        }
    }

    static func sections(
        entries: [LedgerEntry],
        closingBalance: Decimal?,
        month: MonthKey?,
        text: String,
        calendar: Calendar = BankCalendar.calendar
    ) -> [DaySection] {
        let enriched = withBalances(entries, closingBalance: closingBalance)
        let query = text.trimmingCharacters(in: .whitespacesAndNewlines).bankFolded
        let inMonth = enriched.filter { entry in
            guard let month else { return true }
            let parts = calendar.dateComponents([.year, .month], from: entry.createdAt)
            return parts.year == month.year && parts.month == month.month
        }
        let grouped = Dictionary(grouping: inMonth) { calendar.startOfDay(for: $0.createdAt) }
        return grouped.compactMap { day, items -> DaySection? in
            let newest = items.max { lhs, rhs in
                if lhs.createdAt != rhs.createdAt { return lhs.createdAt < rhs.createdAt }
                return lhs.id < rhs.id
            }
            let visible = items.filter { entry in
                guard !query.isEmpty else { return true }
                return "\(entry.title) \(entry.detail) \(entry.rail)".bankFolded.contains(query)
            }.sorted { lhs, rhs in
                if lhs.createdAt != rhs.createdAt { return lhs.createdAt < rhs.createdAt }
                return lhs.id < rhs.id
            }
            guard !visible.isEmpty else { return nil }
            return DaySection(day: day, balance: newest?.balanceAfter, entries: visible)
        }.sorted { $0.day > $1.day }
    }

    static func totals(_ sections: [DaySection]) -> (credits: Decimal, debits: Decimal) {
        let entries = sections.flatMap(\.entries)
        let credits = entries.filter(\.credit).reduce(Decimal(0)) { $0 + $1.amount }
        let debits = entries.filter { !$0.credit }.reduce(Decimal(0)) { $0 + $1.amount }
        return (credits, debits)
    }
}
