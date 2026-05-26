<script setup lang="ts">
import TitleBar from '@/components/TitleBar.vue'
import Sidebar from '@/components/Sidebar.vue'
import { useRoute } from 'vue-router'

const route = useRoute()
</script>

<template>
  <div class="flex flex-col h-screen">
    <!-- 自绘标题栏 -->
    <TitleBar />

    <div class="flex flex-1 overflow-hidden">
      <!-- 侧边导航栏 -->
      <Sidebar />

      <!-- 主内容区 -->
      <main class="flex-1 overflow-y-auto bg-gray-950 p-6">
        <router-view />
      </main>
    </div>
  </div>
</template>
