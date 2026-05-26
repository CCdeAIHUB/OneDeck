<script setup lang="ts">
import { Icon } from '@iconify/vue'
import { useConnectionStore } from '@/stores/connection'

const connection = useConnectionStore()

function minimize() {
  // TODO: 调用 C# 后端窗口控制 JSAPI
}

function maximize() {
  // TODO: 调用 C# 后端窗口控制 JSAPI
}

function close() {
  // TODO: 调用 C# 后端窗口控制 JSAPI
}
</script>

<template>
  <div class="titlebar-drag flex items-center justify-between h-[var(--titlebar-height)] bg-gray-900 border-b border-gray-800 px-4 select-none">
    <!-- 左侧：应用名称与连接状态 -->
    <div class="titlebar-no-drag flex items-center gap-2">
      <Icon icon="solar:widget-bold" class="text-indigo-400 text-lg" />
      <span class="text-sm font-semibold tracking-wide">OneDeck</span>
      <span
        class="w-2 h-2 rounded-full"
        :class="connection.connected ? 'bg-emerald-400' : 'bg-red-400'"
        :title="connection.connected ? '已连接' : '未连接'"
      />
    </div>

    <!-- 中间：拖拽区域 -->
    <div class="flex-1" />

    <!-- 右侧：窗口控制按钮 -->
    <div class="titlebar-no-drag flex items-center gap-1">
      <button
        class="w-8 h-8 flex items-center justify-center rounded hover:bg-gray-700 transition-colors"
        @click="minimize"
      >
        <Icon icon="solar:minimize-square-linear" class="text-gray-400 text-sm" />
      </button>
      <button
        class="w-8 h-8 flex items-center justify-center rounded hover:bg-gray-700 transition-colors"
        @click="maximize"
      >
        <Icon icon="solar:maximize-square-linear" class="text-gray-400 text-sm" />
      </button>
      <button
        class="w-8 h-8 flex items-center justify-center rounded hover:bg-red-600 transition-colors"
        @click="close"
      >
        <Icon icon="solar:close-square-linear" class="text-gray-400 text-sm" />
      </button>
    </div>
  </div>
</template>
