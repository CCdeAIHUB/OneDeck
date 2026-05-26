import { defineStore } from 'pinia'
import { ref, computed } from 'vue'

export interface DeviceInfo {
  deviceId: string
  deviceName: string
  platform: 'android' | 'ios' | 'harmony'
  osVersion: string
  appId: string
  screenWidth: number
  screenHeight: number
  connectedAt: number
  lastHeartbeat: number
  assignedSchemeId: string | null
  isOnline: boolean
}

export const useDeviceStore = defineStore('devices', () => {
  const devices = ref<DeviceInfo[]>([])
  const selectedDeviceId = ref<string | null>(null)

  const onlineDevices = computed(() => devices.value.filter((d) => d.isOnline))
  const selectedDevice = computed(() =>
    devices.value.find((d) => d.deviceId === selectedDeviceId.value)
  )

  function addDevice(device: DeviceInfo) {
    const idx = devices.value.findIndex((d) => d.deviceId === device.deviceId)
    if (idx >= 0) {
      devices.value[idx] = device
    } else {
      devices.value.push(device)
    }
  }

  function removeDevice(deviceId: string) {
    devices.value = devices.value.filter((d) => d.deviceId !== deviceId)
    if (selectedDeviceId.value === deviceId) {
      selectedDeviceId.value = null
    }
  }

  function updateHeartbeat(deviceId: string) {
    const device = devices.value.find((d) => d.deviceId === deviceId)
    if (device) {
      device.lastHeartbeat = Date.now()
      device.isOnline = true
    }
  }

  function setDeviceOffline(deviceId: string) {
    const device = devices.value.find((d) => d.deviceId === deviceId)
    if (device) {
      device.isOnline = false
    }
  }

  function selectDevice(deviceId: string | null) {
    selectedDeviceId.value = deviceId
  }

  return {
    devices,
    selectedDeviceId,
    onlineDevices,
    selectedDevice,
    addDevice,
    removeDevice,
    updateHeartbeat,
    setDeviceOffline,
    selectDevice,
  }
})
