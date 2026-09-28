package com.vortexsoftware.vortexbank.data

import java.math.BigDecimal
import java.math.RoundingMode
import java.text.NumberFormat
import java.time.Instant
import java.time.LocalDate
import java.time.LocalDateTime
import java.time.ZoneId
import java.time.ZonedDateTime
import java.time.format.DateTimeFormatter
import java.util.Locale

private val brazil = Locale.forLanguageTag("pt-BR")
private val saoPaulo: ZoneId = ZoneId.of("America/Sao_Paulo")
private val clock = DateTimeFormatter.ofPattern("dd/MM/yyyy HH:mm:ss", brazil)
private val dayOnly = DateTimeFormatter.ofPattern("dd/MM/yyyy", brazil)
private val dayHeading = DateTimeFormatter.ofPattern("d 'de' MMMM, EEEE", brazil)
private val moneyFormat = NumberFormat.getCurrencyInstance(brazil)

val monthShort = listOf("JAN", "FEV", "MAR", "ABR", "MAI", "JUN", "JUL", "AGO", "SET", "OUT", "NOV", "DEZ")

val merchants = listOf("Mercado", "Combustível", "Farmácia", "Streaming")

fun money(value: Double): String = moneyFormat.format(value)

fun parseAmount(raw: String): BigDecimal? {
    var text = raw.trim().replace("R$", "", ignoreCase = true).replace(" ", "")
    if (text.isEmpty()) return null
    text = when {
        ',' in text && '.' in text && text.lastIndexOf(',') > text.lastIndexOf('.') ->
            text.replace(".", "").replace(',', '.')
        ',' in text && '.' in text -> text.replace(",", "")
        ',' in text -> text.replace(',', '.')
        else -> text
    }
    return text.toBigDecimalOrNull()
}

fun cents(amount: BigDecimal): BigDecimal = amount.setScale(2, RoundingMode.HALF_UP)

fun maskCpf(value: String): String {
    val digits = value.filter(Char::isDigit)
    if (digits.length != 11) return value
    return "${digits.substring(0, 3)}.${digits.substring(3, 6)}.${digits.substring(6, 9)}-${digits.substring(9)}"
}

fun initials(name: String): String =
    name.split(' ').filter { it.isNotBlank() }.take(2).joinToString("") { it.first().uppercase() }

fun firstName(name: String): String = name.substringBefore(' ').ifBlank { name }

fun accountLabel(kind: String): String = if (kind == "corrente") "Conta corrente" else "Poupança"

fun kindLabel(kind: String): String = when (kind) {
    "Seed" -> "Saldo inicial"
    "Pix" -> "Pix"
    "Ted" -> "TED"
    "TedFee" -> "Tarifa TED"
    "TedFeeReversal" -> "Estorno de tarifa"
    "Boleto" -> "Pagamento de boleto"
    "SavingsTransfer" -> "Transferência"
    "SavingsYield" -> "Rendimento"
    "DebitPurchase" -> "Compra no débito"
    "CreditPurchase" -> "Compra no crédito"
    "InvoicePayment" -> "Pagamento de fatura"
    else -> kind
}

fun day(iso: String): String = runCatching { LocalDate.parse(iso).format(dayOnly) }.getOrDefault(iso)

fun dayTitle(iso: String): String =
    runCatching { LocalDate.parse(iso).format(dayHeading) }.getOrDefault(iso)

fun whenLabel(line: Line): String {
    val created = line.createdAt
    if (created.isNullOrBlank()) return day(line.businessDate)
    val instant = parseInstant(created) ?: return day(line.businessDate)
    return instant.atZone(saoPaulo).format(clock)
}

fun issuedNow(): String = ZonedDateTime.now(saoPaulo).format(clock)

fun parseInstant(value: String): Instant? {
    runCatching { return Instant.parse(value) }
    runCatching { return LocalDateTime.parse(value).atZone(ZoneId.of("UTC")).toInstant() }
    return null
}

fun checkingOf(home: Home?): Account? = home?.accounts?.firstOrNull { it.kind == "corrente" }

fun savingsOf(home: Home?): Account? = home?.accounts?.firstOrNull { it.kind == "poupanca" }

fun openBills(home: Home?): List<Boleto> =
    home?.boletos?.filter { it.status == "aberto" && !it.mine }.orEmpty()

data class DayGroup(val date: String, val balance: Double, val lines: List<Line>)

fun groupStatement(lines: List<Line>, balance: Double): List<DayGroup> {
    val ordered = lines.sortedWith(compareByDescending<Line> { it.createdAt ?: it.businessDate })
    val groups = mutableListOf<Triple<String, Double, MutableList<Line>>>()
    var running = balance
    for (line in ordered) {
        val last = groups.lastOrNull()
        if (last == null || last.first != line.businessDate) {
            groups += Triple(line.businessDate, running, mutableListOf())
        }
        groups.last().third.add(0, line)
        running += if (line.direction == "credito") -line.amount else line.amount
    }
    return groups.map { DayGroup(it.first, it.second, it.third.toList()) }
}

data class StatementDocument(
    val holder: String,
    val cpf: String,
    val account: String,
    val number: String,
    val issuedAt: String,
    val credits: String,
    val debits: String,
    val groups: List<DayGroup>,
)

fun visibleStatement(lines: List<Line>, balance: Double, month: String, query: String): Pair<List<DayGroup>, Pair<Double, Double>> {
    val ordered = lines.sortedWith(compareByDescending<Line> { it.createdAt ?: it.businessDate })
    val groups = groupStatement(lines, balance)
    val months = ordered.map { it.businessDate.take(7) }.distinct().sorted()
    val selected = if (month.isNotBlank() && month in months) month else months.lastOrNull().orEmpty()
    val needle = query.trim()
    val filtered = ordered.filter { line ->
        line.businessDate.startsWith(selected) &&
            "${line.description} ${kindLabel(line.kind)}".contains(needle, ignoreCase = true)
    }.toSet()
    val visible = groups
        .filter { it.date.startsWith(selected) }
        .map { group -> group.copy(lines = group.lines.filter { it in filtered }) }
        .filter { it.lines.isNotEmpty() }
    val credits = filtered.filter { it.direction == "credito" }.sumOf { it.amount }
    val debits = filtered.filter { it.direction == "debito" }.sumOf { it.amount }
    return visible to (credits to debits)
}

fun statementMonths(lines: List<Line>): List<String> =
    lines.map { it.businessDate.take(7) }.distinct().sorted()

val knownRecipients = mapOf(
    "39053344705" to "Ana Ribeiro",
    "52998224725" to "Bruno Lima",
    "11144477735" to "Carla Mendes",
)
