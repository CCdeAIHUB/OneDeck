package ai.onedeck.mobile

import android.app.Application

class OneDeckApplication : Application() {
    override fun onCreate() {
        super.onCreate()
        instance = this
    }

    companion object {
        lateinit var instance: OneDeckApplication
            private set
    }
}
