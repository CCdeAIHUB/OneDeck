<script setup lang="ts">
import { Icon } from '@iconify/vue'
import PageHeader from '@/components/PageHeader.vue'
import { useDesignStore } from '@/stores/design'
import { useRouter } from 'vue-router'

const designStore = useDesignStore()
const router = useRouter()

function createNew() {
  const page = designStore.createPage()
  router.push(`/pages/${page.id}/designer`)
}

function editPage(id: string) {
  router.push(`/pages/${id}/designer`)
}

function deletePage(id: string) {
  designStore.deletePage(id)
}
</script>

<template>
  <div>
    <PageHeader title="页面" subtitle="设计移动端页面布局" icon="solar:document-bold">
      <template #actions>
        <button
          class="flex items-center gap-2 px-4 py-2 bg-indigo-600 hover:bg-indigo-500 rounded-lg text-sm transition-colors"
          @click="createNew"
        >
          <Icon icon="solar:add-circle-bold" class="text-base" />
          新建页面
        </button>
      </template>
    </PageHeader>

    <div v-if="designStore.pages.length === 0" class="text-center py-20">
      <Icon icon="solar:document-bold" class="text-6xl text-gray-700 mb-4 mx-auto block" />
      <h3 class="text-lg font-semibold text-gray-400 mb-2">暂无页面</h3>
      <p class="text-sm text-gray-500">创建一个页面，为移动端设计界面布局</p>
      <button
        class="mt-4 px-4 py-2 bg-indigo-600 hover:bg-indigo-500 rounded-lg text-sm transition-colors"
        @click="createNew"
      >
        创建第一个页面
      </button>
    </div>

    <div v-else class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
      <div
        v-for="page in designStore.pages"
        :key="page.id"
        class="bg-gray-900 border border-gray-800 rounded-xl p-5 hover:border-indigo-500/50 transition-all duration-200 cursor-pointer group"
        @click="editPage(page.id)"
      >
        <div class="flex items-start justify-between mb-3">
          <h3 class="font-semibold">{{ page.name }}</h3>
          <span class="text-xs text-gray-500">{{ page.orientation === 'vertical' ? '竖屏' : '横屏' }}</span>
        </div>

        <div class="text-xs text-gray-400 space-y-1 mb-4">
          <p>格子：{{ page.rows }} × {{ page.columns }}</p>
          <p>组件数：{{ page.cells.filter(c => c.componentId).length }} / {{ page.cells.length }}</p>
          <p v-if="page.background.type === 'color'">背景：{{ page.background.color }}</p>
          <p v-else-if="page.background.type === 'image'">背景：图片</p>
          <p v-else>背景：视频</p>
        </div>

        <!-- 格子预览 -->
        <div
          class="rounded-lg overflow-hidden border border-gray-700 mb-3"
          :style="{
            aspectRatio: page.orientation === 'vertical' ? '9/16' : '16/9'
          }"
        >
          <div
            class="w-full h-full grid gap-0.5 p-0.5"
            :style="{
              gridTemplateColumns: `repeat(${page.columns}, 1fr)`,
              gridTemplateRows: `repeat(${page.rows}, 1fr)`,
              backgroundColor: page.background.type === 'color' ? page.background.color : '#1f2937'
            }"
          >
            <div
              v-for="cell in page.cells"
              :key="cell.id"
              class="rounded-sm"
              :class="cell.componentId ? 'bg-indigo-500/40' : 'bg-gray-800/50'"
              :style="{
                gridColumn: `${cell.column + 1} / span ${cell.columnSpan}`,
                gridRow: `${cell.row + 1} / span ${cell.rowSpan}`
              }"
            />
          </div>
        </div>

        <div class="flex items-center justify-between">
          <span class="text-xs text-gray-500">{{ new Date(page.updatedAt).toLocaleDateString() }}</span>
          <button
            class="opacity-0 group-hover:opacity-100 p-1.5 text-gray-400 hover:text-red-400 transition-all"
            @click.stop="deletePage(page.id)"
          >
            <Icon icon="solar:trash-bin-trash-bold" class="text-sm" />
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
