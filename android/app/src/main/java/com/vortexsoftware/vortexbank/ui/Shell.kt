package com.vortexsoftware.vortexbank.ui

import android.content.ActivityNotFoundException
import android.content.Intent
import android.net.Uri
import androidx.activity.compose.BackHandler
import androidx.compose.foundation.background
import androidx.compose.foundation.horizontalScroll
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.WindowInsets
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.safeDrawing
import androidx.compose.foundation.layout.windowInsetsPadding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.vortexsoftware.vortexbank.data.Endpoints
import com.vortexsoftware.vortexbank.data.Profile
import kotlinx.coroutines.delay

@Composable
fun VortexRoot(model: BankModel) {
    val profile = model.profile
    if (profile == null) LoginScreen(model) else BankShell(model, profile)
}

@Composable
private fun BankShell(model: BankModel, profile: Profile) {
    val context = LocalContext.current
    val page = model.page
    val nested = page is Page.Receipt || page == Page.Payments || page == Page.Invest || page == Page.Profile
    BackHandler(enabled = nested) { model.back() }
    LaunchedEffect(profile.userId) {
        while (true) {
            model.refreshLedger()
            delay(15_000)
        }
    }
    Column(
        Modifier.fillMaxSize().background(Canvas).windowInsetsPadding(WindowInsets.safeDrawing),
    ) {
        SimBanner()
        Column(Modifier.fillMaxWidth().padding(horizontal = 16.dp, vertical = 8.dp)) {
            Text(titleOf(page), color = Muted, fontWeight = FontWeight.Bold, fontSize = 13.sp)
            Row(
                Modifier.fillMaxWidth().horizontalScroll(rememberScrollState()).padding(top = 6.dp),
                horizontalArrangement = Arrangement.spacedBy(8.dp),
                verticalAlignment = Alignment.CenterVertically,
            ) {
                Pill(if (model.ledger) "Ledger conectado" else "Ledger indisponível", live = model.ledger)
                GhostButton("A+ Fonte") { model.cycleFont() }
                GhostButton("Swagger") {
                    try {
                        context.startActivity(Intent(Intent.ACTION_VIEW, Uri.parse(Endpoints.SWAGGER)))
                    } catch (_: ActivityNotFoundException) {
                        model.error = "Não foi possível abrir o navegador."
                    }
                }
            }
        }
        if (model.error.isNotBlank()) {
            Text(model.error, color = RosePressed, fontWeight = FontWeight.Bold, modifier = Modifier.padding(horizontal = 16.dp, vertical = 4.dp))
        }
        Box(Modifier.weight(1f).fillMaxWidth()) {
            when (page) {
                Page.Home -> HomeScreen(model, profile.name)
                Page.Pix -> PixScreen(model)
                Page.Statement -> StatementScreen(model, profile)
                Page.Cards -> CardsScreen(model)
                Page.More -> MoreScreen(model)
                Page.Payments -> PaymentsScreen(model)
                Page.Invest -> InvestScreen()
                Page.Profile -> ProfileScreen(profile, model.home)
                is Page.Receipt -> ReceiptScreen(model, page.journalId)
            }
        }
        BottomBar(model.tab) { model.open(it) }
    }
}

private fun titleOf(page: Page): String = when (page) {
    Page.Home -> "Início · Dashboard"
    Page.Pix -> "Início / Central Pix"
    Page.Statement -> "Extrato · Ledger"
    Page.Cards -> "Gestão de cartões"
    Page.Payments -> "Início / Central de Pagamentos"
    Page.Invest -> "Início / Vortex Invest"
    Page.Profile -> "Início / Meu perfil e ajustes"
    Page.More -> "Mais áreas"
    is Page.Receipt -> "Comprovante"
}
