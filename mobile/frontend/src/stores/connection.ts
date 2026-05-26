import { defineStore } from 'pinia'
import { ref } from 'vue'

/**
 * 移动端 WebSocket 连接管理
 * 通讯链路：移动端 JSAPI → 原生壳子 → WebSocket → 桌面端
 * 前端零网络原则：前端通过原生壳子的 WebSocket 连接桌面端
 */
export const useConnectionStore = defineStore('connection', () => {
  const connected = ref(false)
  const frozen = ref(false)
  const deviceId = ref('')
  const desktopUrl = ref('')
  const reconnectAttempts = ref(0)
  const maxReconnectDelay = 30000

  // 断连日志环形缓冲区
  const logBuffer = ref<Array<{
    level: string
    message: string
    source: string
    timestamp: number
    stackTrace?: string
  }>>([])
  const LOG_BUFFER_MAX = 1000

  let ws: WebSocket | null = null
  let reconnectTimer: ReturnType<typeof setTimeout> | null = null

  function connect(url?: string) {
    if (url) desktopUrl.value = url
    if (!desktopUrl.value) return

    if (ws) ws.close()

    ws = new WebSocket(desktopUrl.value)

    ws.onopen = () => {
      connected.value = true
      frozen.value = false
      reconnectAttempts.value = 0

      // 注册设备
      send({
        type: 'device_register',
        deviceId: deviceId.value,
        payload: {
          deviceName: getDeviceName(),
          platform: getPlatform(),
          osVersion: getOsVersion(),
          appId: 'com.onedeck.mobile',
          screenResolution: getScreenResolution(),
        },
      })

      // 补传断连期间的日志
      if (logBuffer.value.length > 0) {
        send({
          type: 'log_batch_transfer',
          deviceId: deviceId.value,
          payload: { logs: logBuffer.value.splice(0) },
        })
      }
    }

    ws.onclose = () => {
      connected.value = false
      frozen.value = true
      scheduleReconnect()
    }

    ws.onerror = () => {
      // 错误处理
    }

    ws.onmessage = (event) => {
      try {
        const message = JSON.parse(event.data)
        handleMessage(message)
      } catch (e) {
        console.error('[OneDeck] Failed to parse message:', e)
      }
    }
  }

  function disconnect() {
    if (reconnectTimer) {
      clearTimeout(reconnectTimer)
      reconnectTimer = null
    }
    ws?.close()
    ws = null
    connected.value = false
  }

  function send(message: Record<string, unknown>) {
    if (ws && ws.readyState === WebSocket.OPEN) {
      ws.send(JSON.stringify(message))
    }
  }

  function scheduleReconnect() {
    if (reconnectTimer) return

    const delay = Math.min(1000 * Math.pow(2, reconnectAttempts.value), maxReconnectDelay)
    reconnectAttempts.value++

    reconnectTimer = setTimeout(() => {
      reconnectTimer = null
      connect()
    }, delay)
  }

  function handleMessage(message: Record<string, unknown>) {
    const { useSchemeStore } = require('./scheme')
    const schemeStore = useSchemeStore()

    switch (message.type) {
      case 'scheme_push':
        schemeStore.loadScheme(message.payload as any)
        break
      case 'module_push':
        schemeStore.loadModule(message.payload as any)
        break
      case 'style_push':
        schemeStore.loadStyle(message.payload as any)
        break
      case 'state_sync':
        schemeStore.updatePluginState(
          (message.payload as any).pluginId,
          (message.payload as any).instanceId,
          (message.payload as any).state
        )
        break
      case 'command':
        handleCommand(message.payload as any)
        break
    }
  }

  function handleCommand(payload: { command: string; data?: Record<string, unknown> }) {
    switch (payload.command) {
      case 'freeze':
        frozen.value = true
        break
      case 'unfreeze':
        frozen.value = false
        break
      case 'reload_scheme':
        // 重新加载方案
        break
    }
  }

  /**
   * 记录日志（断连时缓存到环形缓冲区）
   */
  function log(level: string, source: string, message: string, stackTrace?: string) {
    const entry = { level, message, source, timestamp: Date.now(), stackTrace }

    if (connected.value) {
      send({
        type: 'log_transfer',
        deviceId: deviceId.value,
        payload: entry,
      })
    } else {
      // 缓存到环形缓冲区
      if (logBuffer.value.length >= LOG_BUFFER_MAX) {
        logBuffer.value.shift()
      }
      logBuffer.value.push(entry)
    }
  }

  // 辅助方法（实际由原生层提供）
  function getDeviceName(): string {
    return window.OneDeckNative?.getDeviceId?.() ?? 'Unknown Device'
  }
  function getPlatform(): string {
    return window.OneDeckNative?.getPlatform?.() ?? 'unknown'
  }
  function getOsVersion(): string {
    return '1.0.0'
  }
  function getScreenResolution(): { width: number; height: number } {
    return { width: window.innerWidth, height: window.innerHeight }
  }

  return {
    connected,
    frozen,
    deviceId,
    desktopUrl,
    reconnectAttempts,
    logBuffer,
    connect,
    disconnect,
    send,
    log,
  }
})
