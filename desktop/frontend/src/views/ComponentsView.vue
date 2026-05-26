<script setup lang="ts">
import { Icon } from '@iconify/vue'
import PageHeader from '@/components/PageHeader.vue'
import ConfirmDialog from '@/components/ConfirmDialog.vue'
import { useDesignStore } from '@/stores/design'
import { useRouter } from 'vue-router'
import { ref } from 'vue'

const designStore = useDesignStore()
const router = useRouter()

const showDeleteConfirm = ref(false)
const deleteTargetId = ref<string | null>(null)
const deleteTargetName = ref('')

function createNew() {
  const comp = designStore.createComponent()
  router.push(`/components/${comp.id}/designer`)
}

function editComponent(id: string) {
  router.push(`/components/${id}/designer`)
}

function askDeleteComponent(id: string) {
  const comp = designStore.components.find(c => c.id === id)
  deleteTargetId.value = id
  deleteTargetName.value = comp?.name ?? ''
  showDeleteConfirm.value = true
}

function confirmDeleteComponent() {
  if (deleteTargetId.value) {
    designStore.deleteComponent(deleteTargetId.value)
  }
  showDeleteConfirm.value = false
  deleteTargetId.value = null
}

function exportComponent(comp: any) {
  const blob = new Blob([JSON.stringify(comp, null, 2)], { type: 'application/json' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = `component-${comp.name}-${Date.now()}.json`
  a.click()
  URL.revokeObjectURL(url)
}
</script>

<template>
  <div>
    <PageHeader title="组件" subtitle="设计移动端组件样式与逻辑" icon="solar:widget-2-bold">
      <template #actions>
        <button class="btn-primary" @click="createNew">
          <Icon icon="solar:add-circle-bold" class="text-base" />
          新建组件
        </button>
      </template>
    </PageHeader>

    <div v-if="designStore.components.length === 0" class="text-center py-20">
      <Icon icon="solar:widget-2-bold" class="text-6xl mb-4 mx-auto block" style="color: var(--color-text-dim);" />
      <h3 class="text-lg font-semibold mb-2" style="color: var(--color-text-muted);">暂无组件</h3>
      <p class="text-sm" style="color: var(--color-text-dim);">创建一个组件，使用 Vue3 代码设计移动端样式和交互逻辑</p>
      <button
        class="mt-4 px-4 py-2 rounded-lg text-sm text-white transition-colors"
        style="background-color: var(--color-primary);"
        @click="createNew"
      >
        创建第一个组件
      </button>
    </div>

    <div v-else class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
      <div
        v-for="comp in designStore.components"
        :key="comp.id"
        class="rounded-xl p-5 hover:border-blue-500/50 transition-all duration-200 cursor-pointer group border"
        style="background-color: var(--color-bg-card); border-color: var(--color-border);"
        @click="editComponent(comp.id)"
      >
        <div class="flex items-start justify-between mb-3">
          <h3 class="font-semibold">{{ comp.name }}</h3>
          <span class="text-xs" style="color: var(--color-text-dim);">{{ comp.assets.length }} 资源</span>
        </div>

        <p class="text-xs mb-3" style="color: var(--color-text-muted);">{{ comp.description || '暂无描述' }}</p>

        <!-- 预览封面图 -->
        <div v-if="comp.previewImage" class="rounded-lg overflow-hidden mb-3" style="aspect-ratio: 9/16; max-height: 120px;">
          <img :src="comp.previewImage" class="w-full h-full object-cover" />
        </div>
        <!-- 无封面时显示代码预览 -->
        <div v-else class="rounded-lg p-2 mb-3 max-h-20 overflow-hidden" style="background-color: var(--color-bg-surface);">
          <pre class="text-[10px] font-mono whitespace-pre-wrap" style="color: var(--color-text-dim);">{{ comp.templateCode.slice(0, 120) }}{{ comp.templateCode.length > 120 ? '...' : '' }}</pre>
        </div>

        <div class="flex items-center justify-between">
          <span class="text-xs" style="color: var(--color-text-dim);">{{ new Date(comp.updatedAt).toLocaleDateString() }}</span>
          <div class="flex items-center gap-1">
            <button
              class="opacity-0 group-hover:opacity-100 p-1.5 transition-all"
              style="color: var(--color-text-muted);"
              @click.stop="exportComponent(comp)"
              title="导出"
            >
              <Icon icon="solar:download-bold" class="text-sm" />
            </button>
            <button
              class="opacity-0 group-hover:opacity-100 p-1.5 transition-all hover:text-red-400"
              style="color: var(--color-text-muted);"
              @click.stop="askDeleteComponent(comp.id)"
            >
              <Icon icon="solar:trash-bin-trash-bold" class="text-sm" />
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- 删除确认 -->
    <ConfirmDialog
      v-if="showDeleteConfirm"
      title="删除组件"
      :message="`确定要删除组件「${deleteTargetName}」吗？此操作不可恢复。`"
      confirm-text="删除"
      :danger="true"
      @confirm="confirmDeleteComponent"
      @cancel="showDeleteConfirm = false"
    />
  </div>
</template>
