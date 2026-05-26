<script setup lang="ts">
import { Icon } from '@iconify/vue'
import { ref } from 'vue'
import { useConnectionStore } from '@/stores/connection'
import { useRouter } from 'vue-router'

const connectionStore = useConnectionStore()
const router = useRouter()
const desktopUrl = ref(connectionStore.desktopUrl)

function reconnect() {
  connectionStore.connect(desktopUrl.value)
}

function disconnect() {
  connectionStore.disconnect()
  router.push('/connect')
}

function goToScheme() {
  router.push('/scheme')
}
</script>

<template>
  <div class="h-screen flex flex-col">
    <header class="shrink-0 flex items-center px-4 py-3 border-b border-gray-800">
      <button @click="goToScheme" class="mr-3">
        <Icon icon="solar:alt-arrow-left-bold" class="text-xl text-gray-400" />
      </button>
      <h1 class="text-lg font-semibold">设置</h1>
    </header>

    <main class="flex-1 overflow-y-auto p-4 space-y-4">
      <!-- 连接信息 -->
      <div class="bg-gray-900 border border-gray-800 rounded-xl p-4">
        <h3 class="font-semibold text-sm mb-3">连接</h3>
        <div class="space-y-3">
          <div>
            <label class="text-xs text-gray-400">桌面端地址</label>
            <input
              v-model="desktopUrl"
              type="text"
              class="w-full mt-1 px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm text-white focus:outline-none focus:border-indigo-500"
            />
          </div>
          <div class="flex items-center gap-2">
            <span
              class="w-2 h-2 rounded-full"
              :class="connectionStore.connected ? 'bg-emerald-400' : 'bg-red-400'"
            />
            <span class="text-sm">
              {{ connectionStore.connected ? '已连接' : '未连接' }}
            </span>
          </div>
          <div class="flex gap-2">
            <button
              class="flex-1 py-2 bg-indigo-600 hover:bg-indigo-500 rounded-lg text-sm transition-colors"
              @click="reconnect"
            >
              重新连接
            </button>
            <button
              class="flex-1 py-2 bg-gray-700 hover:bg-gray-600 rounded-lg text-sm transition-colors"
              @click="disconnect"
            >
              断开连接
            </button>
          </div>
        </div>
      </div>

      <!-- 关于 -->
      <div class="bg-gray-900 border border-gray-800 rounded-xl p-4">
        <h3 class="font-semibold text-sm mb-3">关于</h3>
        <div class="text-sm text-gray-400 space-y-1">
          <p>OneDeck Mobile v0.1.0</p>
          <p>设备ID：{{ connectionStore.deviceId || '未分配' }}</p>
        </div>
      </div>
    </main>
  </div>
</template>
