package ai.onedeck.mobile

import org.java_websocket.client.WebSocketClient
import org.java_websocket.handshake.ServerHandshake
import java.net.URI

/**
 * WebSocket 客户端
 * 管理与桌面端的 WebSocket 连接
 * 通讯链路：移动端原生壳子 ↔ 桌面端 C# 壳子
 *
 * 重连策略：指数退避（1s→2s→4s→8s→16s→30s）
 */
class WebSocketClient(
    private val onMessage: (String) -> Unit,
    private val onConnected: () -> Unit,
    private val onDisconnected: (String) -> Unit
) {
    private var ws: WebSocketClient? = null
    private var reconnectAttempts = 0
    private val maxReconnectDelay = 30_000L
    private var shouldReconnect = true
    private var serverUrl = "ws://192.168.1.100:9720/mobile"

    private inner class InnerWebSocket(uri: URI) : WebSocketClient(uri) {
        override fun onOpen(handshakedata: ServerHandshake?) {
            reconnectAttempts = 0
            onConnected()
        }

        override fun onMessage(message: String?) {
            message?.let { onMessage(it) }
        }

        override fun onClose(code: Int, reason: String?, remote: Boolean) {
            onDisconnected(reason ?: "closed")
            if (shouldReconnect) {
                scheduleReconnect()
            }
        }

        override fun onError(ex: Exception?) {
            // 错误处理
        }
    }

    fun connect(url: String? = null) {
        url?.let { serverUrl = it }
        shouldReconnect = true

        try {
            ws?.close()
            ws = InnerWebSocket(URI(serverUrl))
            ws?.connect()
        } catch (e: Exception) {
            scheduleReconnect()
        }
    }

    fun disconnect() {
        shouldReconnect = false
        ws?.close()
        ws = null
    }

    fun send(message: String) {
        ws?.send(message)
    }

    fun isConnected(): Boolean = ws?.isOpen == true

    private fun scheduleReconnect() {
        val delay = minOf(1000L * (1 shl reconnectAttempts), maxReconnectDelay)
        reconnectAttempts++

        Thread {
            Thread.sleep(delay)
            if (shouldReconnect) {
                try {
                    ws?.close()
                    ws = InnerWebSocket(URI(serverUrl))
                    ws?.connect()
                } catch (e: Exception) {
                    scheduleReconnect()
                }
            }
        }.start()
    }
}
