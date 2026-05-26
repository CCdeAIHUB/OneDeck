import { defineStore } from 'pinia'
import { ref, watch } from 'vue'

/**
 * 公共参数库
 * 持久化 JSON 存储，任何组件和插件都可以读写
 * 以动态保存文件的形式存在
 * 注意：存在不安全性，但不影响软件主体
 */

const STORAGE_KEY = 'onedesk-shared-params'

export const useSharedParamsStore = defineStore('sharedParams', () => {
  const params = ref<Record<string, unknown>>({})

  /** 从 localStorage 加载 */
  function load() {
    try {
      const saved = localStorage.getItem(STORAGE_KEY)
      if (saved) {
        params.value = JSON.parse(saved)
      }
    } catch (e) {
      console.error('加载公共参数库失败:', e)
    }
  }

  /** 持久化保存 */
  function persist() {
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(params.value))
    } catch (e) {
      console.error('保存公共参数库失败:', e)
    }
  }

  /** 读取参数 */
  function get<T = unknown>(key: string, defaultValue?: T): T {
    if (key in params.value) {
      return params.value[key] as T
    }
    return defaultValue as T
  }

  /** 写入参数 */
  function set(key: string, value: unknown) {
    params.value[key] = value
    persist()
  }

  /** 批量写入 */
  function batchSet(entries: Record<string, unknown>) {
    for (const [key, value] of Object.entries(entries)) {
      params.value[key] = value
    }
    persist()
  }

  /** 删除参数 */
  function remove(key: string) {
    delete params.value[key]
    persist()
  }

  /** 清空所有参数 */
  function clear() {
    params.value = {}
    persist()
  }

  /** 检查参数是否存在 */
  function has(key: string): boolean {
    return key in params.value
  }

  /** 获取所有参数的 key 列表 */
  function keys(): string[] {
    return Object.keys(params.value)
  }

  /** 导出为 JSON 字符串 */
  function exportJson(): string {
    return JSON.stringify(params.value, null, 2)
  }

  /** 从 JSON 字符串导入 */
  function importJson(json: string, merge = false) {
    try {
      const data = JSON.parse(json)
      if (merge) {
        params.value = { ...params.value, ...data }
      } else {
        params.value = data
      }
      persist()
    } catch (e) {
      console.error('导入公共参数库失败:', e)
      throw new Error('无效的 JSON 格式')
    }
  }

  // 监听变化自动持久化
  watch(params, () => persist(), { deep: true })

  // 初始化加载
  load()

  return {
    params,
    load,
    persist,
    get,
    set,
    batchSet,
    remove,
    clear,
    has,
    keys,
    exportJson,
    importJson,
  }
})
