<script setup lang="ts">
import { Icon } from '@iconify/vue'
import { useNotificationStore, type NotificationType } from '@/stores/notification'

const notificationStore = useNotificationStore()

const iconMap: Record<NotificationType, string> = {
  success: 'solar:check-circle-bold',
  error: 'solar:close-circle-bold',
  warning: 'solar:danger-triangle-bold',
  info: 'solar:info-circle-bold',
}

const colorMap: Record<NotificationType, { bg: string; border: string; icon: string }> = {
  success: { bg: '#10b98120', border: '#10b98140', icon: '#10b981' },
  error: { bg: '#ef444420', border: '#ef444440', icon: '#ef4444' },
  warning: { bg: '#f59e0b20', border: '#f59e0b40', icon: '#f59e0b' },
  info: { bg: '#3b82f620', border: '#3b82f640', icon: '#3b82f6' },
}
</script>

<template>
  <div class="fixed top-10 right-4 z-[9999] flex flex-col gap-2 pointer-events-none" style="max-width: 360px;">
    <TransitionGroup name="notification">
      <div
        v-for="n in notificationStore.notifications"
        :key="n.id"
        class="pointer-events-auto rounded-lg px-4 py-3 flex items-start gap-3 shadow-lg cursor-pointer"
        :style="{
          backgroundColor: `var(--color-bg-card)`,
          border: `1px solid ${colorMap[n.type].border}`,
          borderLeft: `3px solid ${colorMap[n.type].icon}`,
        }"
        @click="notificationStore.remove(n.id)"
      >
        <Icon :icon="iconMap[n.type]" class="text-lg shrink-0 mt-0.5" :style="{ color: colorMap[n.type].icon }" />
        <div class="flex-1 min-w-0">
          <p class="text-sm font-semibold" style="color: var(--color-text);">{{ n.title }}</p>
          <p v-if="n.message" class="text-xs mt-0.5" style="color: var(--color-text-muted);">{{ n.message }}</p>
        </div>
        <button class="shrink-0" style="color: var(--color-text-dim);" @click.stop="notificationStore.remove(n.id)">
          <Icon icon="solar:close-circle-bold" class="text-sm" />
        </button>
      </div>
    </TransitionGroup>
  </div>
</template>

<style>
.notification-enter-active {
  animation: slideInRight 0.3s ease-out;
}
.notification-leave-active {
  animation: slideOutRight 0.25s ease-in;
}

@keyframes slideInRight {
  from {
    transform: translateX(100%);
    opacity: 0;
  }
  to {
    transform: translateX(0);
    opacity: 1;
  }
}

@keyframes slideOutRight {
  from {
    transform: translateX(0);
    opacity: 1;
  }
  to {
    transform: translateX(100%);
    opacity: 0;
  }
}
</style>
