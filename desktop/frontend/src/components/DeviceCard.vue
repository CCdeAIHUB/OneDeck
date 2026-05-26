<script setup lang="ts">
import { Icon } from '@iconify/vue'
import type { DeviceInfo } from '@/stores/devices'

defineProps<{
  device: DeviceInfo
}>()

defineEmits<{
  (e: 'click', deviceId: string): void
  (e: 'assign-scheme', deviceId: string): void
  (e: 'disconnect', deviceId: string): void
}>()

const platformIcon: Record<string, string> = {
  android: 'solar:smartphone-bold',
  ios: 'solar:smartphone-bold',
  harmony: 'solar:smartphone-bold',
}
</script>

<template>
  <div
    class="bg-gray-900 border border-gray-800 rounded-xl p-4 cursor-pointer hover:border-indigo-500/50 transition-all duration-200"
    @click="$emit('click', device.deviceId)"
  >
    <div class="flex items-start justify-between mb-3">
      <div class="flex items-center gap-3">
        <div
          class="w-10 h-10 rounded-lg flex items-center justify-center"
          :class="device.isOnline ? 'bg-emerald-500/20 text-emerald-400' : 'bg-gray-700 text-gray-500'"
        >
          <Icon :icon="platformIcon[device.platform] || 'solar:smartphone-bold'" class="text-xl" />
        </div>
        <div>
          <h3 class="font-semibold text-sm">{{ device.deviceName }}</h3>
          <p class="text-xs text-gray-400">{{ device.platform }} · {{ device.osVersion }}</p>
        </div>
      </div>
      <span
        class="px-2 py-0.5 text-xs rounded-full"
        :class="device.isOnline ? 'bg-emerald-500/20 text-emerald-400' : 'bg-gray-700 text-gray-500'"
      >
        {{ device.isOnline ? '在线' : '离线' }}
      </span>
    </div>

    <div class="flex items-center justify-between text-xs text-gray-500">
      <span>{{ device.screenWidth }}×{{ device.screenHeight }}</span>
      <span>{{ device.assignedSchemeId ? '已分配方案' : '未分配方案' }}</span>
    </div>

    <div class="flex gap-2 mt-3">
      <button
        class="flex-1 px-3 py-1.5 text-xs bg-indigo-600 hover:bg-indigo-500 rounded-lg transition-colors"
        @click.stop="$emit('assign-scheme', device.deviceId)"
      >
        分配方案
      </button>
      <button
        class="px-3 py-1.5 text-xs bg-gray-700 hover:bg-gray-600 rounded-lg transition-colors"
        @click.stop="$emit('disconnect', device.deviceId)"
      >
        断开
      </button>
    </div>
  </div>
</template>
