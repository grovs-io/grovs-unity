package io.grovs.unity

import android.content.Context
import android.os.Bundle

data class GrovsUnityConfig(
    val apiKey: String?,
    val useTestEnvironment: Boolean,
    val baseUrl: String?,
    val startEnabled: Boolean,
    val clipboardDomains: List<String>?,
    val logLevel: Int,
    val trackScenes: Boolean
) {
    companion object {
        private const val LOG_ERROR = 2

        fun fromMetaData(bundle: Bundle?): GrovsUnityConfig {
            if (bundle == null) {
                return GrovsUnityConfig(null, false, null, true, null, LOG_ERROR, false)
            }
            val domains = text(bundle, "grovs_clipboard_domains")
                ?.split(",")
                ?.map { it.trim() }
                ?.filter { it.isNotEmpty() }
                ?.takeIf { it.isNotEmpty() }
            return GrovsUnityConfig(
                apiKey = text(bundle, "grovs_api_key"),
                useTestEnvironment = flag(bundle, "grovs_use_test_environment", false),
                baseUrl = text(bundle, "grovs_base_url"),
                startEnabled = flag(bundle, "grovs_enabled", true),
                clipboardDomains = domains,
                logLevel = logLevel(bundle),
                trackScenes = flag(bundle, "grovs_unity_track_scenes", false)
            )
        }

        @Suppress("DEPRECATION")
        private fun text(bundle: Bundle, key: String): String? =
            bundle.get(key)?.toString()?.trim()?.takeIf { it.isNotEmpty() }

        private fun flag(bundle: Bundle, key: String, default: Boolean): Boolean =
            when (text(bundle, key)?.lowercase()) {
                "true" -> true
                "false" -> false
                else -> default
            }

        private fun logLevel(bundle: Bundle): Int =
            when (val raw = text(bundle, "grovs_log_level")?.lowercase()) {
                "info", "0" -> 0
                "warn", "warning", "1" -> 1
                "error", "2" -> 2
                else -> raw?.toIntOrNull()?.coerceIn(0, 2) ?: LOG_ERROR
            }
    }
}

object GrovsUnityConsent {
    private const val PREFS = "grovs_unity"
    private const val KEY = "enabled"

    fun stored(ctx: Context): Boolean? {
        val prefs = ctx.getSharedPreferences(PREFS, Context.MODE_PRIVATE)
        return if (prefs.contains(KEY)) prefs.getBoolean(KEY, true) else null
    }

    fun store(ctx: Context, enabled: Boolean) {
        ctx.getSharedPreferences(PREFS, Context.MODE_PRIVATE).edit().putBoolean(KEY, enabled).apply()
    }
}
