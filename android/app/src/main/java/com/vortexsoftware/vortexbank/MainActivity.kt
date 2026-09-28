package com.vortexsoftware.vortexbank

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.lifecycle.viewmodel.compose.viewModel
import com.vortexsoftware.vortexbank.ui.BankModel
import com.vortexsoftware.vortexbank.ui.VortexRoot
import com.vortexsoftware.vortexbank.ui.VortexTheme

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        val app = application as VortexApp
        val prefs = getSharedPreferences("vortex", MODE_PRIVATE)
        setContent {
            val model = viewModel { BankModel(app.bank, prefs) }
            VortexTheme(model.fontStep) {
                VortexRoot(model)
            }
        }
    }
}
