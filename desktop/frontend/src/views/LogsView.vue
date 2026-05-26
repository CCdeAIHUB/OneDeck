<script setup lang="ts">
import { Icon } from '@iconify/vue'
import PageHeader from '@/components/PageHeader.vue'
import { useLogStore, type LogLevel } from '@/stores/logs'
import { computed, ref, nextTick, watch } from 'vue'

const logStore = useLogStore()
const logsContainer = ref<HTMLElement | null>(null)
const autoScroll = ref(true)

const levelOptions: { value: LogLevel | null; label: string; color: string }[] = [
  { value: null, label: '全部', color: 'text-gray-400' },
  { value: 'debug', label: 'DEBUG', color: 'text-gray-500' },
  { value: 'info', label: 'INFO', color: 'text-blue-400' },
  { value: 'warn', label: 'WARN', color: 'text-amber-400' },
  { value: 'error', label: 'ERROR', color: 'text-red-400' },
]

const selectedLevel = computed(() => logStore.filter.level)

function setLevel(level: LogLevel | null) {
  logStore.setFilter({ level })
}

function clearLogs() {
  logStore.clearLogs()
}

const levelBadgeClass = (level: LogLevel) => {
  const map: Record<string, string> = {
    debug: 'bg-gray-700 text-gray-400',
    info: 'bg-blue-500/20 text-blue-400',
    warn: 'bg-amber-500/20 text-amber-400',
    error: 'bg-red-500/20 text-red-400',
  }
  return map[level] || 'bg-gray-700 text-gray-400'
}

// 自动滚动到底部
watch(
  () => logStore.logs.length,
  async () => {
    if (autoScroll.value) {
      await nextTick()
      logsContainer.value?.scrollTo({ top: logsContainer.value.scrollHeight })
    }
  }
)
</script>

<template>
  <div class="flex flex-col h-full">
    <PageHeader title="日志" subtitle="系统运行日志查看与排查" icon="solar:document-text-bold">
      <template #actions>
        <button
          class="flex items-center gap-2 px-3 py-2 bg-gray-700 hover:bg-gray-600 rounded-lg text-sm transition-colors"
          @click="clearLogs"
        >
          <Icon icon="solar:trash-bin-trash-bold" class="text-base" />
          清空
        </button>
      </template>
    </PageHeader>

    <!-- 过滤栏 -->
    <div class="flex items-center gap-3 mb-4">
      <!-- 级别筛选 -->
      <div class="flex items-center gap-1">
        <button
          v-for="opt in levelOptions"
          :key="opt.value ?? 'all'"
          class="px-3 py-1 text-xs rounded-full transition-colors"
          :class="selectedLevel === opt.value ? 'bg-indigo-600 text-white' : 'bg-gray-800 text-gray-400 hover:bg-gray-700'"
          @click="setLevel(opt.value)"
        >
          {{ opt.label }}
          <span v-if="opt.value" class="ml-1 opacity-60">
            {{ logStore.levelCounts[opt.value] }}
          </span>
        </button>
      </div>

      <!-- 搜索 -->
      <div class="relative flex-1 max-w-xs">
        <Icon icon="solar:magnifer-linear" class="absolute left-3 top-1/2 -translate-y-1/2 text-gray-500 text-sm" />
        <input
          v-model="logStore.filter.searchText"
          type="text"
          placeholder="搜索日志..."
          class="w-full pl-9 pr-3 py-1.5 bg-gray-800 border border-gray-700 rounded-lg text-sm text-white placeholder-gray-500 focus:outline-none focus:border-indigo-500"
        />
      </div>

      <!-- 来源筛选 -->
      <input
        v-model="logStore.filter.source"
        type="text"
        placeholder="来源..."
        class="w-32 px-3 py-1.5 bg-gray-800 border border-gray-700 rounded-lg text-sm text-white placeholder-gray-500 focus:outline-none focus:border-indigo-500"
      />

      <!-- 自动滚动 -->
      <label class="flex items-center gap-2 text-xs text-gray-400 cursor-pointer">
        <input v-model="autoScroll" type="checkbox" class="rounded" />
        自动滚动
      </label>
    </div>

    <!-- 日志列表 -->
    <div
      ref="logsContainer"
      class="flex-1 bg-gray-900 border border-gray-800 rounded-xl overflow-y-auto font-mono text-xs"
    >
      <table class="w-full">
        <thead class="sticky top-0 bg-gray-900 z-10">
          <tr class="text-left text-gray-500 border-b border-gray-800">
            <th class="px-3 py-2 w-20">级别</th>
            <th class="px-3 py-2 w-20">时间</th>
            <th class="px-3 py-2 w-32">来源</th>
            <th class="px-3 py-2">消息</th>
            <th class="px-3 py-2 w-24">设备</th>
            <th class="px-3 py-2 w-24">插件</th>
          </tr>
        </thead>
        <tbody>
          <tr
            v-for="log in logStore.filteredLogs"
            :key="log.id"
            class="border-b border-gray-800/50 hover:bg-gray-800/50"
          >
            <td class="px-3 py-1.5">
              <span class="px-1.5 py-0.5 rounded text-[10px] font-bold" :class="levelBadgeClass(log.level)">
                {{ log.level.toUpperCase() }}
              </span>
            </td>
            <td class="px-3 py-1.5 text-gray-400">{{ log.timestamp.slice(11, 19) }}</td>
            <td class="px-3 py-1.5 text-gray-400">{{ log.source }}</td>
            <td class="px-3 py-1.5" :class="{ 'text-red-300': log.level === 'error', 'text-amber-300': log.level === 'warn' }">
              {{ log.message }}
            </td>
            <td class="px-3 py-1.5 text-gray-500">{{ log.deviceId || '-' }}</td>
            <td class="px-3 py-1.5 text-gray-500">{{ log.pluginId || '-' }}</td>
          </tr>
        </tbody>
      </table>

      <div v-if="logStore.filteredLogs.length === 0" class="text-center py-12 text-gray-600">
        暂无日志记录
      </div>
    </div>
  </div>
</template>
