<script setup lang="ts">
import { Icon } from '@iconify/vue'
import { useRoute, useRouter } from 'vue-router'
import { computed } from 'vue'

const route = useRoute()
const router = useRouter()

interface NavItem {
  name: string
  icon: string
  iconHover: string
  path: string
}

const navItems: NavItem[] = [
  { name: '仪表盘', icon: 'solar:home-bold', iconHover: 'solar:home-bold-duotone', path: '/dashboard' },
  { name: '设备', icon: 'solar:smartphone-bold', iconHover: 'solar:smartphone-bold-duotone', path: '/devices' },
  { name: '方案', icon: 'solar:widget-bold', iconHover: 'solar:widget-bold-duotone', path: '/schemes' },
  { name: '插件', icon: 'solar:widget-2-bold', iconHover: 'solar:widget-2-bold-duotone', path: '/plugins' },
  { name: '日志', icon: 'solar:document-text-bold', iconHover: 'solar:document-text-bold-duotone', path: '/logs' },
  { name: '设置', icon: 'solar:settings-bold', iconHover: 'solar:settings-bold-duotone', path: '/settings' },
]

const isActive = (path: string) => route.path.startsWith(path)
</script>

<template>
  <nav class="w-16 bg-gray-900 border-r border-gray-800 flex flex-col items-center py-4 gap-2 shrink-0">
    <router-link
      v-for="item in navItems"
      :key="item.path"
      :to="item.path"
      class="group relative w-10 h-10 flex items-center justify-center rounded-lg transition-all duration-200"
      :class="isActive(item.path) ? 'bg-indigo-600 text-white' : 'text-gray-400 hover:bg-gray-800 hover:text-white'"
      :title="item.name"
    >
      <!-- 选中：bold / Hover：bold-duotone -->
      <Icon
        :icon="isActive(item.path) ? item.icon : item.iconHover"
        class="text-xl opacity-0 absolute transition-opacity"
        :class="{ 'opacity-100': isActive(item.path) }"
      />
      <Icon
        :icon="item.iconHover"
        class="text-xl opacity-0 absolute transition-opacity group-hover:opacity-100"
        :class="{ '!opacity-0': isActive(item.path) }"
      />
      <Icon
        :icon="item.icon"
        class="text-xl transition-opacity"
        :class="{ 'opacity-0': isActive(item.path) || $el?.matches(':hover') }"
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
