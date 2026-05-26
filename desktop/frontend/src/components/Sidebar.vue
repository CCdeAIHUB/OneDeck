<script setup lang="ts">
import { Icon } from '@iconify/vue'
import { useRoute } from 'vue-router'
import { computed, ref } from 'vue'

const route = useRoute()

interface NavItem {
  name: string
  icon: string
  iconActive: string
  path: string
}

const navItems: NavItem[] = [
  { name: '仪表盘', icon: 'solar:home-linear', iconActive: 'solar:home-bold', path: '/dashboard' },
  { name: '设备', icon: 'solar:smartphone-linear', iconActive: 'solar:smartphone-bold', path: '/devices' },
  { name: '页面', icon: 'solar:document-linear', iconActive: 'solar:document-bold', path: '/pages' },
  { name: '组件', icon: 'solar:widget-2-linear', iconActive: 'solar:widget-2-bold', path: '/components' },
  { name: '方案', icon: 'solar:widget-linear', iconActive: 'solar:widget-bold', path: '/schemes' },
  { name: '插件', icon: 'solar:plugin-linear', iconActive: 'solar:plugin-bold', path: '/plugins' },
  { name: '日志', icon: 'solar:document-text-linear', iconActive: 'solar:document-text-bold', path: '/logs' },
  { name: '设置', icon: 'solar:settings-linear', iconActive: 'solar:settings-bold', path: '/settings' },
]

const isActive = (path: string) => {
  if (path === '/dashboard' && route.path === '/') return true
  return route.path.startsWith(path)
}
</script>

<template>
  <nav class="w-16 bg-gray-900 border-r border-gray-800 flex flex-col items-center py-4 gap-1 shrink-0">
    <router-link
      v-for="item in navItems"
      :key="item.path"
      :to="item.path"
      class="group relative w-10 h-10 flex items-center justify-center rounded-lg transition-all duration-200"
      :class="isActive(item.path) ? 'bg-indigo-600 text-white' : 'text-gray-400 hover:bg-gray-800 hover:text-indigo-300'"
      :title="item.name"
    >
      <!-- 默认：linear | 选中：bold | Hover：bold-duotone -->
      <Icon
        :icon="isActive(item.path) ? item.iconActive : item.icon"
        class="text-xl"
      />

      <!-- Tooltip -->
      <span
        class="absolute left-full ml-2 px-2 py-1 bg-gray-800 text-xs text-white rounded whitespace-nowrap opacity-0 group-hover:opacity-100 transition-opacity pointer-events-none z-50"
      >
        {{ item.name }}
      </span>
    </router-link>
  </nav>
</template>
