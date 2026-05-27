<script setup lang="ts">
import { Icon } from '@iconify/vue'
import PageHeader from '@/components/PageHeader.vue'
import { useThemeStore, type ThemeMode } from '@/stores/theme'
import { useLogStore, type LogLevel } from '@/stores/logs'
import { ref, computed, nextTick, watch } from 'vue'

const themeStore = useThemeStore()
const logStore = useLogStore()

const wsPort = ref(9720)
const autoStart = ref(false)
const minimizeToTray = ref(true)
const logLevel = ref<'debug' | 'info' | 'warn' | 'error'>('info')
const maxLogEntries = ref(100000)

const themeOptions: { value: ThemeMode; label: string }[] = [
  { value: 'dark', label: '深色' },
  { value: 'light', label: '浅色' },
  { value: 'system', label: '跟随系统' },
]

function saveSettings() {
  // TODO: 调用后端保存设置
}

// ==================== 日志查看器 ====================
const logsContainer = ref<HTMLElement | null>(null)
const autoScroll = ref(true)

const levelOptions: { value: LogLevel | null; label: string; color: string }[] = [
  { value: null, label: '全部', color: 'var(--color-text-muted)' },
  { value: 'debug', label: 'DEBUG', color: 'var(--color-text-dim)' },
  { value: 'info', label: 'INFO', color: 'var(--color-primary)' },
  { value: 'warn', label: 'WARN', color: '#f59e0b' },
  { value: 'error', label: 'ERROR', color: '#ef4444' },
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
    info: 'text-blue-400',
    warn: 'text-amber-400',
    error: 'text-red-400',
  }
  return map[level] || 'bg-gray-700 text-gray-400'
}

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
  <div>
    <PageHeader title="设置" subtitle="系统偏好设置" icon="solar:settings-bold">
      <template #actions>
        <button
          class="flex items-center gap-2 px-4 py-2 rounded-lg text-sm text-white transition-colors"
          style="background-color: var(--color-primary);"
          @click="saveSettings"
        >
          <Icon icon="solar:diskette-bold" class="text-base" />
          保存设置
        </button>
      </template>
    </PageHeader>

    <div class="space-y-6 max-w-3xl">
      <!-- 外观设置 -->
      <div class="rounded-xl p-5 border" style="background-color: var(--color-bg-card); border-color: var(--color-border);">
        <h3 class="font-semibold mb-4">外观</h3>
        <div class="space-y-4">
          <div class="flex items-center justify-between gap-4">
            <div class="min-w-0">
              <p class="text-sm">主题模式</p>
              <p class="text-xs" style="color: var(--color-text-dim);">选择界面主题风格</p>
            </div>
            <div class="flex items-center gap-1 p-0.5 rounded-lg" style="background-color: var(--color-bg-surface);">
              <button
                v-for="opt in themeOptions"
                :key="opt.value"
                class="px-3 py-1.5 text-xs rounded-md transition-colors"
                :style="themeStore.mode === opt.value ? 'background-color: var(--color-primary); color: white;' : 'color: var(--color-text-muted);'"
                @click="themeStore.setMode(opt.value)"
              >
                {{ opt.label }}
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- 网络设置 -->
      <div class="rounded-xl p-5 border" style="background-color: var(--color-bg-card); border-color: var(--color-border);">
        <h3 class="font-semibold mb-4">网络</h3>
        <div class="space-y-4">
          <div class="flex items-center justify-between gap-4">
            <div class="min-w-0">
              <p class="text-sm">WebSocket 端口</p>
              <p class="text-xs" style="color: var(--color-text-dim);">移动端连接端口</p>
            </div>
            <input
              v-model.number="wsPort"
              type="number"
              class="w-24 px-3 py-1.5 rounded-lg text-sm text-right focus:outline-none"
              style="background-color: var(--color-bg-surface); border: 1px solid var(--color-border); color: var(--color-text);"
            />
          </div>
        </div>
      </div>

      <!-- 通用设置 -->
      <div class="rounded-xl p-5 border" style="background-color: var(--color-bg-card); border-color: var(--color-border);">
        <h3 class="font-semibold mb-4">通用</h3>
        <div class="space-y-4">
          <div class="flex items-center justify-between gap-4">
            <div class="min-w-0">
              <p class="text-sm">开机自启动</p>
              <p class="text-xs" style="color: var(--color-text-dim);">系统启动时自动运行 OneDesk</p>
            </div>
            <button
              class="relative w-11 h-6 rounded-full shrink-0 transition-colors duration-200"
              :style="autoStart ? 'background-color: var(--color-primary);' : 'background-color: var(--color-bg-hover);'"
              @click="autoStart = !autoStart"
            >
              <span
                class="absolute top-1 left-1 w-4 h-4 bg-white rounded-full transition-transform duration-200"
                :class="autoStart ? 'translate-x-5' : 'translate-x-0'"
              />
            </button>
          </div>
          <div class="flex items-center justify-between gap-4">
            <div class="min-w-0">
              <p class="text-sm">最小化到托盘</p>
              <p class="text-xs" style="color: var(--color-text-dim);">关闭窗口时最小化到系统托盘</p>
            </div>
            <button
              class="relative w-11 h-6 rounded-full shrink-0 transition-colors duration-200"
              :style="minimizeToTray ? 'background-color: var(--color-primary);' : 'background-color: var(--color-bg-hover);'"
              @click="minimizeToTray = !minimizeToTray"
            >
              <span
                class="absolute top-1 left-1 w-4 h-4 bg-white rounded-full transition-transform duration-200"
                :class="minimizeToTray ? 'translate-x-5' : 'translate-x-0'"
              />
            </button>
          </div>
        </div>
      </div>

      <!-- 日志设置 & 查看器 -->
      <div class="rounded-xl p-5 border" style="background-color: var(--color-bg-card); border-color: var(--color-border);">
        <h3 class="font-semibold mb-4">日志</h3>

        <!-- 日志设置 -->
        <div class="space-y-4 mb-4">
          <div class="flex items-center justify-between gap-4">
            <div class="min-w-0">
              <p class="text-sm">最低日志级别</p>
              <p class="text-xs" style="color: var(--color-text-dim);">低于此级别的日志不会记录</p>
            </div>
            <select
              v-model="logLevel"
              class="px-3 py-1.5 rounded-lg text-sm focus:outline-none"
              style="background-color: var(--color-bg-surface); border: 1px solid var(--color-border); color: var(--color-text);"
            >
              <option value="debug">DEBUG</option>
              <option value="info">INFO</option>
              <option value="warn">WARN</option>
              <option value="error">ERROR</option>
            </select>
          </div>
          <div class="flex items-center justify-between gap-4">
            <div class="min-w-0">
              <p class="text-sm">最大日志条数</p>
              <p class="text-xs" style="color: var(--color-text-dim);">超出后自动清理最旧的日志</p>
            </div>
            <input
              v-model.number="maxLogEntries"
              type="number"
              class="w-32 px-3 py-1.5 rounded-lg text-sm text-right focus:outline-none"
              style="background-color: var(--color-bg-surface); border: 1px solid var(--color-border); color: var(--color-text);"
            />
          </div>
        </div>

        <!-- 日志过滤栏 -->
        <div class="flex items-center gap-3 mb-3">
          <div class="flex items-center gap-1">
            <button
              v-for="opt in levelOptions"
              :key="opt.value ?? 'all'"
              class="px-3 py-1 text-xs rounded-full transition-colors"
              :style="selectedLevel === opt.value ? 'background-color: var(--color-primary); color: white;' : 'color: var(--color-text-muted);'"
              @click="setLevel(opt.value)"
            >
              {{ opt.label }}
              <span v-if="opt.value" class="ml-1 opacity-60">
                {{ logStore.levelCounts[opt.value] }}
              </span>
            </button>
          </div>

          <div class="relative flex-1 max-w-xs">
            <Icon icon="solar:magnifer-linear" class="absolute left-3 top-1/2 -translate-y-1/2 text-sm" style="color: var(--color-text-dim);" />
            <input
              v-model="logStore.filter.searchText"
              type="text"
              placeholder="搜索日志..."
              class="w-full pl-9 pr-3 py-1.5 rounded-lg text-sm placeholder-gray-500 focus:outline-none"
              style="background-color: var(--color-bg-surface); border: 1px solid var(--color-border); color: var(--color-text);"
            />
          </div>

          <button
            class="flex items-center gap-1 px-3 py-1.5 rounded-lg text-xs transition-colors"
            style="background-color: var(--color-bg-surface); color: var(--color-text-muted);"
            @click="clearLogs"
          >
            <Icon icon="solar:trash-bin-trash-bold" class="text-sm" />
            清空
          </button>
        </div>

        <!-- 日志列表 -->
        <div
          ref="logsContainer"
          class="h-64 overflow-y-auto font-mono text-xs rounded-lg border"
          style="background-color: var(--color-bg); border-color: var(--color-border-subtle);"
        >
          <table class="w-full">
            <thead class="sticky top-0 z-10" style="background-color: var(--color-bg-card);">
              <tr class="text-left border-b" style="color: var(--color-text-dim); border-color: var(--color-border-subtle);">
                <th class="px-3 py-2 w-20">级别</th>
                <th class="px-3 py-2 w-20">时间</th>
                <th class="px-3 py-2 w-32">来源</th>
                <th class="px-3 py-2">消息</th>
              </tr>
            </thead>
            <tbody>
              <tr
                v-for="log in logStore.filteredLogs"
                :key="log.id"
                class="border-b transition-colors hover:opacity-80"
                style="border-color: var(--color-border-subtle);"
              >
                <td class="px-3 py-1.5">
                  <span class="px-1.5 py-0.5 rounded text-[10px] font-bold" :class="levelBadgeClass(log.level)">
                    {{ log.level.toUpperCase() }}
                  </span>
                </td>
                <td class="px-3 py-1.5" style="color: var(--color-text-muted);">{{ log.timestamp.slice(11, 19) }}</td>
                <td class="px-3 py-1.5" style="color: var(--color-text-muted);">{{ log.source }}</td>
                <td class="px-3 py-1.5" :class="{ 'text-red-300': log.level === 'error', 'text-amber-300': log.level === 'warn' }">
                  {{ log.message }}
                </td>
              </tr>
            </tbody>
          </table>

          <div v-if="logStore.filteredLogs.length === 0" class="text-center py-12" style="color: var(--color-text-dim);">
            暂无日志记录
          </div>
        </div>
      </div>

      <!-- 关于 -->
      <div class="rounded-xl p-5 border" style="background-color: var(--color-bg-card); border-color: var(--color-border);">
        <h3 class="font-semibold mb-4">关于</h3>
        <div class="text-sm space-y-1" style="color: var(--color-text-muted);">
          <p>OneDesk v0.1.0</p>
          <p>跨平台流控制软件系统</p>
          <p class="text-xs mt-2" style="color: var(--color-text-dim);">桌面端地址：ws://localhost:{{ wsPort }}</p>
        </div>
      </div>
    </div>
  </div>
</template>
