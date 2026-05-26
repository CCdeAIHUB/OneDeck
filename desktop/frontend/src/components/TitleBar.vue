<script setup lang="ts">
import { Icon } from '@iconify/vue'
import { useThemeStore } from '@/stores/theme'
import { computed } from 'vue'

const themeStore = useThemeStore()

// 检测操作系统
const isMac = computed(() => navigator.userAgent.includes('Mac'))
const isMaximized = computed(() => false) // TODO: 从后端获取窗口状态

function minimize() {
  if (window.chrome?.webview) {
    window.chrome.webview.postMessage('window:minimize')
  }
}

function toggleMaximize() {
  if (window.chrome?.webview) {
    window.chrome.webview.postMessage('window:maximize')
  }
}

function close() {
  if (window.chrome?.webview) {
    window.chrome.webview.postMessage('window:close')
  }
}
</script>

<template>
  <div class="titlebar-drag flex items-center justify-between shrink-0 border-b" style="height: var(--titlebar-height); background-color: var(--color-bg-card); border-color: var(--color-border-subtle);">
    <!-- macOS: 左侧红绿灯按钮 -->
    <template v-if="isMac">
      <div class="titlebar-no-drag flex items-center gap-2 pl-3 traffic-lights">
        <button class="traffic-light traffic-light-close" @click="close">
          <svg width="8" height="8" viewBox="0 0 8 8"><line x1="1" y1="1" x2="7" y2="7" stroke="currentColor" stroke-width="1.2"/><line x1="7" y1="1" x2="1" y2="7" stroke="currentColor" stroke-width="1.2"/></svg>
        </button>
        <button class="traffic-light traffic-light-minimize" @click="minimize">
          <svg width="8" height="8" viewBox="0 0 8 8"><line x1="1" y1="4" x2="7" y2="4" stroke="currentColor" stroke-width="1.2"/></svg>
        </button>
        <button class="traffic-light traffic-light-maximize" @click="toggleMaximize">
          <svg width="8" height="8" viewBox="0 0 8 8"><rect x="1" y="1" width="6" height="6" fill="none" stroke="currentColor" stroke-width="1.2"/></svg>
        </button>
      </div>
      <div class="flex-1" />
      <!-- 右侧：主题切换 -->
      <div class="titlebar-no-drag flex items-center gap-1 pr-3">
        <button
          class="p-1.5 rounded transition-colors"
          style="color: var(--color-text-muted);"
          @click="themeStore.setMode(themeStore.getEffectiveTheme() === 'dark' ? 'light' : 'dark')"
          title="切换主题"
        >
          <Icon :icon="themeStore.getEffectiveTheme() === 'dark' ? 'solar:sun-bold' : 'solar:moon-bold'" class="text-sm" />
        </button>
      </div>
    </template>

    <!-- Windows: 左侧应用名 -->
    <template v-else>
      <div class="titlebar-no-drag flex items-center gap-2 pl-3">
        <Icon icon="solar:widget-bold" style="color: var(--color-primary);" class="text-base" />
        <span class="text-xs font-semibold tracking-wide" style="color: var(--color-text-muted);">OneDeck</span>
      </div>

      <div class="flex-1" />

      <!-- 右侧：主题切换 + 窗口控制按钮 -->
      <div class="titlebar-no-drag flex items-center">
        <button
          class="px-2 h-full flex items-center transition-colors"
          style="color: var(--color-text-muted);"
          @click="themeStore.setMode(themeStore.getEffectiveTheme() === 'dark' ? 'light' : 'dark')"
          title="切换主题"
        >
          <Icon :icon="themeStore.getEffectiveTheme() === 'dark' ? 'solar:sun-bold' : 'solar:moon-bold'" class="text-sm" />
        </button>
        <!-- Windows 原生风格按钮 -->
        <button class="titlebar-btn" @click="minimize" title="最小化">
          <svg width="10" height="1" viewBox="0 0 10 1"><rect width="10" height="1" fill="currentColor"/></svg>
        </button>
        <button class="titlebar-btn" @click="toggleMaximize" :title="isMaximized ? '还原' : '最大化'">
          <svg v-if="isMaximized" width="10" height="10" viewBox="0 0 10 10">
            <rect x="2" y="0" width="8" height="8" fill="none" stroke="currentColor" stroke-width="1"/>
            <rect x="0" y="2" width="8" height="8" fill="var(--color-bg-card)" stroke="currentColor" stroke-width="1"/>
          </svg>
          <svg v-else width="10" height="10" viewBox="0 0 10 10">
            <rect x="0" y="0" width="10" height="10" fill="none" stroke="currentColor" stroke-width="1"/>
          </svg>
        </button>
        <button class="titlebar-btn titlebar-btn-close" @click="close" title="关闭">
          <svg width="10" height="10" viewBox="0 0 10 10">
            <line x1="0" y1="0" x2="10" y2="10" stroke="currentColor" stroke-width="1.2"/>
            <line x1="10" y1="0" x2="0" y2="10" stroke="currentColor" stroke-width="1.2"/>
          </svg>
        </button>
      </div>
    </template>
  </div>
</template>
