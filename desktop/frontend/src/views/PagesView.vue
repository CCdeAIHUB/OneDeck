<script setup lang="ts">
import { Icon } from '@iconify/vue'
import PageHeader from '@/components/PageHeader.vue'
import ConfirmDialog from '@/components/ConfirmDialog.vue'
import { useDesignStore } from '@/stores/design'
import { useNotificationStore } from '@/stores/notification'
import { useRouter } from 'vue-router'
import { ref } from 'vue'

const designStore = useDesignStore()
const router = useRouter()
const notify = useNotificationStore()

const showDeleteConfirm = ref(false)
const deleteTargetId = ref<string | null>(null)
const deleteTargetName = ref('')

function createNew() {
  const page = designStore.createPage()
  router.push(`/pages/${page.id}/designer`)
}

function editPage(id: string) {
  router.push(`/pages/${id}/designer`)
}

function askDeletePage(id: string) {
  const page = designStore.pages.find(p => p.id === id)
  deleteTargetId.value = id
  deleteTargetName.value = page?.name ?? ''
  showDeleteConfirm.value = true
}

function confirmDeletePage() {
  if (deleteTargetId.value) {
    designStore.deletePage(deleteTargetId.value)
    notify.success('页面已删除')
  }
  showDeleteConfirm.value = false
  deleteTargetId.value = null
}

function exportPage(page: any) {
  const blob = new Blob([JSON.stringify(page, null, 2)], { type: 'application/json' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = `page-${page.name}-${Date.now()}.json`
  a.click()
  URL.revokeObjectURL(url)
  notify.success('页面已导出')
}

function exportAllPages() {
  const blob = new Blob([JSON.stringify(designStore.pages, null, 2)], { type: 'application/json' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = `onedesk-pages-${Date.now()}.json`
  a.click()
  URL.revokeObjectURL(url)
  notify.success('页面已导出')
}

function importPages() {
  const input = document.createElement('input')
  input.type = 'file'
  input.accept = '.json'
  input.onchange = (e) => {
    const file = (e.target as HTMLInputElement).files?.[0]
    if (!file) return
    const reader = new FileReader()
    reader.onload = (ev) => {
      try {
        const data = JSON.parse(ev.target?.result as string)
        if (Array.isArray(data)) {
          data.forEach((p: any) => designStore.pages.push(p))
          notify.success(`已导入 ${data.length} 个页面`)
        } else if (data.pages) {
          data.pages.forEach((p: any) => designStore.pages.push(p))
          notify.success(`已导入 ${data.pages.length} 个页面`)
        }
      } catch {
        notify.error('导入失败：文件格式无效')
      }
    }
    reader.readAsText(file)
  }
  input.click()
}
</script>

<template>
  <div>
    <PageHeader title="页面" subtitle="设计移动端页面布局" icon="solar:clipboard-list-bold">
      <template #actions>
        <button class="btn-secondary" @click="importPages">
          <Icon icon="solar:import-bold" class="text-base" />
          导入
        </button>
        <button class="btn-secondary" @click="exportAllPages">
          <Icon icon="solar:export-bold" class="text-base" />
          导出
        </button>
        <button class="btn-primary" @click="createNew">
          <Icon icon="solar:add-circle-bold" class="text-base" />
          新建页面
        </button>
      </template>
    </PageHeader>

    <div v-if="designStore.pages.length === 0" class="text-center py-20">
      <Icon icon="solar:clipboard-list-bold" class="text-6xl mb-4 mx-auto block" style="color: var(--color-text-dim);" />
      <h3 class="text-lg font-semibold mb-2" style="color: var(--color-text-muted);">暂无页面</h3>
      <p class="text-sm" style="color: var(--color-text-dim);">创建一个页面，为移动端设计界面布局</p>
      <button
        class="mt-4 px-4 py-2 rounded-lg text-sm text-white transition-colors"
        style="background-color: var(--color-primary);"
        @click="createNew"
      >
        创建第一个页面
      </button>
    </div>

    <div v-else class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 items-start">
      <div
        v-for="page in designStore.pages"
        :key="page.id"
        class="rounded-xl p-5 hover:border-blue-500/50 transition-all duration-200 cursor-pointer group border"
        style="background-color: var(--color-bg-card); border-color: var(--color-border);"
        @click="editPage(page.id)"
      >
        <div class="flex items-start justify-between mb-3">
          <h3 class="font-semibold">{{ page.name }}</h3>
          <span class="text-xs px-1.5 py-0.5 rounded" :style="page.orientation === 'vertical' ? 'background-color: rgba(59,130,246,0.15); color: var(--color-primary);' : 'background-color: rgba(245,158,11,0.15); color: #f59e0b;'">{{ page.orientation === 'vertical' ? '竖屏' : '横屏' }}</span>
        </div>

        <div class="text-xs space-y-1 mb-4" style="color: var(--color-text-muted);">
          <p>格子：{{ page.rows }} × {{ page.columns }}</p>
          <p>组件数：{{ page.cells.filter(c => c.componentId).length }} / {{ page.cells.length }}</p>
          <p v-if="page.background.type === 'color'">背景：{{ page.background.color }}</p>
          <p v-else-if="page.background.type === 'image'">背景：图片</p>
          <p v-else>背景：视频</p>
        </div>

        <!-- 格子预览 - 竖屏高窄，横屏矮宽 -->
        <div
          class="rounded-lg overflow-hidden mb-3"
          :style="{
            aspectRatio: page.orientation === 'vertical' ? '9/16' : '16/9',
            border: '1px solid var(--color-border-subtle)',
          }"
        >
          <div
            class="w-full h-full grid gap-0.5 p-0.5"
            :style="{
              gridTemplateColumns: `repeat(${page.columns}, 1fr)`,
              gridTemplateRows: `repeat(${page.rows}, 1fr)`,
              backgroundColor: page.background.type === 'color' ? page.background.color : 'var(--color-bg-surface)',
              placeContent: 'center',
            }"
          >
            <div
              v-for="cell in page.cells"
              :key="cell.id"
              class="rounded-sm"
              :style="{ backgroundColor: cell.componentId ? 'rgba(59,130,246,0.3)' : 'var(--color-bg-surface)', gridColumn: `${cell.column + 1} / span ${cell.columnSpan}`, gridRow: `${cell.row + 1} / span ${cell.rowSpan}`, aspectRatio: '1' }"
            />
          </div>
        </div>

        <div class="flex items-center justify-between">
          <span class="text-xs" style="color: var(--color-text-dim);">{{ new Date(page.updatedAt).toLocaleDateString() }}</span>
          <div class="flex items-center gap-1">
            <button
              class="opacity-0 group-hover:opacity-100 p-1.5 transition-all"
              style="color: var(--color-text-muted);"
              @click.stop="exportPage(page)"
              title="导出"
            >
              <Icon icon="solar:download-bold" class="text-sm" />
            </button>
            <button
              class="opacity-0 group-hover:opacity-100 p-1.5 transition-all hover:text-red-400"
              style="color: var(--color-text-muted);"
              @click.stop="askDeletePage(page.id)"
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
      title="删除页面"
      :message="`确定要删除页面「${deleteTargetName}」吗？此操作不可恢复。`"
      confirm-text="删除"
      :danger="true"
      @confirm="confirmDeletePage"
      @cancel="showDeleteConfirm = false"
    />
  </div>
</template>
