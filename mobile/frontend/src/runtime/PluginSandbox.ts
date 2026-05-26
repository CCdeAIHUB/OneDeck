/**
 * 插件沙箱运行时
 * 隔离插件运行环境，防止插件间互相干扰和污染主程序
 * 插件无法自主存储任何内容，所有存储通过 JSAPI
 */

export interface SchemeLayout {
  type: 'grid' | 'free' | 'tabs'
  columns: number
  rows: number
  pages: SchemePage[]
}

export interface SchemePage {
  id: string
  name: string
  slots: SchemeSlot[]
}

export interface SchemeSlot {
  id: string
  pluginId: string
  row: number
  column: number
  rowSpan: number
  columnSpan: number
}

export interface SchemePluginInstance {
  pluginId: string
  instanceId: string
  config: Record<string, unknown>
}

export interface PluginContext {
  pluginId: string
  instanceId: string
  callApi(apiName: string, args?: Record<string, unknown>): Promise<unknown>
  storage: PluginStorage
  getConfig(): Record<string, unknown>
  emitEvent(eventType: string, data?: Record<string, unknown>): void
  setState(state: Record<string, unknown>): void
  log: PluginLogger
}

export interface PluginStorage {
  get(key: string): Promise<unknown>
  set(key: string, value: unknown): Promise<void>
  remove(key: string): Promise<void>
  keys(): Promise<string[]>
  clear(): Promise<void>
  temp: {
    get(key: string): unknown
    set(key: string, value: unknown): void
    remove(key: string): void
    clear(): void
  }
}

export interface PluginLogger {
  debug(message: string, ...args: unknown[]): void
  info(message: string, ...args: unknown[]): void
  warn(message: string, ...args: unknown[]): void
  error(message: string, ...args: unknown[]): void
}

/**
 * 创建插件沙箱上下文
 * 每个插件实例获得独立的上下文，互不干扰
 */
export function createPluginContext(
  pluginId: string,
  instanceId: string,
  config: Record<string, unknown>
): PluginContext {
  const tempStorage = new Map<string, unknown>()

  // 调用 JSAPI（通过原生壳子 → WebSocket → 桌面端）
  async function callApi(apiName: string, args?: Record<string, unknown>): Promise<unknown> {
    if (window.OneDeckNative) {
      // 移动端：调用原生 JSAPI
      const result = await window.OneDeckNative.callApi(apiName, JSON.stringify(args ?? {}))
      return JSON.parse(result)
    } else {
      // 开发模式：通过 WebSocket 调用桌面端 JSAPI
      const { useConnectionStore } = require('../stores/connection')
      const connectionStore = useConnectionStore()

      return new Promise((resolve, reject) => {
        const callId = `${pluginId}-${instanceId}-${Date.now()}`
        const timeout = setTimeout(() => reject(new Error('JSAPI call timeout')), 30000)

        // TODO: 注册一次性回调等待 jsapi_response
        connectionStore.send({
          type: 'jsapi_call',
          deviceId: connectionStore.deviceId,
          pluginId,
          payload: { apiName, args: args ?? {}, callId },
        })

        // 临时实现
        clearTimeout(timeout)
        resolve(null)
      })
    }
  }

  const logger: PluginLogger = {
    debug(message: string, ...args: unknown[]) {
      logToDesktop('debug', message, args)
    },
    info(message: string, ...args: unknown[]) {
      logToDesktop('info', message, args)
    },
    warn(message: string, ...args: unknown[]) {
      logToDesktop('warn', message, args)
    },
    error(message: string, ...args: unknown[]) {
      logToDesktop('error', message, args)
    },
  }

  function logToDesktop(level: string, message: string, args: unknown[]) {
    const { useConnectionStore } = require('../stores/connection')
    const connectionStore = useConnectionStore()
    connectionStore.log(level, `Plugin:${pluginId}`, message)
  }

  return {
    pluginId,
    instanceId,
    callApi,

    storage: {
      async get(key: string) {
        return callApi('storage.get', { key })
      },
      async set(key: string, value: unknown) {
        await callApi('storage.set', { key, value: JSON.stringify(value) })
      },
      async remove(key: string) {
        await callApi('storage.remove', { key })
      },
      async keys() {
        const result = await callApi('storage.keys')
        return (result as any)?.keys ?? []
      },
      async clear() {
        await callApi('storage.clear')
      },
      temp: {
        get(key: string) {
          return tempStorage.get(key)
        },
        set(key: string, value: unknown) {
          tempStorage.set(key, value)
        },
        remove(key: string) {
          tempStorage.delete(key)
        },
        clear() {
          tempStorage.clear()
        },
      },
    },

    getConfig() {
      return { ...config }
    },

    emitEvent(eventType: string, data?: Record<string, unknown>) {
      const { useConnectionStore } = require('../stores/connection')
      const connectionStore = useConnectionStore()
      connectionStore.send({
        type: 'event_report',
        deviceId: connectionStore.deviceId,
        pluginId,
        payload: { pluginId, instanceId, eventType, eventData: data ?? {} },
      })
    },

    setState(state: Record<string, unknown>) {
      // 本地更新状态 + 通知桌面端
      const { useSchemeStore } = require('../stores/scheme')
      const schemeStore = useSchemeStore()
      schemeStore.updatePluginState(pluginId, instanceId, state)

      const { useConnectionStore } = require('../stores/connection')
      const connectionStore = useConnectionStore()
      connectionStore.send({
        type: 'state_sync',
        deviceId: connectionStore.deviceId,
        pluginId,
        payload: { pluginId, instanceId, state, source: 'mobile' },
      })
    },

    log: logger,
  }
}
