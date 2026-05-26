<script setup lang="ts">
import { Icon } from '@iconify/vue'
import PageHeader from '@/components/PageHeader.vue'
import { useDesignStore } from '@/stores/design'
import { useRouter } from 'vue-router'

const designStore = useDesignStore()
const router = useRouter()

function createNew() {
  const comp = designStore.createComponent()
  router.push(`/components/${comp.id}/designer`)
}

function editComponent(id: string) {
  router.push(`/components/${id}/designer`)
}

function deleteComponent(id: string) {
  designStore.deleteComponent(id)
}
</script>

<template>
  <div>
    <PageHeader title="组件" subtitle="设计移动端组件样式与逻辑" icon="solar:widget-2-bold">
      <template #actions>
        <button
          class="flex items-center gap-2 px-4 py-2 bg-indigo-600 hover:bg-indigo-500 rounded-lg text-sm transition-colors"
          @click="createNew"
        >
          <Icon icon="solar:add-circle-bold" class="text-base" />
          新建组件
        </button>
      </template>
    </PageHeader>

    <div v-if="designStore.components.length === 0" class="text-center py-20">
      <Icon icon="solar:widget-2-bold" class="text-6xl text-gray-700 mb-4 mx-auto block" />
      <h3 class="text-lg font-semibold text-gray-400 mb-2">暂无组件</h3>
      <p class="text-sm text-gray-500">创建一个组件，使用 Vue3 代码设计移动端样式和交互逻辑</p>
      <button class="mt-4 px-4 py-2 bg-indigo-600 hover:bg-indigo-500 rounded-lg text-sm transition-colors" @click="createNew">
        创建第一个组件
      </button>
    </div>

    <div v-else class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
      <div
        v-for="comp in designStore.components"
        :key="comp.id"
        class="bg-gray-900 border border-gray-800 rounded-xl p-5 hover:border-indigo-500/50 transition-all duration-200 cursor-pointer group"
        @click="editComponent(comp.id)"
      >
        <div class="flex items-start justify-between mb-3">
          <h3 class="font-semibold">{{ comp.name }}</h3>
          <span class="text-xs text-gray-500">{{ comp.assets.length }} 资源</span>
        </div>

        <p class="text-xs text-gray-400 mb-3">{{ comp.description || '暂无描述' }}</p>

        <!-- 代码预览 -->
        <div class="bg-gray-800/50 rounded-lg p-2 mb-3 max-h-20 overflow-hidden">
          <pre class="text-[10px] text-gray-500 font-mono whitespace-pre-wrap">{{ comp.templateCode.slice(0, 120) }}{{ comp.templateCode.length > 120 ? '...' : '' }}</pre>
        </div>

        <div class="flex items-center justify-between">
          <span class="text-xs text-gray-500">{{ new Date(comp.updatedAt).toLocaleDateString() }}</span>
          <button
            class="opacity-0 group-hover:opacity-100 p-1.5 text-gray-400 hover:text-red-400 transition-all"
            @click.stop="deleteComponent(comp.id)"
          >
            <Icon icon="solar:trash-bin-trash-bold" class="text-sm" />
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
