import { defineStore } from 'pinia'
import { ref } from 'vue'

export type NotificationType = 'success' | 'error' | 'warning' | 'info'

export interface Notification {
  id: string
  type: NotificationType
  title: string
  message?: string
  duration: number
}

export const useNotificationStore = defineStore('notification', () => {
  const notifications = ref<Notification[]>([])

  function show(type: NotificationType, title: string, message?: string, duration = 3000) {
    const id = crypto.randomUUID().slice(0, 8)
    notifications.value.push({ id, type, title, message, duration })

    // 自动移除
    if (duration > 0) {
      setTimeout(() => remove(id), duration)
    }
  }

  function remove(id: string) {
    notifications.value = notifications.value.filter(n => n.id !== id)
  }

  function success(title: string, message?: string) { show('success', title, message) }
  function error(title: string, message?: string) { show('error', title, message, 5000) }
  function warning(title: string, message?: string) { show('warning', title, message, 4000) }
  function info(title: string, message?: string) { show('info', title, message) }

  return { notifications, show, remove, success, error, warning, info }
})
