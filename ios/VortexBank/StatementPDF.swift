import UIKit

enum StatementPDF {
    static func render(
        holderName: String,
        cpf: String,
        accountNumber: String,
        accountTitle: String,
        sections: [DaySection],
        issuedAt: Date = Date()
    ) -> Data {
        let page = CGRect(x: 0, y: 0, width: 595, height: 842)
        let renderer = UIGraphicsPDFRenderer(bounds: page)
        let totals = StatementMath.totals(sections)
        return renderer.pdfData { context in
            let margin: CGFloat = 36
            let width = page.width - margin * 2
            var y: CGFloat = margin

            func ink(_ size: CGFloat, bold: Bool) -> UIFont {
                let name = bold ? "Manrope-Bold" : "Manrope-Regular"
                return UIFont(name: name, size: size) ?? UIFont.systemFont(ofSize: size, weight: bold ? .bold : .regular)
            }
            let text = UIColor(red: 28 / 255, green: 20 / 255, blue: 24 / 255, alpha: 1)
            let muted = UIColor(red: 109 / 255, green: 97 / 255, blue: 104 / 255, alpha: 1)
            let debit = UIColor(red: 225 / 255, green: 29 / 255, blue: 72 / 255, alpha: 1)
            let credit = UIColor(red: 21 / 255, green: 122 / 255, blue: 69 / 255, alpha: 1)

            func draw(_ string: String, x: CGFloat, y: CGFloat, width: CGFloat, font: UIFont, color: UIColor, align: NSTextAlignment = .left) -> CGFloat {
                let paragraph = NSMutableParagraphStyle()
                paragraph.alignment = align
                let attributes: [NSAttributedString.Key: Any] = [
                    .font: font,
                    .foregroundColor: color,
                    .paragraphStyle: paragraph
                ]
                let bounds = (string as NSString).boundingRect(
                    with: CGSize(width: width, height: 800),
                    options: [.usesLineFragmentOrigin],
                    attributes: attributes,
                    context: nil
                )
                (string as NSString).draw(
                    with: CGRect(x: x, y: y, width: width, height: ceil(bounds.height)),
                    options: [.usesLineFragmentOrigin],
                    attributes: attributes,
                    context: nil
                )
                return ceil(bounds.height)
            }

            func footer() {
                _ = draw(
                    VortexCopy.pdfFooter,
                    x: margin,
                    y: page.height - 28,
                    width: width,
                    font: ink(9, bold: false),
                    color: muted,
                    align: .left
                )
            }

            func newPage(repeatingHeader: Bool) {
                context.beginPage()
                footer()
                y = margin
                if repeatingHeader {
                    y += draw("Vortex Bank", x: margin, y: y, width: width, font: ink(16, bold: true), color: text)
                    y += 12
                }
            }

            context.beginPage()
            footer()
            y += draw("Vortex Bank", x: margin, y: y, width: width, font: ink(22, bold: true), color: text)
            y += 8
            let headerLines = [
                holderName,
                "CPF \(BankFormat.cpf(cpf))",
                "Agência \(VortexCopy.agency)",
                "Conta \(accountNumber.isEmpty ? "—" : accountNumber) · \(accountTitle)",
                "Emissão \(BankFormat.dateTime(issuedAt))"
            ]
            for line in headerLines {
                y += draw(line, x: margin, y: y, width: width, font: ink(11, bold: false), color: text) + 2
            }
            y += 10

            for section in sections {
                if y > 760 { newPage(repeatingHeader: true) }
                let balance = section.balance.map { "Saldo do dia \(BankFormat.currency($0))" } ?? "Saldo do dia indisponível"
                let dayY = y
                let dayHeight = draw(BankFormat.day(section.day), x: margin, y: dayY, width: width * 0.48, font: ink(12, bold: true), color: text)
                let balanceHeight = draw(balance, x: margin + width * 0.48, y: dayY, width: width * 0.52, font: ink(11, bold: true), color: text, align: .right)
                y = dayY + max(dayHeight, balanceHeight) + 6
                for entry in section.entries {
                    if y > 760 { newPage(repeatingHeader: true) }
                    let titleHeight = draw(entry.title, x: margin, y: y, width: width * 0.68, font: ink(11, bold: true), color: text)
                    _ = draw(
                        BankFormat.signedCurrency(amount: entry.amount, credit: entry.credit),
                        x: margin + width * 0.68,
                        y: y,
                        width: width * 0.32,
                        font: ink(11, bold: true),
                        color: entry.credit ? credit : debit,
                        align: .right
                    )
                    y += titleHeight + 1
                    y += draw(entry.subtitle, x: margin, y: y, width: width, font: ink(10, bold: false), color: muted) + 8
                }
                y += 8
            }

            if y > 740 { newPage(repeatingHeader: true) }
            y += 8
            y += draw("Entradas \(BankFormat.currency(totals.credits))", x: margin, y: y, width: width, font: ink(12, bold: true), color: credit) + 4
            _ = draw("Saídas \(BankFormat.currency(totals.debits))", x: margin, y: y, width: width, font: ink(12, bold: true), color: debit)
        }
    }
}
