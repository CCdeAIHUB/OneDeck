<script setup lang="ts">
import { Icon } from '@iconify/vue'
import { ref } from 'vue'
import { useConnectionStore } from '@/stores/connection'

const connectionStore = useConnectionStore()
const desktopUrl = ref('ws://192.168.1.100:9720/mobile')

function connect() {
  connectionStore.connect(desktopUrl.value)
}
</script>

<template>
  <div class="h-screen flex flex-col items-center justify-center px-6">
    <div class="w-full max-w-sm">
      <!-- Logo -->
      <div class="text-center mb-8">
        <div class="w-20 h-20 bg-indigo-600/20 rounded-2xl flex items-center justify-center mx-auto mb-4">
          <Icon icon="solar:widget-bold" class="text-indigo-400 text-4xl" />
        </div>
        <h1 class="text-2xl font-bold">OneDeck</h1>
        <p class="text-sm text-gray-400 mt-1">连接到桌面端控制中心</p>
      </div>

      <!-- 连接表单 -->
      <div class="space-y-4">
        <div>
          <label class="block text-sm text-gray-400 mb-1.5">桌面端地址</label>
          <input
            v-model="desktopUrl"
            type="text"
            placeholder="ws://192.168.1.100:9720/mobile"
            class="w-full px-4 py-3 bg-gray-800 border border-gray-700 rounded-xl text-white placeholder-gray-500 focus:outline-none focus:border-indigo-500"
          />
        </div>

        <button
          class="w-full py-3 bg-indigo-600 hover:bg-indigo-500 rounded-xl text-sm font-semibold transition-colors"
          @click="connect"
        >
          连接
        </button>
      </div>

      <!-- 扫描二维码 -->
      <div class="mt-6 text-center">
        <button class="text-sm text-indigo-400 hover:text-indigo-300 transition-colors">
          <Icon icon="solar:qr-code-bold" class="inline mr-1" />
          扫描二维码连接
        </button>
      </div>

      <!-- 最近连接 -->
      <div class="mt-8">
        <h3 class="text-xs text-gray-500 uppercase tracking-wider mb-3">最近连接</h3>
        <div class="space-y-2">
          <button
            class="w-full flex items-center gap-3 px-3 py-2.5 bg-gray-800/50 hover:bg-gray-800 rounded-lg transition-colors text-left"
          >
            <Icon icon="solar:monitor-bold" class="text-gray-500" />
            <div>
              <p class="text-sm">Desktop-PC</p>
              <p class="text-xs text-gray-500">ws://192.168.1.100:9720</p>
            </div>
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
