package com.vortexsoftware.vortexbank.ui

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.BoxWithConstraints
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.RowScope
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.WindowInsets
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.imePadding
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.safeDrawing
import androidx.compose.foundation.layout.widthIn
import androidx.compose.foundation.layout.windowInsetsPadding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.OutlinedTextFieldDefaults
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.vortexsoftware.vortexbank.data.Holders

@Composable
fun LoginScreen(model: BankModel) {
    var cpf by rememberSaveable { mutableStateOf("") }
    var password by rememberSaveable { mutableStateOf("") }
    Column(
        Modifier
            .fillMaxSize()
            .background(Brush.verticalGradient(listOf(Color(0xFFF8D5DF), Color(0xFFF6F1F3), Canvas)))
            .windowInsetsPadding(WindowInsets.safeDrawing)
            .imePadding(),
    ) {
        SimBanner()
        BoxWithConstraints(Modifier.weight(1f).fillMaxWidth()) {
            Column(
                Modifier
                    .heightIn(min = maxHeight)
                    .verticalScroll(rememberScrollState())
                    .padding(20.dp)
                    .fillMaxWidth(),
                horizontalAlignment = Alignment.CenterHorizontally,
                verticalArrangement = Arrangement.Center,
            ) {
                Panel(Modifier.widthIn(max = 440.dp)) {
                    Column(Modifier.fillMaxWidth(), horizontalAlignment = Alignment.CenterHorizontally) {
                        ShieldBox()
                        Spacer(Modifier.height(8.dp))
                        Row {
                            Text("Vortex", fontSize = 32.sp, fontWeight = FontWeight.ExtraBold)
                            Text("Bank", fontSize = 32.sp, fontWeight = FontWeight.ExtraBold, color = Rose)
                        }
                        Text(
                            "Banco de varejo e ledger de partidas dobradas",
                            color = Muted,
                            fontSize = 13.sp,
                            textAlign = TextAlign.Center,
                        )
                        Spacer(Modifier.height(8.dp))
                        Text(
                            "PORTFÓLIO / LABORATÓRIO DE DEMONSTRAÇÃO",
                            modifier = Modifier
                                .clip(CircleShape)
                                .background(Blush)
                                .padding(horizontal = 10.dp, vertical = 4.dp),
                            color = Wine,
                            fontSize = 10.sp,
                            fontWeight = FontWeight.ExtraBold,
                        )
                        Spacer(Modifier.height(16.dp))
                        Text("Contas de um clique", color = Muted, modifier = Modifier.align(Alignment.Start))
                        Spacer(Modifier.height(8.dp))
                        Holders.all.forEach { person ->
                            Row(
                                Modifier
                                    .fillMaxWidth()
                                    .padding(bottom = 8.dp)
                                    .clip(CircleShape)
                                    .background(Color.White)
                                    .clickable(enabled = !model.busy) { model.enterHolder(person) }
                                    .padding(horizontal = 8.dp, vertical = 6.dp),
                                verticalAlignment = Alignment.CenterVertically,
                                horizontalArrangement = Arrangement.spacedBy(8.dp),
                            ) {
                                Avatar(person.name)
                                Column {
                                    Text(person.name, fontWeight = FontWeight.Bold, fontSize = 14.sp)
                                    Text(person.cpf, color = Muted, fontSize = 12.sp)
                                }
                            }
                        }
                        Row(verticalAlignment = Alignment.CenterVertically, modifier = Modifier.fillMaxWidth().padding(vertical = 6.dp)) {
                            BoxLine()
                            Text("  OU CPF E SENHA  ", color = Muted, fontSize = 11.sp, fontWeight = FontWeight.Bold)
                            BoxLine()
                        }
                        Field("CPF do titular", cpf, { cpf = it }, keyboard = KeyboardType.Number, placeholder = "000.000.000-00")
                        Field("Senha de acesso", password, { password = it }, secret = true, placeholder = "••••••••")
                        if (model.error.isNotBlank()) {
                            Text(model.error, color = RosePressed, fontWeight = FontWeight.Bold, modifier = Modifier.padding(bottom = 8.dp))
                        }
                        PrimaryButton(
                            text = if (model.busy) "Entrando…" else "Acessar conta segura →",
                            enabled = !model.busy,
                        ) { model.enterByCpf(cpf, password) }
                        Spacer(Modifier.height(12.dp))
                        Text("versão 1.0 · Vortex Software", color = Muted, fontSize = 12.sp)
                    }
                }
            }
        }
    }
}

@Composable
private fun RowScope.BoxLine() {
    Spacer(Modifier.weight(1f).height(1.dp).background(Line))
}

@Composable
private fun Field(
    label: String,
    value: String,
    onChange: (String) -> Unit,
    secret: Boolean = false,
    keyboard: KeyboardType = KeyboardType.Text,
    placeholder: String = "",
) {
    Column(Modifier.fillMaxWidth().padding(bottom = 10.dp)) {
        Text(label, fontWeight = FontWeight.Bold, fontSize = 13.sp)
        Spacer(Modifier.height(4.dp))
        OutlinedTextField(
            value = value,
            onValueChange = onChange,
            modifier = Modifier.fillMaxWidth(),
            singleLine = true,
            placeholder = { Text(placeholder, color = Muted) },
            visualTransformation = if (secret) PasswordVisualTransformation() else androidx.compose.ui.text.input.VisualTransformation.None,
            keyboardOptions = KeyboardOptions(keyboardType = keyboard),
            shape = androidx.compose.foundation.shape.RoundedCornerShape(12.dp),
            colors = fieldColors(),
        )
    }
}

@Composable
fun fieldColors() = OutlinedTextFieldDefaults.colors(
    focusedBorderColor = Rose,
    unfocusedBorderColor = Color(0xFFF0E4E8),
    focusedContainerColor = Field,
    unfocusedContainerColor = Field,
    focusedTextColor = Ink,
    unfocusedTextColor = Ink,
    cursorColor = Rose,
)
