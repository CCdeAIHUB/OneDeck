<script setup lang="ts">
import { Icon } from '@iconify/vue'
import { useConnectionStore } from '@/stores/connection'
import { ref } from 'vue'

const connectionStore = useConnectionStore()
const showDetails = ref(false)
</script>

<template>
  <div class="fixed inset-0 bg-black/80 backdrop-blur-sm z-50 flex items-center justify-center">
    <div class="bg-gray-900 border border-gray-700 rounded-2xl p-8 max-w-sm w-full mx-4 text-center">
      <div class="w-16 h-16 bg-red-500/20 rounded-full flex items-center justify-center mx-auto mb-4">
        <Icon icon="solar:link-broken-bold" class="text-red-400 text-3xl" />
      </div>

      <h2 class="text-xl font-bold mb-2">连接已断开</h2>
      <p class="text-sm text-gray-400 mb-6">
        与桌面端的连接已断开，界面已冻结。请检查网络连接后等待自动重连。
      </p>

      <div v-if="showDetails" class="text-left text-xs text-gray-500 mb-4 bg-gray-800 rounded-lg p-3">
        <p>桌面端地址：{{ connectionStore.desktopUrl || '未设置' }}</p>
        <p>重连次数：{{ connectionStore.reconnectAttempts }}</p>
        <p>缓存日志：{{ connectionStore.logBuffer.length }} 条</p>
      </div>

      <div class="flex gap-3 justify-center">
        <button
          class="px-4 py-2 text-sm bg-gray-700 hover:bg-gray-600 rounded-lg transition-colors"
          @click="showDetails = !showDetails"
        >
          {{ showDetails ? '隐藏详情' : '查看详情' }}
        </button>

        <button
          class="px-4 py-2 text-sm bg-indigo-600 hover:bg-indigo-500 rounded-lg transition-colors"
          @click="connectionStore.connect()"
        >
          重新连接
        </button>
      </div>

      <!-- 重连动画 -->
      <div v-if="connectionStore.reconnectAttempts > 0" class="mt-4">
        <div class="flex items-center justify-center gap-2 text-xs text-gray-500">
          <Icon icon="solar:refresh-bold" class="animate-spin" />
          正在尝试重连...（第 {{ connectionStore.reconnectAttempts }} 次）
        </div>
      </div>
    </div>
  </div>
</template>
