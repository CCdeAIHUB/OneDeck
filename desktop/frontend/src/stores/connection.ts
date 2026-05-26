import { defineStore } from 'pinia'
import { ref } from 'vue'
import type { LogLevel, LogEntry } from './logs'

/**
 * 桌面端与 C# 后端的 WebSocket 连接
 * 前端零网络原则：前端通过本地 WebSocket 连接 C# 后端，不直接处理网络
 */
export const useConnectionStore = defineStore('connection', () => {
  const ws = ref<WebSocket | null>(null)
  const connected = ref(false)
  const reconnectAttempts = ref(0)
  const maxReconnectDelay = 30000 // 最大重连间隔 30s

  let reconnectTimer: ReturnType<typeof setTimeout> | null = null

  function connect(url = 'ws://localhost:9720/desktop') {
    if (ws.value) {
      ws.value.close()
    }

    const socket = new WebSocket(url)

    socket.onopen = () => {
      connected.value = true
      reconnectAttempts.value = 0
      console.log('[OneDeck] Connected to desktop backend')
    }

    socket.onclose = () => {
      connected.value = false
      scheduleReconnect(url)
    }

    socket.onerror = (err) => {
      console.error('[OneDeck] WebSocket error:', err)
    }

    socket.onmessage = (event) => {
      try {
        const message = JSON.parse(event.data)
        handleMessage(message)
      } catch (e) {
        console.error('[OneDeck] Failed to parse message:', e)
      }
    }

    ws.value = socket
  }

  function disconnect() {
    if (reconnectTimer) {
      clearTimeout(reconnectTimer)
      reconnectTimer = null
    }
    ws.value?.close()
    ws.value = null
    connected.value = false
  }

  function send(message: Record<string, unknown>) {
    if (ws.value && ws.value.readyState === WebSocket.OPEN) {
      ws.value.send(JSON.stringify(message))
    }
  }

  function scheduleReconnect(url: string) {
    if (reconnectTimer) return

    // 指数退避：1s → 2s → 4s → 8s → 16s → 30s
    const delay = Math.min(1000 * Math.pow(2, reconnectAttempts.value), maxReconnectDelay)
    reconnectAttempts.value++

    console.log(`[OneDeck] Reconnecting in ${delay}ms (attempt ${reconnectAttempts.value})`)
    reconnectTimer = setTimeout(() => {
      reconnectTimer = null
      connect(url)
    }, delay)
  }

  function handleMessage(message: Record<string, unknown>) {
    // 路由消息到对应的 store
    const { useDeviceStore } = require('./devices')
    const { useLogStore } = require('./logs')
    const { useSchemeStore } = require('./schemes')

    const deviceStore = useDeviceStore()
    const logStore = useLogStore()
    const schemeStore = useSchemeStore()

    switch (message.type) {
      case 'device_register':
        deviceStore.addDevice(message.payload as any)
        break
      case 'device_heartbeat':
        deviceStore.updateHeartbeat(message.deviceId as string)
        break
      case 'log_transfer':
        logStore.addLog(message.payload as any)
        break
      case 'scheme_push':
        schemeStore.updateScheme(message.payload as any)
        break
      // 其他消息类型由各页面组件自行处理
    }
  }

  return {
    ws,
    connected,
    reconnectAttempts,
    connect,
    disconnect,
    send,
  }
})
