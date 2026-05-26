package ai.onedeck.mobile

import android.os.Bundle
import android.webkit.WebView
import android.webkit.WebViewClient
import androidx.appcompat.app.AppCompatActivity

class MainActivity : AppCompatActivity() {

    private lateinit var webView: WebView
    private lateinit var jsBridge: OneDeckJsBridge
    private lateinit var wsClient: WebSocketClient

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        // 初始化 WebView
        webView = WebView(this)
        webView.settings.apply {
            javaScriptEnabled = true
            domStorageEnabled = true
            allowFileAccess = true
            mediaPlaybackRequiresUserGesture = false
        }

        // 注入 JSAPI Bridge
        jsBridge = OneDeckJsBridge(this)
        webView.addJavascriptInterface(jsBridge, "OneDeckNative")

        // 设置 WebView 客户端
        webView.webViewClient = object : WebViewClient() {
            override fun onPageFinished(view: WebView?, url: String?) {
                super.onPageFinished(view, url)
                // 页面加载完成后初始化连接
                initWebSocket()
            }
        }

        // 加载前端
        webView.loadUrl("file:///android_asset/frontend/index.html")
        setContentView(webView)
    }

    private fun initWebSocket() {
        wsClient = WebSocketClient(
            onMessage = { message ->
                // 将 WebSocket 消息转发到前端
                runOnUiThread {
                    webView.evaluateJavascript(
                        "window.__onedeck_handleMessage($message)",
                        null
                    )
                }
            },
            onConnected = {
                runOnUiThread {
                    webView.evaluateJavascript(
                        "window.__onedeck_onConnected()",
                        null
                    )
                }
            },
            onDisconnected = { reason ->
                runOnUiThread {
                    webView.evaluateJavascript(
                        "window.__onedeck_onDisconnected('$reason')",
                        null
                    )
                }
            }
        )
        wsClient.connect()
    }

    override fun onDestroy() {
        wsClient.disconnect()
        webView.destroy()
        super.onDestroy()
    }

    override fun onPause() {
        webView.onPause()
        super.onPause()
    }

    override fun onResume() {
        webView.onResume()
        super.onResume()
    }
}
