package com.vortexsoftware.vortexbank.data

import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.sync.Mutex
import kotlinx.coroutines.sync.withLock
import kotlinx.coroutines.withContext
import kotlinx.serialization.json.Json
import kotlinx.serialization.json.JsonObject
import kotlinx.serialization.json.JsonPrimitive
import kotlinx.serialization.json.buildJsonObject
import kotlinx.serialization.json.contentOrNull
import kotlinx.serialization.json.jsonObject
import kotlinx.serialization.json.jsonPrimitive
import kotlinx.serialization.json.put
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.OkHttpClient
import okhttp3.Request
import okhttp3.RequestBody.Companion.toRequestBody
import okhttp3.Response
import java.io.IOException
import java.math.BigDecimal
import java.net.ConnectException
import java.net.SocketTimeoutException
import java.util.UUID

class BankClient(
    private val http: OkHttpClient,
    private val authBase: String = Endpoints.AUTH,
    private val transactionsBase: String = Endpoints.TRANSACTIONS,
) {
    private val gate = Mutex()
    private val json = Json { ignoreUnknownKeys = true }
    private var accessToken: String? = null
    private var refreshToken: String? = null
    private var profile: Profile? = null

    fun currentProfile(): Profile? = profile

    suspend fun login(email: String, password: String): Profile = io {
        val response = call(
            Request.Builder()
                .url("$authBase/login")
                .post(jsonBody(buildJsonObject {
                    put("email", email)
                    put("password", password)
                }))
                .build(),
        )
        val signedIn = store(response)
        try {
            provision(signedIn)
        } catch (error: Exception) {
            clear()
            throw error
        }
        signedIn
    }

    suspend fun logout() {
        val token = withContext(Dispatchers.IO) { gate.withLock { refreshToken } }
        if (token != null) {
            runCatching {
                withContext(Dispatchers.IO) {
                    call(
                        Request.Builder()
                            .url("$authBase/logout")
                            .post(ByteArray(0).toRequestBody(null))
                            .header("Cookie", cookie(token))
                            .build(),
                    ).close()
                }
            }
        }
        withContext(Dispatchers.IO) { gate.withLock { clear() } }
    }

    suspend fun ledgerUp(): Boolean = withContext(Dispatchers.IO) {
        try {
            call(Request.Builder().url("$transactionsBase/health").get().build()).use { it.isSuccessful }
        } catch (_: IOException) {
            false
        } catch (_: BankException) {
            false
        }
    }

    suspend fun home(): Home = io { decode(authed("GET", "$transactionsBase/home", null, null)) }

    suspend fun statement(accountId: String): List<Line> = io {
        decode(authed("GET", "$transactionsBase/statement?accountId=$accountId", null, null))
    }

    suspend fun receipt(journalId: String): Receipt = io {
        decode(authed("GET", "$transactionsBase/receipts/$journalId", null, null))
    }

    suspend fun sendPix(key: String, amount: BigDecimal): Receipt = io {
        val trimmed = key.trim()
        if (trimmed.isEmpty()) throw BankException("Informe a chave Pix.")
        decode(money("/pix", buildJsonObject {
            put("key", trimmed)
            put("amount", JsonPrimitive(cents(amount)))
        }))
    }

    suspend fun payBoleto(line: String): Receipt = io {
        val trimmed = line.trim()
        if (trimmed.isEmpty()) throw BankException("Linha digitável vazia.")
        decode(money("/boletos/pay", buildJsonObject { put("line", trimmed) }))
    }

    suspend fun payInvoice(invoiceId: String): Receipt = io {
        decode(money("/cards/invoices/$invoiceId/pay", buildJsonObject { }))
    }

    suspend fun purchase(cardId: String, merchant: String, amount: BigDecimal): Receipt = io {
        if (merchant !in merchants) throw BankException("Comerciante não aceito neste simulador.")
        decode(money("/cards/purchases", buildJsonObject {
            put("cardId", cardId)
            put("merchant", merchant)
            put("amount", JsonPrimitive(cents(amount)))
        }))
    }

    private suspend fun <T> io(block: () -> T): T = withContext(Dispatchers.IO) {
        gate.withLock { block() }
    }

    private fun provision(person: Profile) {
        val response = send(
            "POST",
            "$transactionsBase/me/provision",
            buildJsonObject {
                put("name", person.name)
                put("cpf", person.cpf)
            }.toString(),
            null,
            true,
        )
        if (response.code == 401) {
            response.close()
            refresh()
            read(send(
                "POST",
                "$transactionsBase/me/provision",
                buildJsonObject {
                    put("name", person.name)
                    put("cpf", person.cpf)
                }.toString(),
                null,
                true,
            ))
            return
        }
        read(response)
    }

    private fun money(path: String, body: JsonObject): String {
        if (!LedgerPaths.accepts(path)) throw BankException("Esta operação não grava no ledger.")
        val key = UUID.randomUUID().toString()
        return authed("POST", "$transactionsBase$path", body.toString(), key)
    }

    private fun authed(method: String, url: String, body: String?, idempotency: String?): String {
        val first = send(method, url, body, idempotency, true)
        if (first.code == 401) {
            first.close()
            refresh()
            return read(send(method, url, body, idempotency, true))
        }
        return read(first)
    }

    private fun refresh() {
        val token = refreshToken ?: run {
            clear()
            throw BankException("Sessão expirada. Entre de novo.")
        }
        val response = call(
            Request.Builder()
                .url("$authBase/refresh")
                .post(ByteArray(0).toRequestBody(null))
                .header("Cookie", cookie(token))
                .build(),
        )
        if (response.code !in 200..299) {
            response.close()
            clear()
            throw BankException("Sessão expirada. Entre de novo.")
        }
        try {
            store(response)
        } catch (error: Exception) {
            clear()
            throw error
        }
    }

    private fun store(response: Response): Profile {
        response.use { received ->
            val refresh = received.headers.values("Set-Cookie").firstNotNullOfOrNull(::parseRefreshCookie)
            val raw = received.body?.string().orEmpty()
            if (received.code !in 200..299) throw BankException(apiMessage(received.code, raw))
            val body = try {
                json.decodeFromString<AuthResponse>(raw)
            } catch (_: Exception) {
                throw BankException("Resposta de autenticação inválida.")
            }
            if (refresh.isNullOrBlank()) throw BankException("O login não devolveu o cookie de renovação.")
            accessToken = body.accessToken
            refreshToken = refresh
            val signedIn = Profile(body.userId, body.name, body.email, body.cpf)
            profile = signedIn
            return signedIn
        }
    }

    private fun send(method: String, url: String, body: String?, idempotency: String?, bearer: Boolean): Response {
        val builder = Request.Builder().url(url)
        if (bearer) {
            val token = accessToken ?: throw BankException("Entre de novo para continuar.")
            builder.header("Authorization", "Bearer $token")
        }
        if (idempotency != null) builder.header("Idempotency-Key", idempotency)
        when (method) {
            "GET" -> builder.get()
            else -> builder.method(method, if (body == null) ByteArray(0).toRequestBody(null) else jsonBody(body))
        }
        return call(builder.build())
    }

    private fun call(request: Request): Response = try {
        http.newCall(request).execute()
    } catch (error: IOException) {
        throw netError(error)
    }

    private fun read(response: Response): String = response.use { received ->
        val raw = received.body?.string().orEmpty()
        if (received.code !in 200..299) throw BankException(apiMessage(received.code, raw))
        raw
    }

    private inline fun <reified T> decode(raw: String): T {
        if (raw.isBlank()) throw BankException("A API devolveu uma resposta inválida.")
        return try {
            json.decodeFromString(raw)
        } catch (_: Exception) {
            throw BankException("A API devolveu uma resposta inválida.")
        }
    }

    private fun clear() {
        accessToken = null
        refreshToken = null
        profile = null
    }

    private fun cookie(token: String) = "bankcore_refresh=$token"

    private fun jsonBody(raw: String) = raw.toRequestBody(JSON)

    private fun jsonBody(obj: JsonObject) = obj.toString().toRequestBody(JSON)

    private fun apiMessage(status: Int, raw: String): String {
        val trimmed = raw.trim()
        if (trimmed.startsWith("{")) {
            val obj = runCatching { json.parseToJsonElement(trimmed).jsonObject }.getOrNull()
            val message = obj?.get("message")?.jsonPrimitive?.contentOrNull
            if (!message.isNullOrBlank()) return message
            val title = obj?.get("title")?.jsonPrimitive?.contentOrNull
            if (!title.isNullOrBlank()) return title
        }
        if (trimmed.isEmpty()) return "A API recusou a chamada ($status)."
        return trimmed
    }

    private fun netError(error: IOException): BankException {
        val down = error is ConnectException || error is SocketTimeoutException ||
            error.message.orEmpty().contains("failed to connect", ignoreCase = true) ||
            error.message.orEmpty().contains("timeout", ignoreCase = true)
        val offline = if (authBase.startsWith("https://") || authBase.contains("bank.vortexsoftware.tech")) {
            "Não foi possível conectar ao servidor Vortex Bank (https://bank.vortexsoftware.tech). Verifique sua conexão com a internet."
        } else if (authBase.contains("127.0.0.1")) {
            "A API local não respondeu. No Galaxy, conecte o cabo USB e encaminhe as portas 5081, 5082 e 8080. Na pasta vortex-bank\\docker, a API sobe com docker compose up -d."
        } else {
            "A API local não respondeu. Na pasta vortex-bank\\docker, suba com docker compose up -d."
        }
        return BankException(if (down) offline else "Não foi possível falar com a API.")
    }

    private companion object {
        val JSON = "application/json; charset=utf-8".toMediaType()
    }
}
