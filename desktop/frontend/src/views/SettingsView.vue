<script setup lang="ts">
import { Icon } from '@iconify/vue'
import PageHeader from '@/components/PageHeader.vue'
import { ref } from 'vue'

const wsPort = ref(9720)
const httpPort = ref(9721)
const autoStart = ref(false)
const minimizeToTray = ref(true)
const logLevel = ref<'debug' | 'info' | 'warn' | 'error'>('info')
const maxLogEntries = ref(100000)
const theme = ref<'dark' | 'system'>('dark')

function saveSettings() {
  // TODO: 调用后端保存设置
}
</script>

<template>
  <div>
    <PageHeader title="设置" subtitle="系统偏好设置" icon="solar:settings-bold">
      <template #actions>
        <button
          class="flex items-center gap-2 px-4 py-2 bg-indigo-600 hover:bg-indigo-500 rounded-lg text-sm transition-colors"
          @click="saveSettings"
        >
          <Icon icon="solar:diskette-bold" class="text-base" />
          保存设置
        </button>
      </template>
    </PageHeader>

    <div class="space-y-6 max-w-2xl">
      <!-- 网络设置 -->
      <div class="bg-gray-900 border border-gray-800 rounded-xl p-5">
        <h3 class="font-semibold mb-4">网络</h3>
        <div class="space-y-4">
          <div class="flex items-center justify-between">
            <div>
              <p class="text-sm">WebSocket 端口</p>
              <p class="text-xs text-gray-500">移动端连接端口</p>
            </div>
            <input
              v-model.number="wsPort"
              type="number"
              class="w-24 px-3 py-1.5 bg-gray-800 border border-gray-700 rounded-lg text-sm text-white text-right focus:outline-none focus:border-indigo-500"
            />
          </div>
          <div class="flex items-center justify-between">
            <div>
              <p class="text-sm">HTTP 端口</p>
              <p class="text-xs text-gray-500">前端页面服务端口</p>
            </div>
            <input
              v-model.number="httpPort"
              type="number"
              class="w-24 px-3 py-1.5 bg-gray-800 border border-gray-700 rounded-lg text-sm text-white text-right focus:outline-none focus:border-indigo-500"
            />
          </div>
        </div>
      </div>

      <!-- 通用设置 -->
      <div class="bg-gray-900 border border-gray-800 rounded-xl p-5">
        <h3 class="font-semibold mb-4">通用</h3>
        <div class="space-y-4">
          <div class="flex items-center justify-between">
            <div>
              <p class="text-sm">开机自启动</p>
              <p class="text-xs text-gray-500">系统启动时自动运行 OneDeck</p>
            </div>
            <button
              class="relative w-10 h-5 rounded-full transition-colors duration-200"
              :class="autoStart ? 'bg-indigo-600' : 'bg-gray-700'"
              @click="autoStart = !autoStart"
            >
              <span
                class="absolute top-0.5 w-4 h-4 bg-white rounded-full transition-transform duration-200"
                :class="autoStart ? 'translate-x-5' : 'translate-x-0.5'"
              />
            </button>
          </div>
          <div class="flex items-center justify-between">
            <div>
              <p class="text-sm">最小化到托盘</p>
              <p class="text-xs text-gray-500">关闭窗口时最小化到系统托盘</p>
            </div>
            <button
              class="relative w-10 h-5 rounded-full transition-colors duration-200"
              :class="minimizeToTray ? 'bg-indigo-600' : 'bg-gray-700'"
              @click="minimizeToTray = !minimizeToTray"
            >
              <span
                class="absolute top-0.5 w-4 h-4 bg-white rounded-full transition-transform duration-200"
                :class="minimizeToTray ? 'translate-x-5' : 'translate-x-0.5'"
              />
            </button>
          </div>
          <div class="flex items-center justify-between">
            <div>
              <p class="text-sm">主题</p>
              <p class="text-xs text-gray-500">界面主题</p>
            </div>
            <select
              v-model="theme"
              class="px-3 py-1.5 bg-gray-800 border border-gray-700 rounded-lg text-sm text-white focus:outline-none focus:border-indigo-500"
            >
              <option value="dark">深色</option>
              <option value="system">跟随系统</option>
            </select>
          </div>
        </div>
      </div>

      <!-- 日志设置 -->
      <div class="bg-gray-900 border border-gray-800 rounded-xl p-5">
        <h3 class="font-semibold mb-4">日志</h3>
        <div class="space-y-4">
          <div class="flex items-center justify-between">
            <div>
              <p class="text-sm">最低日志级别</p>
              <p class="text-xs text-gray-500">低于此级别的日志不会记录</p>
            </div>
            <select
              v-model="logLevel"
              class="px-3 py-1.5 bg-gray-800 border border-gray-700 rounded-lg text-sm text-white focus:outline-none focus:border-indigo-500"
            >
              <option value="debug">DEBUG</option>
              <option value="info">INFO</option>
              <option value="warn">WARN</option>
              <option value="error">ERROR</option>
            </select>
          </div>
          <div class="flex items-center justify-between">
            <div>
              <p class="text-sm">最大日志条数</p>
              <p class="text-xs text-gray-500">超出后自动清理最旧的日志</p>
            </div>
            <input
              v-model.number="maxLogEntries"
              type="number"
              class="w-32 px-3 py-1.5 bg-gray-800 border border-gray-700 rounded-lg text-sm text-white text-right focus:outline-none focus:border-indigo-500"
            />
          </div>
        </div>
      </div>

      <!-- 关于 -->
      <div class="bg-gray-900 border border-gray-800 rounded-xl p-5">
        <h3 class="font-semibold mb-4">关于</h3>
        <div class="text-sm text-gray-400 space-y-1">
          <p>OneDeck v0.1.0</p>
          <p>跨平台流控制软件系统</p>
          <p class="text-xs text-gray-600 mt-2">桌面端地址：ws://localhost:{{ wsPort }}</p>
        </div>
      </div>
    </div>
  </div>
</template>
