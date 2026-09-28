package com.vortexsoftware.vortexbank.data

object Endpoints {
    const val CLOUD_PRODUCTION = "https://bank.vortexsoftware.tech"
    const val EMULATOR_HOST = "10.0.2.2"
    const val DEVICE_HOST = "127.0.0.1"

    @Volatile
    var useCloudProduction: Boolean = true

    @Volatile
    var host: String = EMULATOR_HOST

    val AUTH: String
        get() = if (useCloudProduction) "$CLOUD_PRODUCTION/api/auth" else "http://$host:5081"

    val TRANSACTIONS: String
        get() = if (useCloudProduction) "$CLOUD_PRODUCTION/api/transactions" else "http://$host:5082"

    val SWAGGER: String
        get() = if (useCloudProduction) "$CLOUD_PRODUCTION/swagger" else "http://$host:8080/swagger"
}
