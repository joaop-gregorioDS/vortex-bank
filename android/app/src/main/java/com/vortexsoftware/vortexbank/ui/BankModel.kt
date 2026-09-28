package com.vortexsoftware.vortexbank.ui

import android.content.SharedPreferences
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.vortexsoftware.vortexbank.data.BankClient
import com.vortexsoftware.vortexbank.data.Holders
import com.vortexsoftware.vortexbank.data.Home
import com.vortexsoftware.vortexbank.data.Line
import com.vortexsoftware.vortexbank.data.Profile
import com.vortexsoftware.vortexbank.data.Receipt
import com.vortexsoftware.vortexbank.data.cents
import com.vortexsoftware.vortexbank.data.parseAmount
import com.vortexsoftware.vortexbank.data.sessionEnded
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.Job
import kotlinx.coroutines.launch
import java.math.BigDecimal

enum class Tab { Home, Pix, Statement, Cards, More }

sealed interface Page {
    data object Home : Page
    data object Pix : Page
    data object Statement : Page
    data object Cards : Page
    data object More : Page
    data object Payments : Page
    data object Invest : Page
    data object Profile : Page
    data class Receipt(val journalId: String) : Page
}

class BankModel(
    private val api: BankClient,
    private val prefs: SharedPreferences,
) : ViewModel() {
    var profile by mutableStateOf<Profile?>(null)
        private set
    var home by mutableStateOf<Home?>(null)
        private set
    var ledger by mutableStateOf(false)
        private set
    var fontStep by mutableStateOf(prefs.getInt(FONT, 0).coerceIn(0, 2))
        private set
    var error by mutableStateOf("")
    var busy by mutableStateOf(false)
        private set
    var page by mutableStateOf<Page>(Page.Home)
        private set
    var tab by mutableStateOf(Tab.Home)
        private set
    var statement by mutableStateOf<List<Line>>(emptyList())
        private set
    var receipt by mutableStateOf<Receipt?>(null)
        private set

    private var statementJob: Job? = null
    private var receiptJob: Job? = null

    fun cycleFont() {
        fontStep = (fontStep + 1) % 3
        prefs.edit().putInt(FONT, fontStep).apply()
    }

    fun enterHolder(holder: com.vortexsoftware.vortexbank.data.Holder) {
        enter(holder.email, holder.password)
    }

    fun enterByCpf(cpf: String, password: String) {
        val person = Holders.byCpf(cpf)
        if (person == null) {
            error = "CPF não encontrado neste simulador."
            return
        }
        enter(person.email, password)
    }

    fun open(next: Tab) {
        tab = next
        page = when (next) {
            Tab.Home -> Page.Home
            Tab.Pix -> Page.Pix
            Tab.Statement -> Page.Statement
            Tab.Cards -> Page.Cards
            Tab.More -> Page.More
        }
        error = ""
    }

    fun openPayments() {
        tab = Tab.More
        page = Page.Payments
    }

    fun openInvest() {
        tab = Tab.More
        page = Page.Invest
    }

    fun openProfile() {
        tab = Tab.More
        page = Page.Profile
    }

    fun openReceipt(journalId: String) {
        page = Page.Receipt(journalId)
        receipt = null
    }

    fun back() {
        page = when (page) {
            Page.Payments, Page.Invest, Page.Profile -> Page.More
            is Page.Receipt -> pageFor(tab)
            else -> page
        }
    }

    fun refreshLedger() {
        viewModelScope.launch { ledger = api.ledgerUp() }
    }

    fun reload() {
        viewModelScope.launch {
            try {
                home = api.home()
                error = ""
            } catch (caught: Exception) {
                if (caught is CancellationException) throw caught
                surface(caught)
            }
        }
    }

    fun loadStatement(accountId: String) {
        statementJob?.cancel()
        statementJob = viewModelScope.launch {
            try {
                statement = api.statement(accountId)
            } catch (caught: Exception) {
                if (caught is CancellationException) throw caught
                surface(caught)
            }
        }
    }

    fun loadReceipt(journalId: String) {
        receiptJob?.cancel()
        receipt = null
        receiptJob = viewModelScope.launch {
            try {
                receipt = api.receipt(journalId)
            } catch (caught: Exception) {
                if (caught is CancellationException) throw caught
                receipt = null
                surface(caught)
            }
        }
    }

    fun sendPix(key: String, amount: String, done: (String, Boolean) -> Unit) {
        val digits = key.filter(Char::isDigit)
        val sent = if (digits.length == 11) digits else key.trim()
        val value = amountOr(amount, done) ?: return
        viewModelScope.launch {
            try {
                val proof = api.sendPix(sent, value)
                home = api.home()
                done("Pix enviado. ${proof.authentication}", true)
            } catch (caught: Exception) {
                if (caught is CancellationException) throw caught
                if (!fail(caught)) done(caught.message ?: "Não foi possível concluir.", false)
            }
        }
    }

    fun payBoleto(line: String, done: (String, Boolean) -> Unit) {
        viewModelScope.launch {
            try {
                api.payBoleto(line)
                home = api.home()
                done("Boleto pago. O valor saiu da conta corrente.", true)
            } catch (caught: Exception) {
                if (caught is CancellationException) throw caught
                if (!fail(caught)) done(caught.message ?: "Não pagou.", false)
            }
        }
    }

    fun payInvoice(invoiceId: String, done: (String, Boolean) -> Unit) {
        viewModelScope.launch {
            try {
                api.payInvoice(invoiceId)
                home = api.home()
                done("Fatura paga com a conta corrente.", true)
            } catch (caught: Exception) {
                if (caught is CancellationException) throw caught
                if (!fail(caught)) done(caught.message ?: "Não foi possível pagar a fatura.", false)
            }
        }
    }

    fun purchase(cardId: String, merchant: String, amount: String, done: (String, Boolean) -> Unit) {
        val value = amountOr(amount, done) ?: return
        viewModelScope.launch {
            try {
                api.purchase(cardId, merchant, value)
                home = api.home()
                done("Compra lançada no ledger.", true)
            } catch (caught: Exception) {
                if (caught is CancellationException) throw caught
                if (!fail(caught)) done(caught.message ?: "Compra recusada.", false)
            }
        }
    }

    fun leave() {
        viewModelScope.launch {
            api.logout()
            drop("" )
        }
    }

    private fun enter(email: String, password: String) {
        viewModelScope.launch {
            busy = true
            error = ""
            try {
                profile = api.login(email, password)
                home = api.home()
                page = Page.Home
                tab = Tab.Home
                error = ""
            } catch (caught: Exception) {
                if (caught is CancellationException) throw caught
                profile = null
                home = null
                error = caught.message ?: "Não foi possível concluir."
            } finally {
                busy = false
            }
        }
    }

    private fun amountOr(raw: String, done: (String, Boolean) -> Unit): BigDecimal? {
        val parsed = parseAmount(raw)
        if (parsed == null || parsed.signum() <= 0) {
            done("Informe um valor maior que zero.", false)
            return null
        }
        return cents(parsed)
    }

    private fun surface(caught: Exception) {
        if (!fail(caught)) error = caught.message ?: "Não foi possível concluir."
    }

    private fun fail(caught: Exception): Boolean {
        if (!sessionEnded(caught)) return false
        drop(caught.message ?: "Sessão expirada. Entre de novo.")
        return true
    }

    private fun drop(message: String) {
        profile = null
        home = null
        statement = emptyList()
        receipt = null
        page = Page.Home
        tab = Tab.Home
        error = message
    }

    private fun pageFor(tab: Tab): Page = when (tab) {
        Tab.Home -> Page.Home
        Tab.Pix -> Page.Pix
        Tab.Statement -> Page.Statement
        Tab.Cards -> Page.Cards
        Tab.More -> Page.More
    }

    private companion object {
        const val FONT = "font"
    }
}
