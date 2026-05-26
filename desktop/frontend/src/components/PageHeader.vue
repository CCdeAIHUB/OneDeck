<script setup lang="ts">
import { Icon } from '@iconify/vue'

defineProps<{
  title: string
  subtitle?: string
  icon?: string
}>()

defineEmits<{
  (e: 'action', action: string): void
}>()
</script>

<template>
  <header class="flex items-center justify-between mb-6 shrink-0">
    <div class="flex items-center gap-3">
      <Icon v-if="icon" :icon="icon" class="text-2xl" style="color: var(--color-primary);" />
      <div>
        <h1 class="text-xl font-bold">{{ title }}</h1>
        <p v-if="subtitle" class="text-sm" style="color: var(--color-text-muted);">{{ subtitle }}</p>
      </div>
    </div>

    <div class="flex items-center gap-2">
      <slot name="actions" />
    </div>
  </header>
</template>
