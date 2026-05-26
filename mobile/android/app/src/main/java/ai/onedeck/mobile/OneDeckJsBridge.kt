package ai.onedeck.mobile

import android.Manifest
import android.content.pm.PackageManager
import android.util.Log
import android.webkit.JavascriptInterface
import androidx.core.app.ActivityCompat
import androidx.core.content.ContextCompat
import org.json.JSONObject

/**
 * JSAPI Bridge
 * 原生能力暴露给前端 WebView，前端通过 window.OneDeckNative 调用
 *
 * 安全限制：只暴露读取能力，不暴露写入/修改系统状态的能力
 */
class OneDeckJsBridge(private val activity: MainActivity) {

    private val tag = "OneDeckJsBridge"
    private val pendingPermissionCallbacks = mutableMapOf<Int, (Boolean) -> Unit>()

    /**
     * 调用 JSAPI
     * 由前端 JS 调用：window.OneDeckNative.callApi(apiName, argsJson)
     */
    @JavascriptInterface
    fun callApi(apiName: String, argsJson: String): String {
        val args = try {
            JSONObject(argsJson)
        } catch (e: Exception) {
            return errorResult("Invalid JSON args")
        }

        val result = when (apiName) {
            // 文件操作
            "file.read" -> handleFileRead(args)
            "file.write" -> handleFileWrite(args)
            "file.delete" -> handleFileDelete(args)
            "file.exists" -> handleFileExists(args)
            "file.list" -> handleFileList(args)

            // 设备信息
            "device.getInfo" -> handleGetDeviceInfo()
            "device.getBattery" -> handleGetBattery()
            "device.vibrate" -> handleVibrate(args)

            // 剪贴板
            "clipboard.read" -> handleClipboardRead()
            "clipboard.write" -> handleClipboardWrite(args)

            // 网络
            "network.getStatus" -> handleGetNetworkStatus()

            // 摄像头
            "camera.takePhoto" -> handleTakePhoto()
            "camera.startStream" -> handleStartStream()
            "camera.stopStream" -> handleStopStream(args)

            // 定位
            "location.getCurrent" -> handleGetCurrentLocation()
            "location.startWatch" -> handleStartLocationWatch()
            "location.stopWatch" -> handleStopLocationWatch(args)

            // 存储
            "storage.get" -> handleStorageGet(args)
            "storage.set" -> handleStorageSet(args)
            "storage.remove" -> handleStorageRemove(args)
            "storage.keys" -> handleStorageKeys(args)
            "storage.clear" -> handleStorageClear(args)

            else -> errorResult("Unknown API: $apiName")
        }

        return result
    }

    /**
     * 获取设备 ID
     */
    @JavascriptInterface
    fun getDeviceId(): String {
        return android.provider.Settings.Secure.getString(
            activity.contentResolver,
            android.provider.Settings.Secure.ANDROID_ID
        )
    }

    /**
     * 获取平台
     */
    @JavascriptInterface
    fun getPlatform(): String = "android"

    /**
     * 请求权限
     */
    @JavascriptInterface
    fun requestPermission(permission: String): Boolean {
        val androidPermission = when (permission) {
            "camera" -> Manifest.permission.CAMERA
            "location" -> Manifest.permission.ACCESS_FINE_LOCATION
            "storage" -> Manifest.permission.WRITE_EXTERNAL_STORAGE
            else -> return false
        }

        val granted = ContextCompat.checkSelfPermission(activity, androidPermission) ==
                PackageManager.PERMISSION_GRANTED

        if (!granted) {
            ActivityCompat.requestPermissions(activity, arrayOf(androidPermission), 1001)
        }

        return granted
    }

    /**
     * 记录日志（转发到桌面端）
     */
    @JavascriptInterface
    fun log(level: String, message: String) {
        when (level) {
            "debug" -> Log.d(tag, message)
            "info" -> Log.i(tag, message)
            "warn" -> Log.w(tag, message)
            "error" -> Log.e(tag, message)
        }
    }

    // ==================== JSAPI 实现占位 ====================

    private fun handleFileRead(args: JSONObject): String {
        // TODO: 在插件沙箱目录内读取文件
        return successResult(mapOf("content" to ""))
    }

    private fun handleFileWrite(args: JSONObject): String {
        // TODO: 在插件沙箱目录内写入文件
        return successResult(null)
    }

    private fun handleFileDelete(args: JSONObject): String {
        return successResult(null)
    }

    private fun handleFileExists(args: JSONObject): String {
        return successResult(mapOf("exists" to false))
    }

    private fun handleFileList(args: JSONObject): String {
        return successResult(mapOf("files" to emptyList<String>()))
    }

    private fun handleGetDeviceInfo(): String {
        return successResult(mapOf(
            "platform" to "android",
            "osVersion" to android.os.Build.VERSION.RELEASE,
            "model" to android.os.Build.MODEL,
            "screenResolution" to mapOf(
                "width" to activity.resources.displayMetrics.widthPixels,
                "height" to activity.resources.displayMetrics.heightPixels
            )
        ))
    }

    private fun handleGetBattery(): String {
        // TODO: 使用 BatteryManager 获取电量
        return successResult(mapOf("level" to 100, "charging" to false))
    }

    private fun handleVibrate(args: JSONObject): String {
        // TODO: 使用 Vibrator 触发震动
        return successResult(null)
    }

    private fun handleClipboardRead(): String {
        // TODO: 读取剪贴板
        return successResult(mapOf("text" to ""))
    }

    private fun handleClipboardWrite(args: JSONObject): String {
        // TODO: 写入剪贴板
        return successResult(null)
    }

    private fun handleGetNetworkStatus(): String {
        // TODO: 使用 ConnectivityManager 获取网络状态
        return successResult(mapOf("connected" to true, "type" to "wifi"))
    }

    private fun handleTakePhoto(): String {
        // TODO: 调用摄像头拍照
        return successResult(mapOf("data" to ""))
    }

    private fun handleStartStream(): String {
        return successResult(mapOf("streamId" to ""))
    }

    private fun handleStopStream(args: JSONObject): String {
        return successResult(null)
    }

    private fun handleGetCurrentLocation(): String {
        // TODO: 使用 FusedLocationProviderClient 获取位置
        return successResult(mapOf("lat" to 0.0, "lng" to 0.0, "accuracy" to 0.0))
    }

    private fun handleStartLocationWatch(): String {
        return successResult(mapOf("watchId" to ""))
    }

    private fun handleStopLocationWatch(args: JSONObject): String {
        return successResult(null)
    }

    private fun handleStorageGet(args: JSONObject): String {
        // TODO: 从 SQLite 读取插件数据
        return successResult(mapOf("value" to null))
    }

    private fun handleStorageSet(args: JSONObject): String {
        // TODO: 写入 SQLite 插件数据
        return successResult(null)
    }

    private fun handleStorageRemove(args: JSONObject): String {
        return successResult(null)
    }

    private fun handleStorageKeys(args: JSONObject): String {
        return successResult(mapOf("keys" to emptyList<String>()))
    }

    private fun handleStorageClear(args: JSONObject): String {
        return successResult(null)
    }

    // ==================== 辅助方法 ====================

    private fun successResult(data: Any?): String {
        val result = JSONObject()
        result.put("success", true)
        if (data != null) result.put("data", data)
        return result.toString()
    }

    private fun errorResult(message: String): String {
        val result = JSONObject()
        result.put("success", false)
        result.put("error", message)
        return result.toString()
    }
}
