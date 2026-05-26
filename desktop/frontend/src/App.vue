<script setup lang="ts">
import TitleBar from '@/components/TitleBar.vue'
import Sidebar from '@/components/Sidebar.vue'
import { useThemeStore } from '@/stores/theme'
import { onMounted } from 'vue'

const themeStore = useThemeStore()

onMounted(() => {
  themeStore.initTheme()
})
</script>

<template>
  <div class="flex flex-col h-screen" :class="themeStore.getEffectiveTheme()">
    <!-- 自绘标题栏 -->
    <TitleBar />

    <div class="flex flex-1 overflow-hidden">
      <!-- 侧边导航栏 -->
      <Sidebar />

      <!-- 主内容区 -->
      <main class="flex-1 overflow-y-auto p-6" style="background-color: var(--color-bg);">
        <router-view />
      </main>
    </div>
  </div>
</template>
