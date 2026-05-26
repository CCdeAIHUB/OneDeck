<script setup lang="ts">
import { Icon } from '@iconify/vue'
import { useRoute } from 'vue-router'
import { computed } from 'vue'
import { useThemeStore } from '@/stores/theme'

const route = useRoute()
const themeStore = useThemeStore()

interface NavItem {
  name: string
  icon: string
  iconActive: string
  path: string
}

const navItems: NavItem[] = [
  { name: '仪表盘', icon: 'solar:home-smile-linear', iconActive: 'solar:home-smile-bold', path: '/dashboard' },
  { name: '设备', icon: 'solar:smartphone-linear', iconActive: 'solar:smartphone-bold', path: '/devices' },
  { name: '页面', icon: 'solar:clipboard-list-linear', iconActive: 'solar:clipboard-list-bold', path: '/pages' },
  { name: '组件', icon: 'solar:widget-2-linear', iconActive: 'solar:widget-2-bold', path: '/components' },
  { name: '方案', icon: 'solar:layers-linear', iconActive: 'solar:layers-bold', path: '/schemes' },
  { name: '插件', icon: 'solar:puzzle-linear', iconActive: 'solar:puzzle-bold', path: '/plugins' },
  { name: '参数库', icon: 'solar:database-linear', iconActive: 'solar:database-bold', path: '/shared-params' },
  { name: '设置', icon: 'solar:settings-linear', iconActive: 'solar:settings-bold', path: '/settings' },
]

const isActive = (path: string) => {
  if (path === '/dashboard' && route.path === '/') return true
  return route.path.startsWith(path)
}
</script>

<template>
  <nav class="w-14 flex flex-col items-center py-3 gap-0.5 shrink-0 border-r" style="background-color: var(--color-bg-card); border-color: var(--color-border-subtle);">
    <!-- Logo -->
    <div class="w-8 h-8 flex items-center justify-center mb-3 rounded-lg" style="background-color: var(--color-primary);">
      <Icon icon="solar:widget-bold" class="text-white text-base" />
    </div>

    <router-link
      v-for="item in navItems"
      :key="item.path"
      :to="item.path"
      class="group relative w-10 h-10 flex items-center justify-center rounded-lg transition-all duration-200"
      :class="isActive(item.path) ? '' : 'opacity-60 hover:opacity-90'"
      :style="isActive(item.path) ? 'background-color: var(--color-primary); color: white;' : 'color: var(--color-text-muted);'"
      :title="item.name"
    >
      <Icon
        :icon="isActive(item.path) ? item.iconActive : item.icon"
        class="text-lg"
      />
      <!-- Tooltip -->
      <span
        class="absolute left-full ml-2 px-2 py-1 text-xs rounded whitespace-nowrap opacity-0 group-hover:opacity-100 transition-opacity pointer-events-none z-50"
        style="background-color: var(--color-bg-surface); color: var(--color-text);"
      >
        {{ item.name }}
      </span>
    </router-link>

    <!-- 底部主题切换 -->
    <div class="mt-auto pt-3">
      <button
        class="w-10 h-10 flex items-center justify-center rounded-lg transition-all duration-200 opacity-60 hover:opacity-90"
        style="color: var(--color-text-muted);"
        @click="themeStore.setMode(themeStore.getEffectiveTheme() === 'dark' ? 'light' : 'dark')"
        :title="themeStore.getEffectiveTheme() === 'dark' ? '切换浅色模式' : '切换深色模式'"
      >
        <Icon :icon="themeStore.getEffectiveTheme() === 'dark' ? 'solar:sun-bold' : 'solar:moon-bold'" class="text-lg" />
      </button>
    </div>
  </nav>
</template>
