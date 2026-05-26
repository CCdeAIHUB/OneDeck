<script setup lang="ts">
import { Icon } from '@iconify/vue'
import { useThemeStore } from '@/stores/theme'
import { useDeviceStore } from '@/stores/devices'
import { computed, ref, onMounted, onUnmounted } from 'vue'

const themeStore = useThemeStore()
const deviceStore = useDeviceStore()

const isMac = computed(() => navigator.userAgent.includes('Mac'))
const isMaximized = ref(false)

const currentDevice = computed(() => deviceStore.selectedDevice)
const onlineDevices = computed(() => deviceStore.onlineDevices)

// 监听窗口最大化状态（通过 C# 后端消息同步）
function handleWindowMessage(e: MessageEvent) {
  if (e.data?.type === 'window:state') {
    isMaximized.value = e.data.maximized
  }
}

onMounted(() => {
  window.addEventListener('message', handleWindowMessage)
  // 请求初始窗口状态
  if (window.chrome?.webview) {
    window.chrome.webview.postMessage('window:getState')
  }
})

onUnmounted(() => {
  window.removeEventListener('message', handleWindowMessage)
})

function minimize() {
  if (window.chrome?.webview) window.chrome.webview.postMessage('window:minimize')
}
function toggleMaximize() {
  if (window.chrome?.webview) window.chrome.webview.postMessage('window:maximize')
}
function close() {
  if (window.chrome?.webview) window.chrome.webview.postMessage('window:close')
}
function selectDevice(id: string | null) {
  deviceStore.selectDevice(id)
}

// 标题栏拖拽：通知 C# 后端进入窗口拖拽模式
function startDrag(e: MouseEvent) {
  // 只响应主按键（左键），且不是在按钮/选择框等交互元素上
  if (e.button !== 0) return
  const target = e.target as HTMLElement
  if (target.closest('button, select, input, a')) return
  if (window.chrome?.webview) window.chrome.webview.postMessage('window:drag')
}

// 双击标题栏切换最大化
function onTitlebarDblClick(e: MouseEvent) {
  const target = e.target as HTMLElement
  if (target.closest('button, select, input, a')) return
  toggleMaximize()
}
</script>

<template>
  <div class="titlebar-drag flex items-center justify-between shrink-0 border-b" style="height: var(--titlebar-height); background-color: var(--color-bg-card); border-color: var(--color-border-subtle);" @mousedown="startDrag" @dblclick="onTitlebarDblClick">
    <!-- macOS: 左侧红绿灯 -->
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
      <div class="titlebar-no-drag flex items-center gap-2 pr-3">
        <!-- 设备选择 -->
        <select
          :value="deviceStore.selectedDeviceId ?? ''"
          @change="selectDevice(($event.target as HTMLSelectElement).value || null)"
          class="px-2 py-0.5 rounded text-xs focus:outline-none"
          style="background-color: var(--color-bg-surface); border: 1px solid var(--color-border); color: var(--color-text-muted); max-width: 120px;"
        >
          <option value="">未选择设备</option>
          <option v-for="d in onlineDevices" :key="d.deviceId" :value="d.deviceId">{{ d.deviceName }}</option>
        </select>
        <button class="p-1 rounded hover:bg-gray-700/50 transition-colors" style="color: var(--color-text-muted);" @click="themeStore.setMode(themeStore.getEffectiveTheme() === 'dark' ? 'light' : 'dark')" title="切换主题">
          <Icon :icon="themeStore.getEffectiveTheme() === 'dark' ? 'solar:sun-bold' : 'solar:moon-bold'" class="text-sm" />
        </button>
      </div>
    </template>

    <!-- Windows -->
    <template v-else>
      <div class="titlebar-no-drag flex items-center gap-2 pl-3">
        <Icon icon="solar:widget-bold" style="color: var(--color-primary);" class="text-base" />
        <span class="text-xs font-semibold tracking-wide" style="color: var(--color-text-muted);">OneDesk</span>
      </div>

      <div class="flex-1" />

      <!-- 中间设备选择 -->
      <div class="titlebar-no-drag flex items-center mr-2">
        <select
          :value="deviceStore.selectedDeviceId ?? ''"
          @change="selectDevice(($event.target as HTMLSelectElement).value || null)"
          class="px-2 py-0.5 rounded text-xs focus:outline-none"
          style="background-color: var(--color-bg-surface); border: 1px solid var(--color-border); color: var(--color-text-muted); max-width: 140px;"
        >
          <option value="">未选择设备</option>
          <option v-for="d in onlineDevices" :key="d.deviceId" :value="d.deviceId">{{ d.deviceName }}</option>
        </select>
      </div>

      <!-- 右侧按钮 -->
      <div class="titlebar-no-drag flex items-center" style="height: var(--titlebar-height);">
        <!-- 主题切换 -->
        <button
          class="inline-flex items-center justify-center transition-colors"
          style="width: 46px; height: var(--titlebar-height); color: var(--color-text-muted);"
          @click="themeStore.setMode(themeStore.getEffectiveTheme() === 'dark' ? 'light' : 'dark')"
          title="切换主题"
          @mouseenter="($event.target as HTMLElement).style.backgroundColor = 'var(--color-bg-hover)'"
          @mouseleave="($event.target as HTMLElement).style.backgroundColor = 'transparent'"
        >
          <Icon :icon="themeStore.getEffectiveTheme() === 'dark' ? 'solar:sun-bold' : 'solar:moon-bold'" class="text-sm" />
        </button>
        <!-- 最小化 -->
        <button
          class="inline-flex items-center justify-center transition-colors"
          style="width: 46px; height: var(--titlebar-height); color: var(--color-text-muted);"
          @click="minimize" title="最小化"
          @mouseenter="($event.target as HTMLElement).style.backgroundColor = 'var(--color-bg-hover)'"
          @mouseleave="($event.target as HTMLElement).style.backgroundColor = 'transparent'"
        >
          <svg width="10" height="1" viewBox="0 0 10 1"><rect width="10" height="1" fill="currentColor"/></svg>
        </button>
        <!-- 最大化 -->
        <button
          class="inline-flex items-center justify-center transition-colors"
          style="width: 46px; height: var(--titlebar-height); color: var(--color-text-muted);"
          @click="toggleMaximize" :title="isMaximized ? '还原' : '最大化'"
          @mouseenter="($event.target as HTMLElement).style.backgroundColor = 'var(--color-bg-hover)'"
          @mouseleave="($event.target as HTMLElement).style.backgroundColor = 'transparent'"
        >
          <!-- 最大化状态：显示还原图标（两个重叠方块） -->
          <svg v-if="isMaximized" width="10" height="10" viewBox="0 0 10 10">
            <rect x="2.5" y="0" width="7" height="7" fill="none" stroke="currentColor" stroke-width="1"/>
            <rect x="0" y="2.5" width="7" height="7" fill="var(--color-bg-card)" stroke="currentColor" stroke-width="1"/>
          </svg>
          <!-- 正常状态：显示最大化图标（单方块） -->
          <svg v-else width="10" height="10" viewBox="0 0 10 10">
            <rect x="0.5" y="0.5" width="9" height="9" fill="none" stroke="currentColor" stroke-width="1"/>
          </svg>
        </button>
        <!-- 关闭 -->
        <button
          class="inline-flex items-center justify-center transition-colors"
          style="width: 46px; height: var(--titlebar-height); color: var(--color-text-muted);"
          @click="close" title="关闭"
          @mouseenter="($event.target as HTMLElement).style.backgroundColor = '#e81123'; ($event.target as HTMLElement).style.color = 'white'"
          @mouseleave="($event.target as HTMLElement).style.backgroundColor = 'transparent'; ($event.target as HTMLElement).style.color = 'var(--color-text-muted)'"
        >
          <svg width="10" height="10" viewBox="0 0 10 10">
            <line x1="0" y1="0" x2="10" y2="10" stroke="currentColor" stroke-width="1.2"/>
            <line x1="10" y1="0" x2="0" y2="10" stroke="currentColor" stroke-width="1.2"/>
          </svg>
        </button>
      </div>
    </template>
  </div>
</template>
