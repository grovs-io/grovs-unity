package io.grovs.unity

import android.app.Application
import android.content.Context
import android.content.pm.PackageManager
import android.util.Log
import androidx.lifecycle.ProcessLifecycleInitializer
import androidx.startup.Initializer

/** Configures Grovs at process start, before Unity or any activity runs. */
class GrovsUnityInitializer : Initializer<Unit> {
    override fun create(context: Context) {
        try {
            val app = context.applicationContext as Application
            GrovsUnityBridge.start(app, GrovsUnityConfig.fromMetaData(metaData(app)))
        } catch (e: Exception) {
            Log.e("GrovsUnity", "Grovs auto-init failed.", e)
        }
    }

    override fun dependencies(): List<Class<out Initializer<*>>> =
        listOf(ProcessLifecycleInitializer::class.java)

    @Suppress("DEPRECATION")
    private fun metaData(app: Application) =
        app.packageManager.getApplicationInfo(app.packageName, PackageManager.GET_META_DATA).metaData
}
