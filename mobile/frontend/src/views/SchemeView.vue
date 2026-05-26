<script setup lang="ts">
import { Icon } from '@iconify/vue'
import { useSchemeStore } from '@/stores/scheme'
import { useConnectionStore } from '@/stores/connection'
import { computed } from 'vue'
import { moduleLoader } from '@/runtime/ModuleLoader'

const schemeStore = useSchemeStore()
const connectionStore = useConnectionStore()

const currentPage = computed(() => schemeStore.currentPage)
const hasScheme = computed(() => schemeStore.isSchemeLoaded)
const totalPages = computed(() => schemeStore.layout?.pages.length ?? 0)
const currentPageIndex = computed(() => schemeStore.currentPageIndex + 1)

function handleSlotClick(pluginId: string, instanceId: string) {
  // 插件交互事件转发到桌面端
  connectionStore.send({
    type: 'event_report',
    deviceId: connectionStore.deviceId,
    pluginId,
    payload: {
      pluginId,
      instanceId,
      eventType: 'click',
      eventData: {},
    },
  })
}
</script>

<template>
  <div class="h-screen flex flex-col">
    <!-- 顶部状态栏 -->
    <header class="shrink-0 flex items-center justify-between px-4 py-2 bg-gray-900/80 backdrop-blur-sm border-b border-gray-800">
      <div class="flex items-center gap-2">
        <Icon icon="solar:widget-bold" class="text-indigo-400" />
        <span class="text-sm font-semibold">{{ schemeStore.schemeName || 'OneDeck' }}</span>
      </div>
      <div class="flex items-center gap-3">
        <span v-if="totalPages > 1" class="text-xs text-gray-400">
          {{ currentPageIndex }} / {{ totalPages }}
        </span>
        <span
          class="w-2 h-2 rounded-full"
          :class="connectionStore.connected ? 'bg-emerald-400' : 'bg-red-400'"
        />
      </div>
    </header>

    <!-- 方案内容区 -->
    <main class="flex-1 overflow-y-auto p-4">
      <div v-if="!hasScheme" class="h-full flex flex-col items-center justify-center text-gray-500">
        <Icon icon="solar:widget-bold" class="text-5xl mb-4" />
        <p class="text-lg font-semibold">等待方案</p>
        <p class="text-sm mt-1">请在桌面端设计并推送方案</p>
      </div>

      <div v-else-if="currentPage" class="h-full">
        <!-- Grid 布局渲染 -->
        <div
          v-if="schemeStore.layout?.type === 'grid'"
          class="grid gap-3 h-full"
          :style="{
            gridTemplateColumns: `repeat(${schemeStore.layout?.columns}, 1fr)`,
            gridTemplateRows: `repeat(${schemeStore.layout?.rows}, 1fr)`,
          }"
        >
          <!-- 空白格位 -->
          <template v-for="row in (schemeStore.layout?.rows ?? 0)" :key="row">
            <template v-for="col in (schemeStore.layout?.columns ?? 0)" :key="`${row}-${col}`">
              <div
                v-if="!currentPage.slots.find(s => s.row === row - 1 && s.column === col - 1)"
                class="bg-gray-900/50 border border-gray-800/50 rounded-xl"
              />
            </template>
          </template>

          <!-- 插件格位 -->
          <div
            v-for="slot in currentPage.slots"
            :key="slot.id"
            class="bg-gray-900 border border-gray-800 rounded-xl overflow-hidden cursor-pointer active:scale-95 transition-transform"
            :style="{
              gridRow: `${slot.row + 1} / span ${slot.rowSpan}`,
              gridColumn: `${slot.column + 1} / span ${slot.columnSpan}`,
            }"
            @click="handleSlotClick(slot.pluginId, slot.id)"
          >
            <div class="h-full flex items-center justify-center text-gray-500">
              <Icon icon="solar:widget-2-bold" class="text-2xl" />
            </div>
          </div>
        </div>
      </div>
    </main>

    <!-- 底部分页指示器 -->
    <footer v-if="totalPages > 1" class="shrink-0 flex items-center justify-center gap-2 py-3 bg-gray-900/80 backdrop-blur-sm border-t border-gray-800">
      <button
        class="w-8 h-8 flex items-center justify-center rounded-lg bg-gray-800 hover:bg-gray-700 transition-colors"
        :class="{ 'opacity-30': currentPageIndex <= 1 }"
        :disabled="currentPageIndex <= 1"
        @click="schemeStore.prevPage()"
      >
        <Icon icon="solar:alt-arrow-left-bold" class="text-sm" />
      </button>

      <div class="flex gap-1.5">
        <button
          v-for="(_, idx) in schemeStore.layout?.pages"
          :key="idx"
          class="w-2 h-2 rounded-full transition-colors"
          :class="idx === schemeStore.currentPageIndex ? 'bg-indigo-400' : 'bg-gray-600'"
          @click="schemeStore.goToPage(idx)"
        />
      </div>

      <button
        class="w-8 h-8 flex items-center justify-center rounded-lg bg-gray-800 hover:bg-gray-700 transition-colors"
        :class="{ 'opacity-30': currentPageIndex >= totalPages }"
        :disabled="currentPageIndex >= totalPages"
        @click="schemeStore.nextPage()"
      >
        <Icon icon="solar:alt-arrow-right-bold" class="text-sm" />
      </button>
    </footer>
  </div>
</template>
