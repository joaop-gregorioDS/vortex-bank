package com.vortexsoftware.vortexbank

import android.app.Application
import android.os.Build
import com.vortexsoftware.vortexbank.data.BankClient
import com.vortexsoftware.vortexbank.data.Endpoints
import okhttp3.OkHttpClient
import java.util.concurrent.TimeUnit

class VortexApp : Application() {
    lateinit var bank: BankClient
        private set

    override fun onCreate() {
        super.onCreate()
        val http = OkHttpClient.Builder()
            .callTimeout(20, TimeUnit.SECONDS)
            .connectTimeout(20, TimeUnit.SECONDS)
            .readTimeout(20, TimeUnit.SECONDS)
            .build()

        // Conexão padrão com a nuvem de produção Vortex Bank (HTTPS)
        Endpoints.useCloudProduction = true
        Endpoints.host = if (runningOnEmulator()) Endpoints.EMULATOR_HOST else Endpoints.DEVICE_HOST
        bank = BankClient(http)
    }

    private fun runningOnEmulator(): Boolean {
        val fingerprint = Build.FINGERPRINT.orEmpty()
        val model = Build.MODEL.orEmpty()
        val hardware = Build.HARDWARE.orEmpty()
        val product = Build.PRODUCT.orEmpty()
        return fingerprint.startsWith("generic")
            || fingerprint.startsWith("unknown")
            || fingerprint.contains("emulator", ignoreCase = true)
            || model.contains("google_sdk", ignoreCase = true)
            || model.contains("Emulator", ignoreCase = true)
            || model.contains("Android SDK built for", ignoreCase = true)
            || hardware.contains("goldfish", ignoreCase = true)
            || hardware.contains("ranchu", ignoreCase = true)
            || product.contains("sdk", ignoreCase = true)
            || Build.MANUFACTURER.contains("Genymotion", ignoreCase = true)
    }
}
