<script setup lang="ts">
import { Icon } from '@iconify/vue'
import PageHeader from '@/components/PageHeader.vue'
import ConfirmDialog from '@/components/ConfirmDialog.vue'
import { useSchemeStore } from '@/stores/schemes'
import { useNotificationStore } from '@/stores/notification'
import { useRouter } from 'vue-router'
import { ref } from 'vue'

const schemeStore = useSchemeStore()
const router = useRouter()
const notify = useNotificationStore()

const showCreateDialog = ref(false)
const newSchemeName = ref('')
const showDeleteConfirm = ref(false)
const deleteTargetId = ref<string | null>(null)
const deleteTargetName = ref('')

function createScheme() {
  showCreateDialog.value = true
  newSchemeName.value = ''
}

function confirmCreate() {
  const name = newSchemeName.value.trim() || '新方案'
  const scheme = {
    id: crypto.randomUUID().slice(0, 8),
    name,
    targetDeviceId: '',
    homePageId: '',
    layout: {
      type: 'grid' as const,
      columns: 3,
      rows: 4,
      pages: [],
    },
    plugins: [],
    version: 1,
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
  }
  schemeStore.addScheme(scheme)
  showCreateDialog.value = false
  router.push(`/schemes/${scheme.id}/editor`)
}

function editScheme(id: string) {
  router.push(`/schemes/${id}/editor`)
}

function askDeleteScheme(id: string) {
  const scheme = schemeStore.schemes.find(s => s.id === id)
  deleteTargetId.value = id
  deleteTargetName.value = scheme?.name ?? ''
  showDeleteConfirm.value = true
}

function confirmDeleteScheme() {
  if (deleteTargetId.value) {
    schemeStore.removeScheme(deleteTargetId.value)
    notify.success('方案已删除')
  }
  showDeleteConfirm.value = false
  deleteTargetId.value = null
}

function exportSchemes() {
  const blob = new Blob([JSON.stringify(schemeStore.schemes, null, 2)], { type: 'application/json' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = `onedesk-schemes-${Date.now()}.json`
  a.click()
  URL.revokeObjectURL(url)
  notify.success('方案已导出')
}

function importSchemes() {
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
          data.forEach((s: any) => schemeStore.addScheme(s))
          notify.success(`已导入 ${data.length} 个方案`)
        } else if (data.schemes) {
          data.schemes.forEach((s: any) => schemeStore.addScheme(s))
          notify.success(`已导入 ${data.schemes.length} 个方案`)
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
    <PageHeader title="方案管理" subtitle="设计移动端界面方案" icon="solar:layers-bold">
      <template #actions>
        <button class="btn-secondary" @click="importSchemes">
          <Icon icon="solar:import-bold" class="text-base" />
          导入
        </button>
        <button class="btn-secondary" @click="exportSchemes">
          <Icon icon="solar:export-bold" class="text-base" />
          导出
        </button>
        <button class="btn-primary" @click="createScheme">
          <Icon icon="solar:add-circle-bold" class="text-base" />
          新建方案
        </button>
      </template>
    </PageHeader>

    <div v-if="schemeStore.schemes.length === 0" class="text-center py-20">
      <Icon icon="solar:layers-bold" class="text-6xl mb-4 mx-auto block" style="color: var(--color-text-dim);" />
      <h3 class="text-lg font-semibold mb-2" style="color: var(--color-text-muted);">暂无方案</h3>
      <p class="text-sm" style="color: var(--color-text-dim);">创建一个方案，为移动端设计界面布局和交互逻辑</p>
      <button
        class="mt-4 px-4 py-2 rounded-lg text-sm text-white transition-colors"
        style="background-color: var(--color-primary);"
        @click="createScheme"
      >
        创建第一个方案
      </button>
    </div>

    <div v-else class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
      <div
        v-for="scheme in schemeStore.schemes"
        :key="scheme.id"
        class="rounded-xl p-5 cursor-pointer transition-all duration-200 border hover:border-blue-500/50"
        style="background-color: var(--color-bg-card); border-color: var(--color-border);"
        @click="editScheme(scheme.id)"
      >
        <div class="flex items-start justify-between mb-3">
          <h3 class="font-semibold">{{ scheme.name }}</h3>
          <span class="text-xs" style="color: var(--color-text-dim);">v{{ scheme.version }}</span>
        </div>

        <div class="text-xs space-y-1" style="color: var(--color-text-muted);">
          <p>布局：{{ scheme.layout.type }}（{{ scheme.layout.columns }}×{{ scheme.layout.rows }}）</p>
          <p>页面数：{{ scheme.layout.pages.length }}</p>
          <p>插件数：{{ scheme.plugins.length }}</p>
        </div>

        <div class="flex items-center justify-between mt-4 pt-3 text-xs" style="border-top: 1px solid var(--color-border-subtle); color: var(--color-text-dim);">
          <span>更新于 {{ new Date(scheme.updatedAt).toLocaleDateString() }}</span>
          <div class="flex items-center gap-2">
            <Icon icon="solar:pen-bold" class="text-sm" style="color: var(--color-primary);" />
            <button class="hover:text-red-400 transition-colors" @click.stop="askDeleteScheme(scheme.id)">
              <Icon icon="solar:trash-bin-trash-bold" class="text-sm" />
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- 创建方案对话框 -->
    <div v-if="showCreateDialog" class="dialog-overlay" @click.self="showCreateDialog = false">
      <div class="dialog-card space-y-4">
        <h3 class="text-lg font-bold">新建方案</h3>
        <div>
          <label class="text-sm" style="color: var(--color-text-muted);">方案名称</label>
          <input
            v-model="newSchemeName"
            class="w-full mt-1 px-3 py-2 rounded-lg text-sm focus:outline-none"
            style="background-color: var(--color-bg-surface); border: 1px solid var(--color-border); color: var(--color-text);"
            placeholder="输入方案名称"
            @keyup.enter="confirmCreate"
          />
        </div>
        <div class="flex gap-3 justify-end">
          <button class="btn-secondary" @click="showCreateDialog = false">取消</button>
          <button class="btn-primary" @click="confirmCreate">创建</button>
        </div>
      </div>
    </div>

    <!-- 删除确认 -->
    <ConfirmDialog
      v-if="showDeleteConfirm"
      title="删除方案"
      :message="`确定要删除方案「${deleteTargetName}」吗？此操作不可恢复。`"
      confirm-text="删除"
      :danger="true"
      @confirm="confirmDeleteScheme"
      @cancel="showDeleteConfirm = false"
    />
  </div>
</template>
