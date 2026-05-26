<script setup lang="ts">
import { Icon } from '@iconify/vue'
import PageHeader from '@/components/PageHeader.vue'
import { useDeviceStore } from '@/stores/devices'
import { useRoute, useRouter } from 'vue-router'
import { computed } from 'vue'

const route = useRoute()
const router = useRouter()
const deviceStore = useDeviceStore()

const device = computed(() =>
  deviceStore.devices.find((d) => d.deviceId === route.params.id)
)

function goBack() {
  router.push('/devices')
}
</script>

<template>
  <div v-if="device">
    <PageHeader
      :title="device.deviceName"
      :subtitle="`${device.platform} · ${device.osVersion}`"
      icon="solar:smartphone-bold"
    >
      <template #actions>
        <button
          class="px-4 py-2 bg-gray-700 hover:bg-gray-600 rounded-lg text-sm transition-colors"
          @click="goBack"
        >
          返回设备列表
        </button>
      </template>
    </PageHeader>

    <!-- 设备信息 -->
    <div class="grid grid-cols-2 gap-6">
      <div class="bg-gray-900 border border-gray-800 rounded-xl p-5">
        <h3 class="font-semibold mb-4">设备信息</h3>
        <div class="space-y-3 text-sm">
          <div class="flex justify-between">
            <span class="text-gray-400">设备ID</span>
            <span class="font-mono">{{ device.deviceId }}</span>
          </div>
          <div class="flex justify-between">
            <span class="text-gray-400">平台</span>
            <span>{{ device.platform }}</span>
          </div>
          <div class="flex justify-between">
            <span class="text-gray-400">系统版本</span>
            <span>{{ device.osVersion }}</span>
          </div>
          <div class="flex justify-between">
            <span class="text-gray-400">屏幕分辨率</span>
            <span>{{ device.screenWidth }}×{{ device.screenHeight }}</span>
          </div>
          <div class="flex justify-between">
            <span class="text-gray-400">状态</span>
            <span
              class="px-2 py-0.5 text-xs rounded-full"
              :class="device.isOnline ? 'bg-emerald-500/20 text-emerald-400' : 'bg-gray-700 text-gray-500'"
            >
              {{ device.isOnline ? '在线' : '离线' }}
            </span>
          </div>
          <div class="flex justify-between">
            <span class="text-gray-400">连接时间</span>
            <span>{{ new Date(device.connectedAt).toLocaleString() }}</span>
          </div>
        </div>
      </div>

      <div class="bg-gray-900 border border-gray-800 rounded-xl p-5">
        <h3 class="font-semibold mb-4">方案</h3>
        <div v-if="device.assignedSchemeId" class="text-sm">
          <p>已分配方案：{{ device.assignedSchemeId }}</p>
          <button class="mt-3 px-4 py-2 bg-indigo-600 hover:bg-indigo-500 rounded-lg text-sm transition-colors">
            编辑方案
          </button>
        </div>
        <div v-else class="text-center py-8 text-gray-500">
          <Icon icon="solar:widget-bold" class="text-3xl mb-2 mx-auto block" />
          <p class="text-sm">尚未分配方案</p>
          <button class="mt-3 px-4 py-2 bg-indigo-600 hover:bg-indigo-500 rounded-lg text-sm transition-colors">
            创建方案
          </button>
        </div>
      </div>
    </div>
  </div>

  <div v-else class="text-center py-20 text-gray-500">
    <p>设备未找到</p>
    <button
      class="mt-4 px-4 py-2 bg-gray-700 hover:bg-gray-600 rounded-lg text-sm transition-colors"
      @click="goBack"
    >
      返回设备列表
    </button>
  </div>
</template>
