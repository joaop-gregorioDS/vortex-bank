package com.vortexsoftware.vortexbank.ui

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.interaction.MutableInteractionSource
import androidx.compose.foundation.interaction.collectIsPressedAsState
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.ColumnScope
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.shadow
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.foundation.Image
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.vortexsoftware.vortexbank.R
import com.vortexsoftware.vortexbank.data.initials

private val CardShape = RoundedCornerShape(18.dp)
private val ButtonShape = RoundedCornerShape(14.dp)
val Band = Brush.horizontalGradient(listOf(RoseSoft, Rose, Wine))
val PlasticBrush = Brush.linearGradient(listOf(Color(0xFF4C0519), Wine, Rose))
val DebitBrush = Brush.linearGradient(listOf(Color(0xFF500724), Color(0xFF9D174D), Color(0xFFBE185D)))

@Composable
fun SimBanner() {
    Text(
        "Ambiente simulado. Nenhum valor é real.",
        modifier = Modifier.fillMaxWidth().background(Band).padding(vertical = 8.dp, horizontal = 16.dp),
        color = Color.White,
        fontWeight = FontWeight.ExtraBold,
        fontSize = 12.sp,
        textAlign = TextAlign.Center,
    )
}

@Composable
fun Panel(modifier: Modifier = Modifier, content: @Composable ColumnScope.() -> Unit) {
    Column(
        modifier
            .fillMaxWidth()
            .shadow(8.dp, CardShape)
            .background(Color.White, CardShape)
            .padding(16.dp),
        content = content,
    )
}

@Composable
fun Pill(text: String, live: Boolean = false, modifier: Modifier = Modifier) {
    Text(
        text,
        modifier = modifier
            .background(if (live) Color(0xFFE8F6EE) else Color.White, CircleShape)
            .padding(horizontal = 10.dp, vertical = 4.dp),
        color = if (live) Credit else Muted,
        fontSize = 11.sp,
        fontWeight = FontWeight.Bold,
    )
}

@Composable
fun PrimaryButton(text: String, enabled: Boolean = true, onClick: () -> Unit) {
    val interaction = remember { MutableInteractionSource() }
    val pressed by interaction.collectIsPressedAsState()
    Box(
        Modifier
            .fillMaxWidth()
            .height(48.dp)
            .clip(ButtonShape)
            .background(if (!enabled) Rose.copy(alpha = 0.45f) else if (pressed) RosePressed else Rose)
            .clickable(interaction, null, enabled = enabled, onClick = onClick),
        contentAlignment = Alignment.Center,
    ) {
        Text(text, color = Color.White, fontWeight = FontWeight.Bold)
    }
}

@Composable
fun GhostButton(text: String, modifier: Modifier = Modifier, onClick: () -> Unit) {
    val interaction = remember { MutableInteractionSource() }
    val pressed by interaction.collectIsPressedAsState()
    Box(
        modifier
            .height(44.dp)
            .clip(RoundedCornerShape(12.dp))
            .background(if (pressed) Rose else Color.White)
            .clickable(interaction, null, onClick = onClick)
            .padding(horizontal = 14.dp),
        contentAlignment = Alignment.Center,
    ) {
        Text(text, color = if (pressed) Color.White else Ink, fontWeight = FontWeight.Bold)
    }
}

@Composable
fun LeaveButton(onClick: () -> Unit) {
    val interaction = remember { MutableInteractionSource() }
    val pressed by interaction.collectIsPressedAsState()
    Box(
        Modifier
            .fillMaxWidth()
            .height(48.dp)
            .clip(ButtonShape)
            .background(if (pressed) Rose else Color.White)
            .clickable(interaction, null, onClick = onClick),
        contentAlignment = Alignment.Center,
    ) {
        Text("Sair", color = if (pressed) Color.White else Rose, fontWeight = FontWeight.Bold)
    }
}

@Composable
fun Chip(text: String, selected: Boolean = false, onClick: () -> Unit) {
    Text(
        text,
        modifier = Modifier
            .clip(RoundedCornerShape(12.dp))
            .background(if (selected) Blush else Field)
            .clickable(onClick = onClick)
            .padding(horizontal = 12.dp, vertical = 10.dp),
        color = if (selected) Rose else Ink,
        fontWeight = FontWeight.Bold,
    )
}

@Composable
fun HubCard(title: String, detail: String, mark: String, warn: Boolean = false, onClick: () -> Unit) {
    Row(
        Modifier
            .fillMaxWidth()
            .clip(RoundedCornerShape(16.dp))
            .background(if (warn) Color(0xFFFFF5F7) else Color.White)
            .clickable(onClick = onClick)
            .padding(14.dp),
        horizontalArrangement = Arrangement.spacedBy(12.dp),
    ) {
        Box(
            Modifier.size(36.dp).clip(RoundedCornerShape(12.dp)).background(Blush),
            contentAlignment = Alignment.Center,
        ) {
            Text(mark, color = Rose, fontWeight = FontWeight.Bold)
        }
        Column(Modifier.weight(1f)) {
            Text(title, fontWeight = FontWeight.Bold)
            Text(detail, color = Muted, fontSize = 13.sp)
        }
    }
}

@Composable
fun Avatar(name: String, large: Boolean = false) {
    val side = if (large) 48.dp else 36.dp
    Box(
        Modifier.size(side).clip(CircleShape).background(if (large) Band else Brush.linearGradient(listOf(Color(0xFF3F2A31), Color(0xFF3F2A31)))),
        contentAlignment = Alignment.Center,
    ) {
        Text(initials(name), color = Color.White, fontWeight = FontWeight.ExtraBold, fontSize = if (large) 16.sp else 12.sp)
    }
}

@Composable
fun ShieldBox(modifier: Modifier = Modifier) {
    Box(
        modifier.shadow(8.dp, RoundedCornerShape(18.dp)).size(64.dp).clip(RoundedCornerShape(18.dp)).background(Band),
        contentAlignment = Alignment.Center,
    ) {
        Image(painterResource(R.drawable.ic_shield), contentDescription = "Escudo Vortex", modifier = Modifier.size(32.dp, 38.dp))
    }
}

@Composable
fun Note(text: String) {
    Text(
        text,
        modifier = Modifier.fillMaxWidth().clip(RoundedCornerShape(14.dp)).background(Color(0xFFFFF7F8)).padding(12.dp),
        color = Ink,
        fontSize = 13.sp,
    )
}

@Composable
fun SectionLabel(text: String) {
    Text(text, color = Muted, fontWeight = FontWeight.ExtraBold, fontSize = 12.sp, letterSpacing = 1.sp, modifier = Modifier.padding(top = 8.dp))
}

@Composable
fun BottomBar(tab: Tab, onSelect: (Tab) -> Unit) {
    val items = listOf(
        Tab.Home to "Início",
        Tab.Pix to "Pix",
        Tab.Statement to "Extrato",
        Tab.Cards to "Cartões",
        Tab.More to "Mais",
    )
    Row(
        Modifier.fillMaxWidth().background(Color.White).padding(horizontal = 4.dp, vertical = 6.dp),
        horizontalArrangement = Arrangement.spacedBy(4.dp),
    ) {
        items.forEach { (item, label) ->
            val interaction = remember(item) { MutableInteractionSource() }
            val pressed by interaction.collectIsPressedAsState()
            val active = tab == item || pressed
            Box(
                Modifier
                    .weight(1f)
                    .height(48.dp)
                    .clip(RoundedCornerShape(12.dp))
                    .background(if (active) Rose else Color.Transparent)
                    .clickable(interaction, null) { onSelect(item) },
                contentAlignment = Alignment.Center,
            ) {
                Text(label, color = if (active) Color.White else Ink, fontWeight = FontWeight.Bold, fontSize = 12.sp, textAlign = TextAlign.Center)
            }
        }
    }
}
