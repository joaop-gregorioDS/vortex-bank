package com.vortexsoftware.vortexbank

import com.vortexsoftware.vortexbank.data.BankClient
import com.vortexsoftware.vortexbank.data.Holders
import com.vortexsoftware.vortexbank.data.LedgerPaths
import com.vortexsoftware.vortexbank.data.Line
import com.vortexsoftware.vortexbank.data.cents
import com.vortexsoftware.vortexbank.data.groupStatement
import com.vortexsoftware.vortexbank.data.parseAmount
import com.vortexsoftware.vortexbank.data.parseRefreshCookie
import kotlinx.coroutines.runBlocking
import okhttp3.OkHttpClient
import okhttp3.mockwebserver.Dispatcher
import okhttp3.mockwebserver.MockResponse
import okhttp3.mockwebserver.MockWebServer
import okhttp3.mockwebserver.RecordedRequest
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Before
import org.junit.Test
import java.math.BigDecimal

class BankRulesTest {
    private val server = MockWebServer()

    @Before fun start() { server.start() }

    @After fun stop() { server.shutdown() }

    @Test
    fun refreshCookieIgnoresPath() {
        val raw = "bankcore_refresh=ABC123; expires=Sun, 11 Oct 2026 16:50:53 GMT; path=/api/auth; httponly; samesite=lax"
        assertEquals("ABC123", parseRefreshCookie(raw))
        assertEquals("quoted", parseRefreshCookie("bankcore_refresh=\"quoted\"; Path=/api/auth"))
        assertNull(parseRefreshCookie("other=1; path=/"))
        assertNull(parseRefreshCookie("bankcore_refresh="))
    }

    @Test
    fun onlyAnaAndBrunoSignIn() {
        assertEquals(listOf("Ana Ribeiro", "Bruno Lima"), Holders.all.map { it.name })
        assertEquals("ana.ribeiro@vortexbank.demo", Holders.byCpf("390.533.447-05")?.email)
        assertEquals("Bruno Lima", Holders.byCpf("52998224725")?.name)
        assertNull(Holders.byCpf("111.444.777-35"))
        assertEquals("Ana-demo-2026", Holders.all[0].password)
        assertEquals("Bruno-demo-2026", Holders.all[1].password)
    }

    @Test
    fun moneyPathsAreTheLedgerOnes() {
        assertTrue(LedgerPaths.accepts("/pix"))
        assertTrue(LedgerPaths.accepts("/boletos/pay"))
        assertTrue(LedgerPaths.accepts("/cards/purchases"))
        assertTrue(LedgerPaths.accepts("/cards/invoices/abc/pay"))
        assertFalse(LedgerPaths.accepts("/ted"))
        assertFalse(LedgerPaths.accepts("/savings"))
        assertFalse(LedgerPaths.accepts("/clock/advance"))
        assertFalse(LedgerPaths.accepts("/pix/keys"))
    }

    @Test
    fun amountAndDayBalance() {
        assertEquals(0, parseAmount("1,50")!!.compareTo(BigDecimal("1.50")))
        assertEquals(0, parseAmount("1.234,56")!!.compareTo(BigDecimal("1234.56")))
        assertEquals(0, cents(BigDecimal("1.005")).compareTo(BigDecimal("1.01")))
        val lines = listOf(
            line("d2c", "2026-09-02", "2026-09-02T12:00:00Z", "credito", 10.0),
            line("d1", "2026-09-01", "2026-09-01T10:00:00Z", "credito", 20.0),
            line("d2d", "2026-09-02", "2026-09-02T10:00:00Z", "debito", 5.0),
        )
        val groups = groupStatement(lines, 100.0)
        assertEquals(listOf("2026-09-02", "2026-09-01"), groups.map { it.date })
        assertEquals(100.0, groups[0].balance, 0.001)
        assertEquals(listOf("d2d", "d2c"), groups[0].lines.map { it.journalId })
        assertEquals(95.0, groups[1].balance, 0.001)
    }

    @Test
    fun loginKeepsRefreshAndRetriesMoneyWithTheSameKey() = runBlocking {
        var pixCalls = 0
        val idempotency = mutableListOf<String?>()
        server.dispatcher = object : Dispatcher() {
            override fun dispatch(request: RecordedRequest): MockResponse {
                val path = request.path.orEmpty()
                return when {
                    path == "/login" -> json(auth("token-1"), "bankcore_refresh=ABC123; Path=/api/auth; HttpOnly")
                    path == "/me/provision" -> {
                        assertEquals("Bearer token-1", request.getHeader("Authorization"))
                        assertTrue(request.body.readUtf8().contains("39053344705"))
                        MockResponse().setResponseCode(204)
                    }
                    path == "/pix" -> {
                        pixCalls += 1
                        idempotency += request.getHeader("Idempotency-Key")
                        if (pixCalls == 1) MockResponse().setResponseCode(401)
                        else json(receipt())
                    }
                    path == "/refresh" -> {
                        assertEquals("/refresh", path)
                        assertEquals("bankcore_refresh=ABC123", request.getHeader("Cookie"))
                        json(auth("token-2"), "bankcore_refresh=NEXT; Path=/api/auth; HttpOnly")
                    }
                    else -> MockResponse().setResponseCode(404)
                }
            }
        }
        val base = server.url("/").toString().trimEnd('/')
        val client = BankClient(OkHttpClient(), base, base)
        val profile = client.login("ana.ribeiro@vortexbank.demo", "Ana-demo-2026")
        assertEquals("Ana Ribeiro", profile.name)
        assertEquals("ana.ribeiro@vortexbank.demo", profile.email)
        val proof = client.sendPix("52998224725", BigDecimal("1.50"))
        assertEquals("AUT-1", proof.authentication)
        assertEquals(2, pixCalls)
        assertEquals(idempotency[0], idempotency[1])
        assertTrue(!idempotency[0].isNullOrBlank())
    }

    private fun auth(token: String) = """
        {"userId":"u1","name":"Ana Ribeiro","email":"ana.ribeiro@vortexbank.demo","cpf":"39053344705","accessToken":"$token","accessExpiresUtc":"2026-09-27T18:00:00Z"}
    """.trimIndent()

    private fun receipt() = """
        {"journalId":"j1","businessDate":"2026-09-27","kind":"Pix","amount":1.5,"description":"Pix","authentication":"AUT-1"}
    """.trimIndent()

    private fun json(body: String, cookie: String? = null): MockResponse {
        val response = MockResponse().setResponseCode(200).setBody(body)
        if (cookie != null) response.addHeader("Set-Cookie", cookie)
        return response
    }

    private fun line(id: String, date: String, created: String, direction: String, amount: Double) = Line(
        journalId = id,
        businessDate = date,
        createdAt = created,
        kind = "Pix",
        direction = direction,
        amount = amount,
        description = id,
    )
}
