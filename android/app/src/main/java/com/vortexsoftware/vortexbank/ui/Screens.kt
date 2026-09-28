package com.vortexsoftware.vortexbank.ui

import android.content.ClipData
import android.content.ClipboardManager
import androidx.activity.compose.BackHandler
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.horizontalScroll
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Slider
import androidx.compose.material3.SliderDefaults
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableFloatStateOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.vortexsoftware.vortexbank.data.Home
import com.vortexsoftware.vortexbank.data.Profile
import com.vortexsoftware.vortexbank.data.StatementDocument
import com.vortexsoftware.vortexbank.data.accountLabel
import com.vortexsoftware.vortexbank.data.checkingOf
import com.vortexsoftware.vortexbank.data.day
import com.vortexsoftware.vortexbank.data.dayTitle
import com.vortexsoftware.vortexbank.data.firstName
import com.vortexsoftware.vortexbank.data.issuedNow
import com.vortexsoftware.vortexbank.data.kindLabel
import com.vortexsoftware.vortexbank.data.knownRecipients
import com.vortexsoftware.vortexbank.data.maskCpf
import com.vortexsoftware.vortexbank.data.merchants
import com.vortexsoftware.vortexbank.data.money
import com.vortexsoftware.vortexbank.data.monthShort
import com.vortexsoftware.vortexbank.data.openBills
import com.vortexsoftware.vortexbank.data.statementMonths
import com.vortexsoftware.vortexbank.data.visibleStatement
import com.vortexsoftware.vortexbank.data.whenLabel

@Composable
fun PageBody(content: @Composable () -> Unit) {
    Column(
        Modifier.fillMaxSize().verticalScroll(rememberScrollState()).padding(16.dp),
        verticalArrangement = Arrangement.spacedBy(12.dp),
    ) {
        content()
    }
}

@Composable
fun HomeScreen(model: BankModel, name: String) {
    val home = model.home
    var hidden by rememberSaveable { mutableStateOf(false) }
    if (home == null) {
        PageBody { Text("Carregando saldos…") }
        return
    }
    val checking = checkingOf(home)
    val bills = openBills(home)
    val due = bills.sumOf { it.amount }
    val credit = home.cards.firstOrNull { it.kind == "credito" }
    fun show(value: Double) = if (hidden) "R$ •••••" else money(value)
    PageBody {
        Row(Modifier.fillMaxWidth(), verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.spacedBy(10.dp)) {
            Avatar(name, large = true)
            Column(Modifier.weight(1f)) {
                Text("Olá, ${firstName(name)}", fontSize = 26.sp, fontWeight = FontWeight.ExtraBold)
                Text("Vortex Bank · Ag 0001 · Cc ${checking?.number ?: "—"}", color = Muted, fontSize = 13.sp)
            }
            GhostButton(if (hidden) "Mostrar" else "Ocultar") { hidden = !hidden }
        }
        Panel {
            Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
                Text("Saldo em conta corrente", color = Muted, fontWeight = FontWeight.Bold)
                Pill("Ledger", live = true)
            }
            Text(show(checking?.balance ?: 0.0), fontSize = 34.sp, fontWeight = FontWeight.ExtraBold)
            Text("Partidas dobradas ativas · nenhum valor é real", color = Muted, fontSize = 13.sp)
        }
        Panel {
            Text("Boletos em aberto", color = Muted, fontWeight = FontWeight.Bold)
            Text(show(due), color = Rose, fontSize = 28.sp, fontWeight = FontWeight.ExtraBold)
            Text("${bills.size} boletos a liquidar neste ambiente", color = Muted, fontSize = 13.sp)
        }
        HubCard("Transferir Pix", "Enviar pela chave de outro titular", "↗") { model.open(Tab.Pix) }
        HubCard("Extrato", "Corrente e poupança, por dia", "≡") { model.open(Tab.Statement) }
        HubCard("Pagar boleto", "Liquida no ledger", "▤") { model.openPayments() }
        HubCard("Investimentos", "Cenário ilustrativo", "%") { model.openInvest() }
        HubCard("Cartões", "Fatura e compra", "▭") { model.open(Tab.Cards) }
        HubCard("Comprovante", "Último lançamento do painel", "⌘") {
            val id = home.recent.firstOrNull()?.journalId
            if (id == null) model.error = "Abra um lançamento do extrato para ver o comprovante."
            else model.openReceipt(id)
        }
        HubCard("Perfil", "Dados do titular", "☺") { model.openProfile() }
        Panel {
            Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween, verticalAlignment = Alignment.CenterVertically) {
                Text("Extrato recente", color = Muted, fontWeight = FontWeight.Bold)
                Text("Ver completo", color = Rose, fontWeight = FontWeight.Bold, modifier = Modifier.clickable { model.open(Tab.Statement) })
            }
            if (home.recent.isEmpty()) Text("Nenhum lançamento ainda.", color = Muted)
            home.recent.take(4).forEach { line ->
                Row(
                    Modifier.fillMaxWidth().clickable { model.openReceipt(line.journalId) }.padding(vertical = 8.dp),
                    horizontalArrangement = Arrangement.SpaceBetween,
                ) {
                    Column(Modifier.weight(1f)) {
                        Text(line.description, fontWeight = FontWeight.Bold)
                        Text(day(line.businessDate), color = Muted, fontSize = 12.sp)
                    }
                    val debit = line.direction == "debito"
                    Text(
                        "${if (debit) "−" else "+"} ${if (hidden) "••••" else money(line.amount)}",
                        color = if (debit) Rose else Credit,
                        fontWeight = FontWeight.ExtraBold,
                    )
                }
            }
        }
        Panel {
            Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
                Text("Cartão principal", color = Muted, fontWeight = FontWeight.Bold)
                Pill("Simulado")
            }
            Spacer(Modifier.height(10.dp))
            Plastic(credit?.holder ?: name, credit?.pan, if (credit?.openInvoiceDue != null) "Vence ${day(credit.openInvoiceDue)}" else credit?.expiry, "Crédito")
            Text("Fatura ${money(credit?.openInvoiceAmount ?: 0.0)} · usado ${money(credit?.used ?: 0.0)}", color = Muted, modifier = Modifier.padding(top = 8.dp))
        }
    }
}

@Composable
fun Plastic(holder: String, pan: String?, extra: String?, kind: String, debit: Boolean = false) {
    Column(
        Modifier.fillMaxWidth().height(150.dp).clip(RoundedCornerShape(16.dp)).background(if (debit) DebitBrush else PlasticBrush).padding(16.dp),
        verticalArrangement = Arrangement.SpaceBetween,
    ) {
        Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
            Text("Vortex", color = Color.White, fontWeight = FontWeight.Bold)
            Text(kind, color = Color.White, fontWeight = FontWeight.Bold)
        }
        Text("•••• ${pan?.takeLast(4) ?: "0000"}", color = Color.White, fontWeight = FontWeight.Bold, letterSpacing = 2.sp, fontSize = 18.sp)
        Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
            Text(holder.uppercase(), color = Color.White, fontWeight = FontWeight.Bold, fontSize = 12.sp)
            Text(extra ?: "", color = Color.White, fontSize = 12.sp)
        }
    }
}

@Composable
fun PixScreen(model: BankModel) {
    var view by rememberSaveable { mutableStateOf("menu") }
    var fake by rememberSaveable { mutableStateOf("") }
    var key by rememberSaveable { mutableStateOf(otherCpf(model.profile?.cpf)) }
    var amount by rememberSaveable { mutableStateOf("1.00") }
    var message by rememberSaveable { mutableStateOf("") }
    var ok by rememberSaveable { mutableStateOf(false) }
    var copied by rememberSaveable { mutableStateOf(false) }
    val home = model.home
    val checking = checkingOf(home)
    val ownKey = home?.pixKeys?.firstOrNull()?.value.orEmpty()
    val recipient = knownRecipients[key.filter(Char::isDigit)]
    val context = LocalContext.current
    BackHandler(enabled = view != "menu") {
        view = "menu"
        message = ""
    }
    PageBody {
        Text("Central Pix", fontSize = 28.sp, fontWeight = FontWeight.ExtraBold)
        Text("Pix interno entre clientes do Vortex Bank. QR Code, copia e cola, presente e golpe são vitrine e não gravam no ledger.", color = Muted)
        when (view) {
            "menu" -> {
                SectionLabel("PAGAR")
                HubCard("Fazer um Pix", "Transfere no ledger interno por CPF. Debita a origem e credita o destino.", "↗") { view = "send" }
                HubCard("Ler QR Code", "Vitrine. Não lê câmera nem paga um código de fora.", "▣") { fake = "Ler QR Code"; view = "fake" }
                HubCard("Pix Copia e Cola", "Vitrine. O código colado não liquida no ledger.", "⌘") { fake = "Pix Copia e Cola"; view = "fake" }
                HubCard("Pix de presente", "Vitrine. Cartão comemorativo de demonstração. Não envia valor.", "✦") { fake = "Pix de presente"; view = "fake" }
                SectionLabel("RECEBER")
                HubCard("Criar QR Code", "Vitrine de cobrança. Não gera um código pagável.", "▣") { fake = "Criar QR Code"; view = "fake" }
                HubCard("Minhas chaves Pix", "A chave de CPF desta conta, para outro cliente do laboratório enviar.", "◉") { view = "keys" }
                SectionLabel("CONSULTAR")
                HubCard("Extrato Pix", "Lançamentos e comprovantes que de fato passaram pelo ledger.", "≡") { model.open(Tab.Statement) }
                HubCard("Meus limites Pix", "Limite diário de R$ 20.000,00 neste simulador. Tarifa zero.", "▤") { view = "limits" }
                HubCard("Informar golpe ou fraude", "Vitrine. Não existe devolução especial aqui. Nenhum estorno é disparado.", "!", warn = true) { fake = "Informar golpe"; view = "fake" }
            }
            "send" -> {
                Chip("Voltar à Central") { view = "menu"; message = "" }
                Panel {
                    Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
                        Text("Saldo disponível para Pix", color = Muted, fontWeight = FontWeight.Bold)
                        Pill("Liquidação imediata", live = true)
                    }
                    Text(if (checking == null) "—" else money(checking.balance), fontSize = 30.sp, fontWeight = FontWeight.ExtraBold)
                    AmountField("Chave Pix do destinatário", key) { key = it }
                    if (recipient != null) {
                        Row(
                            Modifier.fillMaxWidth().clip(RoundedCornerShape(14.dp)).background(Blush).padding(12.dp),
                            horizontalArrangement = Arrangement.SpaceBetween,
                        ) {
                            Column {
                                Text("DESTINATÁRIO", color = Muted, fontSize = 11.sp, fontWeight = FontWeight.Bold)
                                Text(recipient, fontWeight = FontWeight.Bold)
                                Text("Vortex Bank · agência 0001", color = Muted, fontSize = 12.sp)
                            }
                            Pill("No simulador", live = true)
                        }
                    }
                    AmountField("Valor da transferência", amount, KeyboardType.Decimal) { amount = it }
                    Row(horizontalArrangement = Arrangement.spacedBy(8.dp), modifier = Modifier.horizontalScroll(rememberScrollState())) {
                        listOf(1, 10, 50, 100).forEach { step ->
                            Chip("+ ${money(step.toDouble())}") {
                                val current = amount.replace(',', '.').toDoubleOrNull() ?: 0.0
                                amount = String.format(java.util.Locale.US, "%.2f", current + step)
                            }
                        }
                    }
                    if (message.isNotBlank()) Text(message, color = if (ok) Credit else RosePressed, fontWeight = FontWeight.Bold)
                    Spacer(Modifier.height(8.dp))
                    PrimaryButton("Confirmar e transferir Pix") {
                        model.sendPix(key, amount) { text, success ->
                            message = text
                            ok = success
                        }
                    }
                }
                Note("Só “Fazer um Pix” grava partidas. As outras opções da central são vitrine e não movem saldo.")
            }
            "keys" -> {
                Chip("Voltar à Central") { view = "menu" }
                Panel {
                    Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
                        Text("Sua chave Pix", color = Muted, fontWeight = FontWeight.Bold)
                        Pill("CPF")
                    }
                    Text(ownKey.ifBlank { "—" }, fontWeight = FontWeight.Bold, modifier = Modifier.padding(vertical = 8.dp))
                    GhostButton(if (copied) "Copiada" else "Copiar") {
                        val board = context.getSystemService(ClipboardManager::class.java)
                        board?.setPrimaryClip(ClipData.newPlainText("chave pix", ownKey))
                        copied = true
                    }
                }
            }
            "limits" -> {
                Chip("Voltar à Central") { view = "menu" }
                Panel {
                    PairRow("Limite diário", "R$ 20.000,00")
                    PairRow("Tarifa", "R$ 0,00")
                    Note("Acima desse valor no mesmo dia útil, o ledger recusa o Pix. Não há limite noturno separado.")
                }
            }
            else -> {
                Chip("Voltar à Central") { view = "menu" }
                Panel {
                    Text(fake, fontSize = 26.sp, fontWeight = FontWeight.ExtraBold)
                    Spacer(Modifier.height(8.dp))
                    Pill("Simulado")
                    Spacer(Modifier.height(8.dp))
                    Note("Esta opção é vitrine. Ela não lê QR, não cola payload e não estorna valor. O saldo permanece o do ledger.")
                }
            }
        }
    }
}

@Composable
fun StatementScreen(model: BankModel, profile: Profile) {
    val home = model.home
    var accountId by rememberSaveable { mutableStateOf("") }
    var month by rememberSaveable { mutableStateOf("") }
    var query by rememberSaveable { mutableStateOf("") }
    val context = LocalContext.current
    LaunchedEffect(home) {
        val accounts = home?.accounts.orEmpty()
        if (accountId.isBlank() || accounts.none { it.id == accountId }) {
            accountId = accounts.firstOrNull { it.kind == "corrente" }?.id ?: accounts.firstOrNull()?.id.orEmpty()
            month = ""
        }
    }
    LaunchedEffect(accountId, home?.accounts) {
        if (accountId.isNotBlank()) model.loadStatement(accountId)
    }
    val account = home?.accounts?.firstOrNull { it.id == accountId }
    val months = statementMonths(model.statement)
    val selected = if (month.isNotBlank() && month in months) month else months.lastOrNull().orEmpty()
    val (groups, totals) = visibleStatement(model.statement, account?.balance ?: 0.0, selected, query)
    PageBody {
        Text("Extrato", fontSize = 28.sp, fontWeight = FontWeight.ExtraBold)
        Text("Selecione o extrato que deseja detalhar.")
        Row(horizontalArrangement = Arrangement.spacedBy(8.dp), modifier = Modifier.horizontalScroll(rememberScrollState())) {
            home?.accounts.orEmpty().forEach { item ->
                Chip(accountLabel(item.kind), item.id == accountId) {
                    accountId = item.id
                    month = ""
                }
            }
        }
        Row(horizontalArrangement = Arrangement.spacedBy(8.dp), verticalAlignment = Alignment.CenterVertically, modifier = Modifier.horizontalScroll(rememberScrollState())) {
            Text(selected.take(4).ifBlank { "—" }, modifier = Modifier.clip(RoundedCornerShape(99.dp)).background(Color.White).padding(horizontal = 10.dp, vertical = 6.dp), fontWeight = FontWeight.Bold)
            months.forEach { item ->
                val index = item.substring(5).toIntOrNull()?.minus(1) ?: 0
                Chip(monthShort.getOrElse(index) { item }, item == selected) { month = item }
            }
        }
        AmountField("Filtrar por", query) { query = it }
        GhostButton("Salvar PDF") {
            try {
                shareStatement(context, document(profile, account?.kind, account?.number, groups, totals.first, totals.second))
            } catch (_: android.content.ActivityNotFoundException) {
                model.error = "Não foi possível abrir a folha de compartilhamento."
            }
        }
        if (groups.isEmpty()) Text("Nenhum lançamento neste mês.", color = Muted)
        groups.forEach { group ->
            Text(
                dayTitle(group.date),
                modifier = Modifier.fillMaxWidth().clip(RoundedCornerShape(8.dp)).background(DayBar).padding(horizontal = 12.dp, vertical = 8.dp),
                fontWeight = FontWeight.ExtraBold,
            )
            Row(Modifier.fillMaxWidth().padding(vertical = 6.dp), horizontalArrangement = Arrangement.SpaceBetween) {
                Text("Saldo do dia", fontWeight = FontWeight.Bold)
                Text(money(group.balance), fontWeight = FontWeight.Bold)
            }
            group.lines.forEach { line ->
                val debit = line.direction == "debito"
                Row(
                    Modifier.fillMaxWidth().clickable { model.openReceipt(line.journalId) }.padding(vertical = 8.dp),
                    horizontalArrangement = Arrangement.SpaceBetween,
                    verticalAlignment = Alignment.CenterVertically,
                ) {
                    Column(Modifier.weight(1f).padding(end = 12.dp)) {
                        Text("${kindLabel(line.kind)} · ${if (debit) "enviado" else "recebido"}", fontWeight = FontWeight.Bold)
                        Text("${whenLabel(line)} · ${line.description}", color = Muted, fontSize = 12.sp)
                    }
                    Text(
                        "${if (debit) "−" else "+"} ${money(line.amount)}",
                        color = if (debit) Rose else Credit,
                        fontWeight = FontWeight.ExtraBold,
                    )
                }
            }
        }
        Text("Entradas + ${money(totals.first)}", color = Credit, fontWeight = FontWeight.ExtraBold)
        Text("Saídas − ${money(totals.second)}", color = Rose, fontWeight = FontWeight.ExtraBold)
        Panel {
            Text("Saldos", fontWeight = FontWeight.Bold)
            PairRow("Saldo", if (account == null) "—" else money(account.balance))
            PairRow(accountLabel(account?.kind ?: "corrente"), if (account == null) "—" else money(account.balance))
            PairRow("Saldo disponível", if (account == null) "—" else money(account.balance))
            Text("${profile.name}\nCPF ${maskCpf(profile.cpf)} · Ag. 0001 · Cc. ${account?.number ?: "—"}", color = Muted)
        }
    }
}

private fun document(
    profile: Profile,
    kind: String?,
    number: String?,
    groups: List<com.vortexsoftware.vortexbank.data.DayGroup>,
    credits: Double,
    debits: Double,
) = StatementDocument(
    holder = profile.name,
    cpf = maskCpf(profile.cpf),
    account = accountLabel(kind ?: "corrente"),
    number = number ?: "—",
    issuedAt = issuedNow(),
    credits = "+ ${money(credits)}",
    debits = "− ${money(debits)}",
    groups = groups,
)

@Composable
fun PaymentsScreen(model: BankModel) {
    val home = model.home
    val checking = checkingOf(home)
    val open = openBills(home)
    val total = open.sumOf { it.amount }
    var message by rememberSaveable { mutableStateOf("") }
    PageBody {
        Text("Central de Pagamentos", fontSize = 28.sp, fontWeight = FontWeight.ExtraBold)
        Text("Pagar um boleto desta lista grava no ledger. Débito automático e o teto de R$ 50.000,00 são vitrine.", color = Muted)
        Panel {
            Text("Saldo disponível", color = Muted, fontWeight = FontWeight.Bold)
            Text(if (checking == null) "—" else money(checking.balance), fontSize = 30.sp, fontWeight = FontWeight.ExtraBold)
            Text("Conta corrente · agência 0001", color = Muted)
        }
        Panel {
            Text("Agendamentos em aberto", color = Muted, fontWeight = FontWeight.Bold)
            Text("− ${money(total)}", color = Rose, fontSize = 30.sp, fontWeight = FontWeight.ExtraBold)
            Text("Boletos desta lista ainda não pagos", color = Muted)
        }
        HubCard("Pix", "Atalho para a central", "↗") { model.open(Tab.Pix) }
        HubCard("Pagar fatura", "Cartão de crédito", "▭") { model.open(Tab.Cards) }
        HubCard("Agenda DDA", "Vitrine. Só os boletos da lista abaixo podem ser pagos.", "▦") {
            message = "A agenda DDA é vitrine. Só os boletos da lista abaixo podem ser pagos."
        }
        Panel {
            Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
                Text("Boletos em aberto", fontWeight = FontWeight.Bold)
                Pill("${open.size} para pagar", live = true)
            }
            Text("Emitidos dentro do Vortex Bank. A linha não quita boleto de fora.", color = Muted)
            if (open.isEmpty()) Text("Nenhum boleto em aberto.", color = Muted)
            open.forEach { boleto ->
                Column(Modifier.fillMaxWidth().padding(vertical = 8.dp)) {
                    Text(boleto.beneficiary, fontWeight = FontWeight.Bold)
                    Text("Vencimento ${day(boleto.dueDate)}", color = Muted, fontSize = 12.sp)
                    Text(money(boleto.amount), fontWeight = FontWeight.Bold)
                    Spacer(Modifier.height(6.dp))
                    Chip("Pagar") { model.payBoleto(boleto.line) { text, _ -> message = text } }
                }
            }
            if (message.isNotBlank()) Text(message, fontWeight = FontWeight.Bold, modifier = Modifier.padding(top = 8.dp))
        }
        Panel {
            Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
                Text("Contas em débito automático", fontWeight = FontWeight.Bold)
                Pill("Simulado")
            }
            listOf(
                "Enel Energia" to "Débito ilustrativo todo dia 15",
                "Sabesp Água" to "Débito ilustrativo todo dia 18",
                "Fibra Internet" to "Débito ilustrativo todo dia 22",
            ).forEach { (name, whenText) ->
                Row(Modifier.fillMaxWidth().padding(vertical = 6.dp), horizontalArrangement = Arrangement.SpaceBetween) {
                    Column {
                        Text(name, fontWeight = FontWeight.Bold)
                        Text(whenText, color = Muted, fontSize = 12.sp)
                    }
                    Pill("✓", live = true)
                }
            }
        }
        Panel {
            Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
                Text("Limite de pagamento", fontWeight = FontWeight.Bold)
                Pill("Simulado")
            }
            PairRow("Teto ilustrativo de boletos no dia", "R$ 50.000,00")
            Box(Modifier.fillMaxWidth().height(6.dp).clip(RoundedCornerShape(99.dp)).background(Color(0xFFF3E7EB))) {
                Box(Modifier.fillMaxWidth(0.7f).height(6.dp).background(Band))
            }
            Text("O ledger não aplica esse teto. Ele só recusa quando o saldo da corrente não cobre o boleto.", color = Muted, modifier = Modifier.padding(top = 8.dp))
        }
    }
}

@Composable
fun CardsScreen(model: BankModel) {
    val home = model.home
    val credit = home?.cards?.firstOrNull { it.kind == "credito" }
    val debit = home?.cards?.firstOrNull { it.kind == "debito" }
    var message by rememberSaveable { mutableStateOf("") }
    var showCvv by rememberSaveable { mutableStateOf(false) }
    var cardId by rememberSaveable { mutableStateOf("") }
    var merchant by rememberSaveable { mutableStateOf(merchants.first()) }
    var amount by rememberSaveable { mutableStateOf("") }
    LaunchedEffect(home) {
        if (cardId.isBlank() || home?.cards?.none { it.id == cardId } == true) {
            cardId = home?.cards?.firstOrNull()?.id.orEmpty()
        }
    }
    val used = credit?.used ?: 0.0
    val limit = credit?.limit ?: 0.0
    val ratio = if (limit > 0) (used / limit).toFloat().coerceIn(0f, 1f) else 0f
    PageBody {
        Text("Gestão de cartões", fontSize = 28.sp, fontWeight = FontWeight.ExtraBold)
        Text("Plásticos do laboratório. Pagar fatura e lançar compra gravam no ledger. O restante é vitrine.", color = Muted)
        Panel {
            Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
                Text("Cartão principal", fontWeight = FontWeight.Bold)
                Pill("Ativo", live = true)
            }
            Spacer(Modifier.height(10.dp))
            Plastic(credit?.holder ?: "—", credit?.pan, if (credit?.openInvoiceDue != null) "Vence ${day(credit.openInvoiceDue)}" else credit?.expiry, "Crédito")
            Spacer(Modifier.height(8.dp))
            PairRow("Limite utilizado", "${money(used)} de ${money(limit)}")
            Box(Modifier.fillMaxWidth().height(6.dp).clip(RoundedCornerShape(99.dp)).background(Color(0xFFF3E7EB))) {
                Box(Modifier.fillMaxWidth(ratio).height(6.dp).background(Band))
            }
            Row(Modifier.fillMaxWidth().padding(top = 10.dp), horizontalArrangement = Arrangement.SpaceBetween, verticalAlignment = Alignment.CenterVertically) {
                Column {
                    Text("FATURA", color = Muted, fontSize = 11.sp, fontWeight = FontWeight.Bold)
                    Text(money(credit?.openInvoiceAmount ?: 0.0), color = Rose, fontWeight = FontWeight.ExtraBold, fontSize = 20.sp)
                }
                if (credit?.openInvoiceId != null) {
                    GhostButton("Pagar fatura") { model.payInvoice(credit.openInvoiceId) { text, _ -> message = text } }
                } else {
                    Pill("Sem fatura aberta")
                }
            }
            Spacer(Modifier.height(8.dp))
            GhostButton("Ajustar limite", modifier = Modifier.fillMaxWidth()) {
                message = "Ajustar limite é vitrine. O limite deste cartão continua o que o ledger devolveu."
            }
            Spacer(Modifier.height(8.dp))
            GhostButton("Bloquear cartão", modifier = Modifier.fillMaxWidth()) {
                message = "Bloquear cartão é vitrine. Nenhuma compra foi impedida no ledger."
            }
        }
        Panel {
            Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
                Text("Cartão de débito", fontWeight = FontWeight.Bold)
                Pill("Conta corrente")
            }
            Spacer(Modifier.height(10.dp))
            Plastic(debit?.holder ?: "—", debit?.pan, if (showCvv) "CVV ${debit?.cvv ?: "—"}" else "CVV oculto", "Débito", debit = true)
            Text("Compras no débito saem da corrente, na hora.", color = Muted, modifier = Modifier.padding(vertical = 8.dp))
            GhostButton(if (showCvv) "Ocultar CVV" else "Ver dados e CVV", modifier = Modifier.fillMaxWidth()) { showCvv = !showCvv }
            Spacer(Modifier.height(8.dp))
            GhostButton("Gerar novo cartão", modifier = Modifier.fillMaxWidth()) {
                message = "Gerar novo cartão é vitrine. O número deste plástico não muda."
            }
        }
        Panel { Text("Conciliação", fontWeight = FontWeight.Bold); Text("Exportar OFX é vitrine. O extrato oficial é o da tela Extrato.", color = Muted) }
        Panel { Text("Seguro", fontWeight = FontWeight.Bold); Text("Não há apólice. O cartão só existe dentro do simulador.", color = Muted) }
        Panel { Text("Controle de limites", fontWeight = FontWeight.Bold); Text("O crédito recusa compra acima do limite. Isso o ledger aplica de verdade.", color = Muted) }
        Panel {
            Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
                Text("Nova compra no ledger", fontWeight = FontWeight.Bold)
                Pill("Grava partida", live = true)
            }
            Text("Cartão", fontWeight = FontWeight.Bold, modifier = Modifier.padding(top = 8.dp))
            Row(horizontalArrangement = Arrangement.spacedBy(8.dp), modifier = Modifier.horizontalScroll(rememberScrollState())) {
                home?.cards.orEmpty().forEach { card ->
                    Chip("${if (card.kind == "credito") "Crédito" else "Débito"} · ${card.pan.takeLast(4)}", card.id == cardId) { cardId = card.id }
                }
            }
            Text("Comerciante", fontWeight = FontWeight.Bold)
            Row(horizontalArrangement = Arrangement.spacedBy(8.dp), modifier = Modifier.horizontalScroll(rememberScrollState())) {
                merchants.forEach { item -> Chip(item, item == merchant) { merchant = item } }
            }
            AmountField("Valor", amount, KeyboardType.Decimal) { amount = it }
            if (message.isNotBlank()) Text(message, fontWeight = FontWeight.Bold)
            PrimaryButton("Lançar compra", enabled = cardId.isNotBlank()) {
                model.purchase(cardId, merchant, amount) { text, _ -> message = text }
            }
        }
    }
}

@Composable
fun InvestScreen() {
    var aporte by remember { mutableFloatStateOf(10_000f) }
    var notice by rememberSaveable { mutableStateOf("") }
    val noApply = { notice = "Este módulo não aplica nem resgata. O saldo da conta corrente permanece o do ledger." }
    PageBody {
        Text("Vortex Invest", fontSize = 28.sp, fontWeight = FontWeight.ExtraBold)
        Text("Números de cenário, iguais para qualquer cliente. Investir e novo aporte não alteram saldo.", color = Muted)
        PrimaryButton("Novo aporte", onClick = noApply)
        Panel {
            Text("PATRIMÔNIO ILUSTRATIVO", color = Muted, fontSize = 11.sp, fontWeight = FontWeight.Bold)
            Text("R$ 45.890,20", color = Credit, fontSize = 26.sp, fontWeight = FontWeight.ExtraBold)
            Text("+ R$ 480,20 neste cenário", color = Credit, fontSize = 12.sp)
            Spacer(Modifier.height(8.dp))
            Text("REFERÊNCIA", color = Muted, fontSize = 11.sp, fontWeight = FontWeight.Bold)
            Text("104% do CDI", fontSize = 22.sp, fontWeight = FontWeight.ExtraBold)
            Spacer(Modifier.height(8.dp))
            Text("LIQUIDEZ IMEDIATA (D+0)", color = Muted, fontSize = 11.sp, fontWeight = FontWeight.Bold)
            Text("R$ 25.000,00", color = Rose, fontSize = 22.sp, fontWeight = FontWeight.ExtraBold)
            Text("65% renda fixa · 20% Tesouro · 15% fundos", color = Muted, modifier = Modifier.padding(top = 8.dp))
            Row(Modifier.fillMaxWidth().height(8.dp).clip(RoundedCornerShape(99.dp))) {
                Box(Modifier.weight(0.65f).height(8.dp).background(RoseSoft))
                Box(Modifier.weight(0.20f).height(8.dp).background(Rose))
                Box(Modifier.weight(0.15f).height(8.dp).background(Wine))
            }
        }
        Text("Produtos de renda fixa e títulos", fontWeight = FontWeight.Bold, fontSize = 18.sp)
        Product("CDB Liquidez Diária", "104% do CDI · resgate ilustrativo em D+0", "FGC", "Aplicação mínima: R$ 100,00", noApply)
        Product("LCA Sustentabilidade Agro", "98% do CDI · isento de IR na vitrine", "Isento", "Vencimento ilustrativo: 12 meses", noApply)
        Product("Tesouro Selic 2029", "Selic + 0,15% ao ano", "Tesouro", "Aplicação mínima: R$ 150,00", noApply)
        Product("Fundo Vortex Tech", "Rentabilidade ilustrativa em 12 meses: + 24,8%", "Fundo", "Série fictícia, sem custódia", noApply)
        Panel {
            Text("Simulador de rendimento em 12 meses", fontWeight = FontWeight.Bold)
            PairRow("Valor do aporte simulado", money(aporte.toDouble()))
            Slider(
                value = aporte,
                onValueChange = { aporte = it },
                valueRange = 1_000f..50_000f,
                steps = 97,
                colors = SliderDefaults.colors(thumbColor = Rose, activeTrackColor = Rose),
            )
            Text("Neste cenário (104% do CDI)", color = Muted, fontSize = 12.sp)
            Text(money(aporte * 1.1144), color = Credit, fontWeight = FontWeight.ExtraBold, fontSize = 22.sp)
            Text("Na poupança antiga, só para comparar", color = Muted, fontSize = 12.sp)
            Text(money(aporte * 1.0617), fontWeight = FontWeight.ExtraBold, fontSize = 22.sp)
            if (notice.isNotBlank()) Note(notice)
        }
    }
}

@Composable
private fun Product(name: String, detail: String, tag: String, extra: String, onInvest: () -> Unit) {
    Panel {
        Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
            Text(name, fontWeight = FontWeight.Bold, modifier = Modifier.weight(1f))
            Pill(tag)
        }
        Text(detail, color = Muted)
        Text(extra, color = Muted, fontSize = 12.sp)
        Spacer(Modifier.height(8.dp))
        PrimaryButton("Investir", onClick = onInvest)
    }
}

@Composable
fun ProfileScreen(profile: Profile, home: Home?) {
    val checking = checkingOf(home)
    var notice by rememberSaveable { mutableStateOf("") }
    var bio by rememberSaveable { mutableStateOf(true) }
    PageBody {
        Panel {
            Row(horizontalArrangement = Arrangement.spacedBy(12.dp), verticalAlignment = Alignment.CenterVertically) {
                Avatar(profile.name, large = true)
                Column(Modifier.weight(1f)) {
                    Text(profile.name, fontWeight = FontWeight.ExtraBold, fontSize = 20.sp)
                    Text("CPF ${maskCpf(profile.cpf)}", color = Muted)
                    Text("Ag. 0001 · Cc. ${checking?.number ?: "—"} · Vortex Bank", color = Muted, fontSize = 13.sp)
                }
            }
            Text("Ativa neste simulador", color = Credit, fontWeight = FontWeight.Bold, modifier = Modifier.padding(top = 8.dp))
        }
        Panel {
            Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
                Text("Dados cadastrais do titular", fontWeight = FontWeight.Bold)
                Pill("Somente leitura")
            }
            ReadOnly("Nome completo", profile.name)
            ReadOnly("CPF do titular", maskCpf(profile.cpf))
            ReadOnly("E-mail", profile.email)
            ReadOnly("Telefone", "(11) 90000-0000")
            Text("Telefone é vitrine.", color = Muted, fontSize = 12.sp)
            ReadOnly("Endereço", "Não informado neste laboratório")
            Text("Endereço é vitrine.", color = Muted, fontSize = 12.sp)
            PrimaryButton("Salvar alterações cadastrais") {
                notice = "Nome, CPF e e-mail já são os da conta. Telefone e endereço não são gravados."
            }
            if (notice.isNotBlank()) Note(notice)
        }
        Panel {
            Text("Segurança", fontWeight = FontWeight.Bold)
            Row(Modifier.fillMaxWidth().padding(vertical = 8.dp), horizontalArrangement = Arrangement.SpaceBetween, verticalAlignment = Alignment.CenterVertically) {
                Column(Modifier.weight(1f)) {
                    Text("Biometria", fontWeight = FontWeight.Bold)
                    Text("Vitrine. Não autentica compra.", color = Muted, fontSize = 12.sp)
                }
                Chip(if (bio) "Ligada" else "Desligada", bio) { bio = !bio }
            }
            Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
                Column(Modifier.weight(1f)) {
                    Text("Token", fontWeight = FontWeight.Bold)
                    Text("Não há segundo fator nesta versão.", color = Muted, fontSize = 12.sp)
                }
                Pill("Simulado")
            }
            Spacer(Modifier.height(8.dp))
            GhostButton("Alterar senha de acesso", modifier = Modifier.fillMaxWidth()) {
                notice = "A senha dos titulares de demonstração não é alterada por esta tela."
            }
        }
        Panel {
            Text("Dispositivos", fontWeight = FontWeight.Bold)
            PairRow("Este aparelho", "Ativo")
            Text("Não há outro aparelho conectado de verdade.", color = Muted)
        }
        Panel {
            Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
                Text("Dados da empresa", fontWeight = FontWeight.Bold)
                Pill("Simulado")
            }
            Text("A vitrine mostra a Vortex Software. Não é o cadastro do titular e não vai para o ledger.", color = Muted)
            Text("Razão social: Vortex Software", fontWeight = FontWeight.Bold, modifier = Modifier.padding(top = 8.dp))
            Text("Nome fantasia: Vortex Software", fontWeight = FontWeight.Bold)
            Text("Regime: ilustrativo", fontWeight = FontWeight.Bold)
            Text("Atividade ilustrativa: desenvolvimento de programas de computador sob encomenda.", color = Muted, modifier = Modifier.padding(top = 6.dp))
        }
    }
}

@Composable
fun MoreScreen(model: BankModel) {
    PageBody {
        Text("Mais áreas", fontSize = 28.sp, fontWeight = FontWeight.ExtraBold)
        HubCard("Central de Pagamentos", "Boletos que gravam no ledger", "▤") { model.openPayments() }
        HubCard("Vortex Invest", "Números de cenário, sem mexer no saldo", "%") { model.openInvest() }
        HubCard("Meu perfil e ajustes", "Titular, conta e vitrines", "☺") { model.openProfile() }
        LeaveButton { model.leave() }
    }
}

@Composable
fun ReceiptScreen(model: BankModel, journalId: String) {
    LaunchedEffect(journalId) { model.loadReceipt(journalId) }
    val receipt = model.receipt
    PageBody {
        Chip("Voltar") { model.back() }
        if (receipt == null || receipt.journalId != journalId) {
            Text("Abrindo comprovante…")
        } else {
            Panel {
                Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
                    Text("COMPROVANTE", color = Rose, fontWeight = FontWeight.ExtraBold, fontSize = 12.sp)
                    Pill("Liquidado no ledger", live = true)
                }
                Text(receipt.description, fontSize = 26.sp, fontWeight = FontWeight.ExtraBold)
                Text(money(receipt.amount), fontSize = 32.sp, fontWeight = FontWeight.ExtraBold)
                PairRow("Dia útil", day(receipt.businessDate))
                PairRow("Tipo", kindLabel(receipt.kind))
                PairRow("Autenticação", receipt.authentication)
                Note("Documento de demonstração da Vortex Software. Não é comprovante de instituição real. A liquidação ocorreu no livro-razão do simulador.")
            }
        }
    }
}

@Composable
private fun ReadOnly(label: String, value: String) {
    Column(Modifier.padding(top = 8.dp)) {
        Text(label, fontWeight = FontWeight.Bold, fontSize = 13.sp)
        Text(
            value,
            modifier = Modifier.fillMaxWidth().padding(top = 4.dp).clip(RoundedCornerShape(12.dp)).background(Field).padding(14.dp),
        )
    }
}

@Composable
private fun AmountField(label: String, value: String, keyboard: KeyboardType = KeyboardType.Text, onChange: (String) -> Unit) {
    Column(Modifier.fillMaxWidth().padding(vertical = 4.dp)) {
        Text(label, fontWeight = FontWeight.Bold, fontSize = 13.sp)
        OutlinedTextField(
            value = value,
            onValueChange = onChange,
            modifier = Modifier.fillMaxWidth(),
            singleLine = true,
            keyboardOptions = KeyboardOptions(keyboardType = keyboard),
            shape = RoundedCornerShape(12.dp),
            colors = fieldColors(),
        )
    }
}

@Composable
private fun PairRow(left: String, right: String) {
    Row(Modifier.fillMaxWidth().padding(vertical = 6.dp), horizontalArrangement = Arrangement.SpaceBetween) {
        Text(left, modifier = Modifier.weight(1f), color = Muted)
        Text(right, fontWeight = FontWeight.Bold)
    }
}

private fun otherCpf(own: String?): String {
    val digits = own.orEmpty().filter(Char::isDigit)
    val other = com.vortexsoftware.vortexbank.data.Holders.all.firstOrNull { it.cpf.filter(Char::isDigit) != digits }
    return (other ?: com.vortexsoftware.vortexbank.data.Holders.all.first()).cpf.filter(Char::isDigit)
}
