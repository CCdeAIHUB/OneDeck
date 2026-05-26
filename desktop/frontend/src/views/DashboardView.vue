<script setup lang="ts">
import { Icon } from '@iconify/vue'
import PageHeader from '@/components/PageHeader.vue'
import { useDeviceStore } from '@/stores/devices'
import { usePluginStore } from '@/stores/plugins'
import { useLogStore } from '@/stores/logs'
import { computed } from 'vue'

const deviceStore = useDeviceStore()
const pluginStore = usePluginStore()
const logStore = useLogStore()

const stats = computed(() => ({
  devices: deviceStore.devices.length,
  online: deviceStore.onlineDevices.length,
  plugins: pluginStore.plugins.length,
  errors: logStore.levelCounts.error,
}))
</script>

<template>
  <div>
    <PageHeader title="仪表盘" subtitle="系统概览" icon="solar:home-bold" />

    <!-- 统计卡片 -->
    <div class="grid grid-cols-4 gap-4 mb-6">
      <div class="bg-gray-900 border border-gray-800 rounded-xl p-4">
        <div class="flex items-center gap-3">
          <div class="w-10 h-10 rounded-lg bg-indigo-500/20 flex items-center justify-center">
            <Icon icon="solar:smartphone-bold" class="text-indigo-400 text-xl" />
          </div>
          <div>
            <p class="text-2xl font-bold">{{ stats.devices }}</p>
            <p class="text-xs text-gray-400">已连接设备</p>
          </div>
        </div>
      </div>

      <div class="bg-gray-900 border border-gray-800 rounded-xl p-4">
        <div class="flex items-center gap-3">
          <div class="w-10 h-10 rounded-lg bg-emerald-500/20 flex items-center justify-center">
            <Icon icon="solar:shield-check-bold" class="text-emerald-400 text-xl" />
          </div>
          <div>
            <p class="text-2xl font-bold">{{ stats.online }}</p>
            <p class="text-xs text-gray-400">在线设备</p>
          </div>
        </div>
      </div>

      <div class="bg-gray-900 border border-gray-800 rounded-xl p-4">
        <div class="flex items-center gap-3">
          <div class="w-10 h-10 rounded-lg bg-amber-500/20 flex items-center justify-center">
            <Icon icon="solar:widget-2-bold" class="text-amber-400 text-xl" />
          </div>
          <div>
            <p class="text-2xl font-bold">{{ stats.plugins }}</p>
            <p class="text-xs text-gray-400">已安装插件</p>
          </div>
        </div>
      </div>

      <div class="bg-gray-900 border border-gray-800 rounded-xl p-4">
        <div class="flex items-center gap-3">
          <div class="w-10 h-10 rounded-lg bg-red-500/20 flex items-center justify-center">
            <Icon icon="solar:danger-triangle-bold" class="text-red-400 text-xl" />
          </div>
          <div>
            <p class="text-2xl font-bold">{{ stats.errors }}</p>
            <p class="text-xs text-gray-400">错误日志</p>
          </div>
        </div>
      </div>
    </div>

    <!-- 最近设备 -->
    <div class="mb-6">
      <h2 class="text-lg font-semibold mb-3">最近设备</h2>
      <div v-if="deviceStore.devices.length === 0" class="text-center py-12 text-gray-500">
        <Icon icon="solar:smartphone-bold" class="text-4xl mb-2 mx-auto block" />
        <p>暂无已连接设备</p>
        <p class="text-sm">在移动端打开 OneDeck 并连接到此桌面端</p>
      </div>
      <div v-else class="grid grid-cols-3 gap-4">
        <!-- DeviceCard 组件会在此处渲染 -->
      </div>
    </div>

    <!-- 最近日志 -->
    <div>
      <h2 class="text-lg font-semibold mb-3">最近日志</h2>
      <div class="bg-gray-900 border border-gray-800 rounded-xl p-4 max-h-64 overflow-y-auto">
        <div v-if="logStore.logs.length === 0" class="text-center py-6 text-gray-500 text-sm">
          暂无日志
        </div>
        <div v-else class="space-y-1 font-mono text-xs">
          <div
            v-for="log in logStore.logs.slice(-20)"
            :key="log.id"
            class="flex gap-2 py-0.5"
          >
            <span
              class="w-10 text-right font-bold"
              :class="{
                'text-gray-500': log.level === 'debug',
                'text-blue-400': log.level === 'info',
                'text-amber-400': log.level === 'warn',
                'text-red-400': log.level === 'error',
              }"
            >
              {{ log.level.toUpperCase() }}
            </span>
            <span class="text-gray-500">{{ log.timestamp.slice(11, 19) }}</span>
            <span class="text-gray-400">[{{ log.source }}]</span>
            <span>{{ log.message }}</span>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
