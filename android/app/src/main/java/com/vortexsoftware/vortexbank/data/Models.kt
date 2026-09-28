package com.vortexsoftware.vortexbank.data

import kotlinx.serialization.Serializable

@Serializable
data class Profile(
    val userId: String,
    val name: String,
    val email: String,
    val cpf: String,
)

@Serializable
data class AuthResponse(
    val userId: String,
    val name: String,
    val email: String,
    val cpf: String,
    val accessToken: String,
    val accessExpiresUtc: String = "",
)

@Serializable
data class Account(
    val id: String,
    val kind: String,
    val agency: String,
    val number: String,
    val balance: Double,
)

@Serializable
data class Card(
    val id: String,
    val kind: String,
    val pan: String,
    val holder: String,
    val expiry: String,
    val cvv: String,
    val limit: Double? = null,
    val used: Double? = null,
    val openInvoiceId: String? = null,
    val openInvoiceAmount: Double? = null,
    val openInvoiceDue: String? = null,
)

@Serializable
data class PixKey(
    val id: String,
    val kind: String,
    val value: String,
)

@Serializable
data class Boleto(
    val id: String,
    val line: String,
    val beneficiary: String,
    val amount: Double,
    val dueDate: String,
    val status: String,
    val external: Boolean = false,
    val mine: Boolean = false,
)

@Serializable
data class Line(
    val journalId: String,
    val businessDate: String,
    val createdAt: String? = null,
    val kind: String,
    val direction: String,
    val amount: Double,
    val description: String,
)

@Serializable
data class Receipt(
    val journalId: String,
    val businessDate: String,
    val kind: String,
    val amount: Double,
    val description: String,
    val authentication: String,
)

@Serializable
data class Home(
    val businessDate: String,
    val accounts: List<Account> = emptyList(),
    val cards: List<Card> = emptyList(),
    val pixKeys: List<PixKey> = emptyList(),
    val boletos: List<Boleto> = emptyList(),
    val recent: List<Line> = emptyList(),
)

class BankException(message: String) : Exception(message)

fun sessionEnded(error: Throwable): Boolean {
    val text = error.message ?: return false
    return text.contains("Entre de novo") || text.contains("Sessão expirada")
}
