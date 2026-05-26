<script setup lang="ts">
import { Icon } from '@iconify/vue'

defineProps<{
  title: string
  message: string
  confirmText?: string
  cancelText?: string
  danger?: boolean
}>()

const emit = defineEmits<{
  confirm: []
  cancel: []
}>()
</script>

<template>
  <div class="dialog-overlay" @click.self="emit('cancel')">
    <div class="dialog-card space-y-4">
      <div class="flex items-center gap-3">
        <div v-if="danger" class="w-10 h-10 rounded-full flex items-center justify-center shrink-0" style="background-color: rgba(239,68,68,0.15);">
          <Icon icon="solar:danger-triangle-bold" class="text-xl text-red-400" />
        </div>
        <div v-else class="w-10 h-10 rounded-full flex items-center justify-center shrink-0" style="background-color: rgba(59,130,246,0.15);">
          <Icon icon="solar:question-circle-bold" class="text-xl" style="color: var(--color-primary);" />
        </div>
        <h3 class="text-lg font-bold">{{ title }}</h3>
      </div>
      <p class="text-sm" style="color: var(--color-text-muted);">{{ message }}</p>
      <div class="flex gap-3 justify-end">
        <button class="btn-secondary" @click="emit('cancel')">{{ cancelText ?? '取消' }}</button>
        <button
          class="px-4 py-2 rounded-lg text-sm text-white transition-colors"
          :style="danger ? 'background-color: #ef4444;' : 'background-color: var(--color-primary);'"
          @mouseenter="($event.target as HTMLElement).style.opacity = '0.85'"
          @mouseleave="($event.target as HTMLElement).style.opacity = '1'"
          @click="emit('confirm')"
        >
          {{ confirmText ?? '确定' }}
        </button>
      </div>
    </div>
  </div>
</template>
