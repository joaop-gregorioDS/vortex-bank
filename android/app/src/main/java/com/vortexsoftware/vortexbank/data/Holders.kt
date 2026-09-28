package com.vortexsoftware.vortexbank.data

data class Holder(
    val name: String,
    val email: String,
    val password: String,
    val cpf: String,
)

object Holders {
    val all: List<Holder> = listOf(
        Holder("Ana Ribeiro", "ana.ribeiro@vortexbank.demo", "Ana-demo-2026", "390.533.447-05"),
        Holder("Bruno Lima", "bruno.lima@vortexbank.demo", "Bruno-demo-2026", "529.982.247-25"),
    )

    fun byCpf(raw: String): Holder? {
        val digits = raw.filter(Char::isDigit)
        if (digits.isEmpty()) return null
        return all.find { it.cpf.filter(Char::isDigit) == digits }
    }
}

object LedgerPaths {
    fun accepts(path: String): Boolean {
        if (path == "/pix" || path == "/boletos/pay" || path == "/cards/purchases") return true
        return path.startsWith("/cards/invoices/") && path.endsWith("/pay") && path.count { it == '/' } == 4
    }
}

fun parseRefreshCookie(raw: String): String? {
    val pair = raw.split(';').firstOrNull()?.trim().orEmpty()
    val split = pair.indexOf('=')
    if (split <= 0) return null
    val name = pair.substring(0, split).trim()
    val token = pair.substring(split + 1).trim().trim('"')
    if (name == "bankcore_refresh" && token.isNotEmpty()) return token
    return null
}
