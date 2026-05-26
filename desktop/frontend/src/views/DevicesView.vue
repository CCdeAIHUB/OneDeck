<script setup lang="ts">
import { Icon } from '@iconify/vue'
import PageHeader from '@/components/PageHeader.vue'
import DeviceCard from '@/components/DeviceCard.vue'
import { useDeviceStore } from '@/stores/devices'
import { useRouter } from 'vue-router'

const deviceStore = useDeviceStore()
const router = useRouter()

function goToDetail(deviceId: string) {
  router.push(`/devices/${deviceId}`)
}

function assignScheme(deviceId: string) {
  // TODO: 打开方案分配弹窗
}

function disconnect(deviceId: string) {
  // TODO: 调用后端断开设备
}
</script>

<template>
  <div>
    <PageHeader title="设备管理" subtitle="管理已连接的移动端设备" icon="solar:smartphone-bold">
      <template #actions>
        <button class="btn-primary">
          <Icon icon="solar:refresh-bold" class="text-base" />
          扫描设备
        </button>
      </template>
    </PageHeader>

    <div v-if="deviceStore.devices.length === 0" class="text-center py-20">
      <Icon icon="solar:smartphone-bold" class="text-6xl text-gray-700 mb-4 mx-auto block" />
      <h3 class="text-lg font-semibold text-gray-400 mb-2">暂无设备连接</h3>
      <p class="text-sm text-gray-500">在移动设备上打开 OneDesk 应用，输入桌面端地址进行连接</p>
      <p class="text-sm text-gray-600 mt-1">桌面端地址：ws://localhost:9720</p>
    </div>

    <div v-else class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
      <DeviceCard
        v-for="device in deviceStore.devices"
        :key="device.deviceId"
        :device="device"
        @click="goToDetail"
        @assign-scheme="assignScheme"
        @disconnect="disconnect"
      />
    </div>
  </div>
</template>
