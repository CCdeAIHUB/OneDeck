import { defineStore } from 'pinia'
import { ref, computed } from 'vue'

export type LogLevel = 'debug' | 'info' | 'warn' | 'error'

export interface LogEntry {
  id: number
  level: LogLevel
  message: string
  source: string
  stackTrace?: string
  deviceId?: string
  pluginId?: string
  timestamp: string
}

export interface LogFilter {
  level: LogLevel | null
  source: string
  searchText: string
  deviceId: string | null
  pluginId: string | null
}

export const useLogStore = defineStore('logs', () => {
  const logs = ref<LogEntry[]>([])
  const filter = ref<LogFilter>({
    level: null,
    source: '',
    searchText: '',
    deviceId: null,
    pluginId: null,
  })
  const total = ref(0)
  const offset = ref(0)
  const limit = ref(100)
  const autoScroll = ref(true)

  const filteredLogs = computed(() => {
    return logs.value.filter((log) => {
      if (filter.value.level && log.level !== filter.value.level) return false
      if (filter.value.source && !log.source.toLowerCase().includes(filter.value.source.toLowerCase())) return false
      if (filter.value.searchText && !log.message.toLowerCase().includes(filter.value.searchText.toLowerCase())) return false
      if (filter.value.deviceId && log.deviceId !== filter.value.deviceId) return false
      if (filter.value.pluginId && log.pluginId !== filter.value.pluginId) return false
      return true
    })
  })

  const levelCounts = computed(() => ({
    debug: logs.value.filter((l) => l.level === 'debug').length,
    info: logs.value.filter((l) => l.level === 'info').length,
    warn: logs.value.filter((l) => l.level === 'warn').length,
    error: logs.value.filter((l) => l.level === 'error').length,
  }))

  function addLog(entry: LogEntry) {
    logs.value.push(entry)
  }

  function setLogs(list: LogEntry[], totalCount: number) {
    logs.value = list
    total.value = totalCount
  }

  function setFilter(newFilter: Partial<LogFilter>) {
    filter.value = { ...filter.value, ...newFilter }
  }

  function clearLogs() {
    logs.value = []
    total.value = 0
    offset.value = 0
  }

  return {
    logs,
    filter,
    total,
    offset,
    limit,
    autoScroll,
    filteredLogs,
    levelCounts,
    addLog,
    setLogs,
    setFilter,
    clearLogs,
  }
})
