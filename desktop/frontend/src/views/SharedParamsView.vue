<script setup lang="ts">
import { Icon } from '@iconify/vue'
import PageHeader from '@/components/PageHeader.vue'
import ConfirmDialog from '@/components/ConfirmDialog.vue'
import { useSharedParamsStore } from '@/stores/sharedParams'
import { useNotificationStore } from '@/stores/notification'
import { ref, computed } from 'vue'

const sharedParams = useSharedParamsStore()
const notify = useNotificationStore()

const newKey = ref('')
const newValue = ref('')
const newValueType = ref<'string' | 'number' | 'boolean' | 'object'>('string')
const editingKey = ref<string | null>(null)
const editValue = ref('')
const showAddDialog = ref(false)
// 删除确认
const showDeleteConfirm = ref(false)
const deleteTargetKey = ref('')
// 清空确认
const showClearConfirm = ref(false)

const paramEntries = computed(() => {
  return Object.entries(sharedParams.params).map(([key, value]) => ({
    key,
    value,
    type: typeof value,
  }))
})

function addParam() {
  if (!newKey.value.trim()) return
  let parsedValue: unknown = newValue.value
  switch (newValueType.value) {
    case 'number':
      parsedValue = Number(newValue.value)
      break
    case 'boolean':
      parsedValue = newValue.value === 'true'
      break
    case 'object':
      try { parsedValue = JSON.parse(newValue.value) } catch { parsedValue = newValue.value }
      break
  }
  sharedParams.set(newKey.value.trim(), parsedValue)
  newKey.value = ''
  newValue.value = ''
  showAddDialog.value = false
}

function startEdit(key: string) {
  editingKey.value = key
  editValue.value = JSON.stringify(sharedParams.get(key), null, 2)
}

function saveEdit() {
  if (!editingKey.value) return
  try {
    const parsed = JSON.parse(editValue.value)
    sharedParams.set(editingKey.value, parsed)
  } catch {
    sharedParams.set(editingKey.value, editValue.value)
  }
  editingKey.value = null
  editValue.value = ''
}

function cancelEdit() {
  editingKey.value = null
  editValue.value = ''
}

function deleteParam(key: string) {
  deleteTargetKey.value = key
  showDeleteConfirm.value = true
}

function confirmDeleteParam() {
  sharedParams.remove(deleteTargetKey.value)
  showDeleteConfirm.value = false
  notify.success('参数已删除')
}

function exportParams() {
  const json = sharedParams.exportJson()
  const blob = new Blob([json], { type: 'application/json' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = `onedesk-params-${Date.now()}.json`
  a.click()
  URL.revokeObjectURL(url)
}

function importParams() {
  const input = document.createElement('input')
  input.type = 'file'
  input.accept = '.json'
  input.onchange = (e) => {
    const file = (e.target as HTMLInputElement).files?.[0]
    if (!file) return
    const reader = new FileReader()
    reader.onload = (ev) => {
      try {
        sharedParams.importJson(ev.target?.result as string, true)
        notify.success('参数已导入')
      } catch {
        notify.error('导入失败：无效的 JSON 格式')
      }
    }
    reader.readAsText(file)
  }
  input.click()
}

function clearAll() {
  showClearConfirm.value = true
}

function confirmClearAll() {
  sharedParams.clear()
  showClearConfirm.value = false
  notify.success('参数已清空')
}

function formatValue(value: unknown): string {
  if (typeof value === 'object') return JSON.stringify(value)
  return String(value)
}
</script>

<template>
  <div>
    <PageHeader title="公共参数库" subtitle="全局共享数据存储，任何组件和插件均可读写" icon="solar:database-bold">
      <template #actions>
        <button
          class="flex items-center gap-2 px-3 py-2 rounded-lg text-sm transition-colors"
          style="background-color: var(--color-bg-surface); color: var(--color-text-muted); border: 1px solid var(--color-border);"
          @click="exportParams"
        >
          <Icon icon="solar:download-bold" class="text-base" />
          导出
        </button>
        <button
          class="flex items-center gap-2 px-3 py-2 rounded-lg text-sm transition-colors"
          style="background-color: var(--color-bg-surface); color: var(--color-text-muted); border: 1px solid var(--color-border);"
          @click="importParams"
        >
          <Icon icon="solar:upload-bold" class="text-base" />
          导入
        </button>
        <button
          class="flex items-center gap-2 px-4 py-2 rounded-lg text-sm text-white transition-colors"
          style="background-color: var(--color-primary);"
          @click="showAddDialog = true"
        >
          <Icon icon="solar:add-circle-bold" class="text-base" />
          新增参数
        </button>
      </template>
    </PageHeader>

    <!-- 安全提示 -->
    <div class="mb-4 px-4 py-3 rounded-lg flex items-center gap-2 text-xs" style="background-color: #f59e0b20; color: #f59e0b; border: 1px solid #f59e0b40;">
      <Icon icon="solar:danger-triangle-bold" class="text-base" />
      <span>公共参数库对所有组件和插件开放读写权限，请勿存储敏感数据</span>
    </div>

    <!-- 参数列表 -->
    <div v-if="paramEntries.length === 0" class="text-center py-20">
      <Icon icon="solar:database-bold" class="text-6xl mb-4 mx-auto block" style="color: var(--color-text-dim);" />
      <h3 class="text-lg font-semibold mb-2" style="color: var(--color-text-muted);">暂无参数</h3>
      <p class="text-sm" style="color: var(--color-text-dim);">创建参数以在组件和插件间共享数据</p>
    </div>

    <div v-else class="space-y-2">
      <div
        v-for="entry in paramEntries"
        :key="entry.key"
        class="rounded-lg p-4 border transition-colors"
        style="background-color: var(--color-bg-card); border-color: var(--color-border);"
      >
        <div v-if="editingKey === entry.key" class="space-y-2">
          <div class="flex items-center gap-2">
            <span class="text-sm font-mono font-semibold" style="color: var(--color-primary);">{{ entry.key }}</span>
            <span class="text-xs px-1.5 py-0.5 rounded" style="background-color: var(--color-bg-surface); color: var(--color-text-dim);">{{ entry.type }}</span>
          </div>
          <textarea
            v-model="editValue"
            rows="4"
            class="w-full px-3 py-2 rounded-lg text-sm font-mono focus:outline-none resize-none"
            style="background-color: var(--color-bg-surface); border: 1px solid var(--color-border); color: var(--color-text);"
          />
          <div class="flex gap-2">
            <button @click="saveEdit" class="px-3 py-1 rounded text-xs text-white" style="background-color: var(--color-primary);">保存</button>
            <button @click="cancelEdit" class="px-3 py-1 rounded text-xs" style="background-color: var(--color-bg-surface); color: var(--color-text-muted);">取消</button>
          </div>
        </div>
        <div v-else class="flex items-start justify-between gap-4">
          <div class="flex-1 min-w-0">
            <div class="flex items-center gap-2 mb-1">
              <span class="text-sm font-mono font-semibold" style="color: var(--color-primary);">{{ entry.key }}</span>
              <span class="text-xs px-1.5 py-0.5 rounded" style="background-color: var(--color-bg-surface); color: var(--color-text-dim);">{{ entry.type }}</span>
            </div>
            <p class="text-xs font-mono truncate" style="color: var(--color-text-muted);" :title="formatValue(entry.value)">
              {{ formatValue(entry.value) }}
            </p>
          </div>
          <div class="flex items-center gap-1 shrink-0">
            <button @click="startEdit(entry.key)" class="p-1.5 rounded transition-colors" style="color: var(--color-text-dim);" title="编辑">
              <Icon icon="solar:pen-bold" class="text-sm" />
            </button>
            <button @click="deleteParam(entry.key)" class="p-1.5 rounded transition-colors hover:text-red-400" style="color: var(--color-text-dim);" title="删除">
              <Icon icon="solar:trash-bin-trash-bold" class="text-sm" />
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- 清空按钮 -->
    <div v-if="paramEntries.length > 0" class="mt-6 flex justify-end">
      <button
        class="px-4 py-2 rounded-lg text-sm transition-colors hover:text-red-400"
        style="color: var(--color-text-dim);"
        @click="clearAll"
      >
        <Icon icon="solar:trash-bin-trash-bold" class="inline mr-1" />
        清空所有参数
      </button>
    </div>

    <!-- 新增参数对话框 -->
    <div v-if="showAddDialog" class="dialog-overlay" @click.self="showAddDialog = false">
      <div class="dialog-card space-y-4">
        <h3 class="text-lg font-bold">新增参数</h3>
        <div>
          <label class="text-sm" style="color: var(--color-text-muted);">键名</label>
          <input
            v-model="newKey"
            class="w-full mt-1 px-3 py-2 rounded-lg text-sm font-mono focus:outline-none"
            style="background-color: var(--color-bg-surface); border: 1px solid var(--color-border); color: var(--color-text);"
            placeholder="param_name"
          />
        </div>
        <div>
          <label class="text-sm" style="color: var(--color-text-muted);">值类型</label>
          <div class="flex items-center gap-2 mt-1">
            <button
              v-for="t in (['string', 'number', 'boolean', 'object'] as const)"
              :key="t"
              class="px-3 py-1 text-xs rounded-lg transition-colors"
              :style="newValueType === t ? 'background-color: var(--color-primary); color: white;' : 'background-color: var(--color-bg-surface); color: var(--color-text-muted);'"
              @click="newValueType = t"
            >{{ t }}</button>
          </div>
        </div>
        <div>
          <label class="text-sm" style="color: var(--color-text-muted);">值</label>
          <!-- number: 数字输入 -->
          <input
            v-if="newValueType === 'number'"
            v-model.number="newValue"
            type="number"
            step="any"
            class="w-full mt-1 px-3 py-2 rounded-lg text-sm font-mono focus:outline-none"
            style="background-color: var(--color-bg-surface); border: 1px solid var(--color-border); color: var(--color-text);"
            placeholder="0"
          />
          <!-- boolean: 选择框 -->
          <select
            v-else-if="newValueType === 'boolean'"
            v-model="newValue"
            class="w-full mt-1 px-3 py-2 rounded-lg text-sm focus:outline-none"
            style="background-color: var(--color-bg-surface); border: 1px solid var(--color-border); color: var(--color-text);"
          >
            <option value="true">true</option>
            <option value="false">false</option>
          </select>
          <!-- object: textarea -->
          <textarea
            v-else-if="newValueType === 'object'"
            v-model="newValue"
            rows="3"
            class="w-full mt-1 px-3 py-2 rounded-lg text-sm font-mono focus:outline-none resize-none"
            style="background-color: var(--color-bg-surface); border: 1px solid var(--color-border); color: var(--color-text);"
            placeholder='{ "key": "value" }'
          />
          <!-- string: 文本输入 -->
          <input
            v-else
            v-model="newValue"
            class="w-full mt-1 px-3 py-2 rounded-lg text-sm focus:outline-none"
            style="background-color: var(--color-bg-surface); border: 1px solid var(--color-border); color: var(--color-text);"
            placeholder="输入文本值..."
          />
        </div>
        <div class="flex gap-3 justify-end">
          <button
            class="px-4 py-2 rounded-lg text-sm transition-colors"
            style="background-color: var(--color-bg-surface); color: var(--color-text-muted);"
            @click="showAddDialog = false"
          >
            取消
          </button>
          <button
            class="px-4 py-2 rounded-lg text-sm text-white transition-colors"
            style="background-color: var(--color-primary);"
            @click="addParam"
          >
            添加
          </button>
        </div>
      </div>
    </div>

    <!-- 删除确认 -->
    <ConfirmDialog
      v-if="showDeleteConfirm"
      title="删除参数"
      :message="`确定要删除参数「${deleteTargetKey}」吗？`"
      confirm-text="删除"
      :danger="true"
      @confirm="confirmDeleteParam"
      @cancel="showDeleteConfirm = false"
    />

    <!-- 清空确认 -->
    <ConfirmDialog
      v-if="showClearConfirm"
      title="清空所有参数"
      message="确定要清空所有参数吗？此操作不可恢复。"
      confirm-text="清空"
      :danger="true"
      @confirm="confirmClearAll"
      @cancel="showClearConfirm = false"
    />
  </div>
</template>
