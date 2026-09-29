import Foundation

extension String {
    var bankFolded: String {
        folding(options: .diacriticInsensitive, locale: Locale(identifier: "pt_BR")).lowercased()
    }
}

enum BankCalendar {
    static let timeZone = TimeZone(identifier: "America/Sao_Paulo") ?? .gmt

    static var calendar: Calendar {
        var calendar = Calendar(identifier: .gregorian)
        calendar.timeZone = timeZone
        calendar.locale = Locale(identifier: "pt_BR")
        return calendar
    }
}

enum BankDateParser {
    static func parse(_ string: String) -> Date? {
        let trimmed = string.trimmingCharacters(in: .whitespacesAndNewlines)
        guard !trimmed.isEmpty else { return nil }
        let fractional = ISO8601DateFormatter()
        fractional.formatOptions = [.withInternetDateTime, .withFractionalSeconds]
        if let date = fractional.date(from: trimmed) { return date }
        let plain = ISO8601DateFormatter()
        plain.formatOptions = [.withInternetDateTime]
        if let date = plain.date(from: trimmed) { return date }
        return parseManual(trimmed)
    }

    private static func parseManual(_ string: String) -> Date? {
        let separator = string.firstIndex(of: "T") ?? string.firstIndex(of: " ")
        guard let separator else { return nil }
        let datePart = string[..<separator]
        var timePart = String(string[string.index(after: separator)...])
        var secondsFromGMT = 0
        if timePart.hasSuffix("Z") {
            timePart.removeLast()
        } else if let match = timePart.range(of: #"[+-]\d{2}:?\d{2}$"#, options: .regularExpression) {
            let token = String(timePart[match]).replacingOccurrences(of: ":", with: "")
            timePart.removeSubrange(match)
            let sign = token.hasPrefix("-") ? -1 : 1
            let digits = token.dropFirst()
            let hours = Int(digits.prefix(2)) ?? 0
            let minutes = Int(digits.suffix(2)) ?? 0
            secondsFromGMT = sign * ((hours * 60 + minutes) * 60)
        }
        let dateBits = datePart.split(separator: "-")
        guard dateBits.count == 3,
              let year = Int(dateBits[0]),
              let month = Int(dateBits[1]),
              let day = Int(dateBits[2]) else { return nil }
        let pieces = timePart.split(separator: ".", maxSplits: 1, omittingEmptySubsequences: false)
        let clock = pieces[0].split(separator: ":")
        guard clock.count >= 2, let hour = Int(clock[0]), let minute = Int(clock[1]) else { return nil }
        let second = clock.count > 2 ? Int(clock[2]) ?? 0 : 0
        var nanosecond = 0
        if pieces.count == 2 {
            var fraction = String(pieces[1].prefix(9))
            while fraction.count < 9 { fraction.append("0") }
            nanosecond = Int(fraction) ?? 0
        }
        var components = DateComponents()
        components.calendar = Calendar(identifier: .gregorian)
        components.timeZone = TimeZone(secondsFromGMT: secondsFromGMT)
        components.year = year
        components.month = month
        components.day = day
        components.hour = hour
        components.minute = minute
        components.second = second
        components.nanosecond = nanosecond
        return components.date
    }
}

enum BankFormat {
    static func currency(_ value: Decimal) -> String {
        let formatter = NumberFormatter()
        formatter.locale = Locale(identifier: "pt_BR")
        formatter.numberStyle = .currency
        formatter.currencyCode = "BRL"
        formatter.minimumFractionDigits = 2
        formatter.maximumFractionDigits = 2
        return formatter.string(from: NSDecimalNumber(decimal: value)) ?? "R$ 0,00"
    }

    static func signedCurrency(amount: Decimal, credit: Bool) -> String {
        let text = currency(amount)
        return credit ? text : "−\(text)"
    }

    static func day(_ date: Date) -> String {
        let formatter = DateFormatter()
        formatter.locale = Locale(identifier: "pt_BR")
        formatter.timeZone = BankCalendar.timeZone
        formatter.setLocalizedDateFormatFromTemplate("d MMM yyyy")
        return formatter.string(from: date)
    }

    static func time(_ date: Date) -> String {
        let formatter = DateFormatter()
        formatter.locale = Locale(identifier: "pt_BR")
        formatter.timeZone = BankCalendar.timeZone
        formatter.dateFormat = "HH:mm"
        return formatter.string(from: date)
    }

    static func dateTime(_ date: Date) -> String {
        "\(day(date)) \(time(date))"
    }

    static func month(_ key: MonthKey) -> String {
        var components = DateComponents()
        components.year = key.year
        components.month = key.month
        components.day = 1
        components.timeZone = BankCalendar.timeZone
        let date = BankCalendar.calendar.date(from: components) ?? Date()
        let formatter = DateFormatter()
        formatter.locale = Locale(identifier: "pt_BR")
        formatter.timeZone = BankCalendar.timeZone
        formatter.setLocalizedDateFormatFromTemplate("MMMM yyyy")
        return formatter.string(from: date).capitalized
    }

    static func cpf(_ raw: String) -> String {
        let digits = raw.filter(\.isNumber)
        guard digits.count == 11 else { return raw.isEmpty ? "—" : raw }
        let chars = Array(digits)
        return "\(chars[0])\(chars[1])\(chars[2]).\(chars[3])\(chars[4])\(chars[5]).\(chars[6])\(chars[7])\(chars[8])-\(chars[9])\(chars[10])"
    }

    static func maskCPF(_ raw: String) -> String {
        let digits = raw.filter(\.isNumber).prefix(11)
        var result = ""
        for (index, character) in digits.enumerated() {
            if index == 3 || index == 6 { result.append(".") }
            if index == 9 { result.append("-") }
            result.append(character)
        }
        return result
    }

    static func greeting(at date: Date = Date()) -> String {
        let hour = BankCalendar.calendar.component(.hour, from: date)
        switch hour {
        case ..<12: return "Bom dia"
        case 12..<18: return "Boa tarde"
        default: return "Boa noite"
        }
    }

    static func firstName(_ name: String) -> String {
        name.split(separator: " ").first.map(String.init) ?? name
    }
}

enum Money {
    static func rounded(_ value: Decimal) -> Decimal {
        var input = value
        var output = Decimal()
        NSDecimalRound(&output, &input, 2, .bankers)
        return output
    }

    static func jsonNumber(_ value: Decimal) -> String {
        let formatter = NumberFormatter()
        formatter.locale = Locale(identifier: "en_US_POSIX")
        formatter.numberStyle = .decimal
        formatter.usesGroupingSeparator = false
        formatter.minimumFractionDigits = 2
        formatter.maximumFractionDigits = 2
        formatter.decimalSeparator = "."
        return formatter.string(from: NSDecimalNumber(decimal: rounded(value))) ?? "0.00"
    }

    static func parseUser(_ text: String) -> Decimal? {
        var source = text.trimmingCharacters(in: .whitespacesAndNewlines)
        guard !source.isEmpty else { return nil }
        if source.contains(",") && source.contains(".") {
            source = source.replacingOccurrences(of: ".", with: "").replacingOccurrences(of: ",", with: ".")
        } else if source.contains(",") {
            source = source.replacingOccurrences(of: ",", with: ".")
        }
        guard let value = Decimal(string: source, locale: Locale(identifier: "en_US_POSIX")) else { return nil }
        let fraction = source.split(separator: ".").dropFirst().first
        if (fraction?.count ?? 0) > 2 { return nil }
        guard value > 0 else { return nil }
        return rounded(value)
    }
}

enum PixKeyNormalizer {
    static func forAPI(_ raw: String) -> String {
        let trimmed = raw.trimmingCharacters(in: .whitespacesAndNewlines)
        if trimmed.contains("@") { return trimmed }
        let digits = trimmed.filter(\.isNumber)
        let numericMask = trimmed.allSatisfy { $0.isNumber || $0 == "." || $0 == "-" || $0 == "/" || $0 == " " }
        if numericMask && digits.count == 11 { return digits }
        return trimmed
    }
}

enum JSONText {
    static func string(_ value: String) -> String {
        guard let data = try? JSONSerialization.data(withJSONObject: [value]),
              let text = String(data: data, encoding: .utf8),
              text.count >= 2 else { return "\"\"" }
        return String(text.dropFirst().dropLast())
    }

    static func object(_ pairs: [(String, String)]) -> Data {
        let body = pairs.map { "\(string($0.0)):\($0.1)" }.joined(separator: ",")
        return Data("{\(body)}".utf8)
    }
}
