<script setup lang="ts">
import { Icon } from '@iconify/vue'
import PageHeader from '@/components/PageHeader.vue'
import { usePluginStore } from '@/stores/plugins'
import { useRouter } from 'vue-router'
import { ref } from 'vue'

const pluginStore = usePluginStore()
const router = useRouter()

const showCreateDialog = ref(false)
const newPluginName = ref('')

function createPlugin() {
  showCreateDialog.value = true
  newPluginName.value = ''
}

function confirmCreate() {
  const name = newPluginName.value.trim() || '新插件'
  const plugin = {
    id: crypto.randomUUID().slice(0, 8),
    name,
    version: '1.0.0',
    description: '',
    author: '',
    icon: 'solar:code-square-bold',
    isEnabled: true,
    installedAt: new Date().toISOString(),
  }
  pluginStore.addPlugin(plugin)
  showCreateDialog.value = false
  router.push(`/plugins/${plugin.id}/editor`)
}

function editPlugin(id: string) {
  router.push(`/plugins/${id}/editor`)
}

function togglePlugin(pluginId: string) {
  pluginStore.togglePlugin(pluginId)
}

function deletePlugin(id: string) {
  if (confirm('确定删除该插件？')) {
    pluginStore.removePlugin(id)
  }
}

function exportPlugin(plugin: any) {
  const blob = new Blob([JSON.stringify(plugin, null, 2)], { type: 'application/json' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = `plugin-${plugin.name}-${Date.now()}.json`
  a.click()
  URL.revokeObjectURL(url)
}
</script>

<template>
  <div>
    <PageHeader title="插件" subtitle="开发和编辑插件代码" icon="solar:code-square-bold">
      <template #actions>
        <button
          class="flex items-center gap-2 px-4 py-2 rounded-lg text-sm text-white transition-colors"
          style="background-color: var(--color-primary);"
          @click="createPlugin"
        >
          <Icon icon="solar:add-circle-bold" class="text-base" />
          新建插件
        </button>
      </template>
    </PageHeader>

    <div v-if="pluginStore.plugins.length === 0" class="text-center py-20">
      <Icon icon="solar:code-square-bold" class="text-6xl mb-4 mx-auto block" style="color: var(--color-text-dim);" />
      <h3 class="text-lg font-semibold mb-2" style="color: var(--color-text-muted);">暂无插件</h3>
      <p class="text-sm" style="color: var(--color-text-dim);">创建一个插件来扩展 OneDeck 的功能</p>
      <button
        class="mt-4 px-4 py-2 rounded-lg text-sm text-white transition-colors"
        style="background-color: var(--color-primary);"
        @click="createPlugin"
      >
        创建第一个插件
      </button>
    </div>

    <div v-else class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
      <div
        v-for="plugin in pluginStore.plugins"
        :key="plugin.id"
        class="rounded-xl p-5 hover:border-blue-500/50 transition-all duration-200 cursor-pointer group border"
        style="background-color: var(--color-bg-card); border-color: var(--color-border);"
        @click="editPlugin(plugin.id)"
      >
        <div class="flex items-start justify-between mb-3">
          <div class="flex items-center gap-3">
            <div class="w-10 h-10 rounded-lg flex items-center justify-center" style="background-color: rgba(59,130,246,0.15);">
              <Icon :icon="plugin.icon || 'solar:code-square-bold'" class="text-xl" style="color: var(--color-primary);" />
            </div>
            <div>
              <h3 class="font-semibold">{{ plugin.name }}</h3>
              <p class="text-xs" style="color: var(--color-text-dim);">v{{ plugin.version }}</p>
            </div>
          </div>
          <!-- 启用开关 -->
          <button
            class="relative w-9 h-5 rounded-full shrink-0 transition-colors duration-200"
            :style="plugin.isEnabled ? 'background-color: var(--color-primary);' : 'background-color: var(--color-bg-hover);'"
            @click.stop="togglePlugin(plugin.id)"
          >
            <span class="absolute top-0.5 left-0.5 w-4 h-4 bg-white rounded-full transition-transform duration-200"
              :class="plugin.isEnabled ? 'translate-x-4' : ''" />
          </button>
        </div>

        <p class="text-xs mb-3" style="color: var(--color-text-muted);">{{ plugin.description || '暂无描述' }}</p>

        <div class="flex items-center justify-between">
          <span class="text-xs" style="color: var(--color-text-dim);">{{ plugin.author || '未知作者' }}</span>
          <div class="flex items-center gap-1">
            <button class="opacity-0 group-hover:opacity-100 p-1.5 transition-all" style="color: var(--color-text-muted);" @click.stop="exportPlugin(plugin)" title="导出">
              <Icon icon="solar:download-bold" class="text-sm" />
            </button>
            <button class="opacity-0 group-hover:opacity-100 p-1.5 transition-all hover:text-red-400" style="color: var(--color-text-muted);" @click.stop="deletePlugin(plugin.id)">
              <Icon icon="solar:trash-bin-trash-bold" class="text-sm" />
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- 创建插件对话框 -->
    <div v-if="showCreateDialog" class="fixed inset-0 bg-black/50 flex items-center justify-center z-50" @click.self="showCreateDialog = false">
      <div class="rounded-xl p-6 w-96 space-y-4" style="background-color: var(--color-bg-card);">
        <h3 class="text-lg font-bold">新建插件</h3>
        <div>
          <label class="text-sm" style="color: var(--color-text-muted);">插件名称</label>
          <input v-model="newPluginName" class="w-full mt-1 px-3 py-2 rounded-lg text-sm focus:outline-none" style="background-color: var(--color-bg-surface); border: 1px solid var(--color-border); color: var(--color-text);" placeholder="输入插件名称" @keyup.enter="confirmCreate" />
        </div>
        <div class="flex gap-3 justify-end">
          <button class="px-4 py-2 rounded-lg text-sm transition-colors" style="background-color: var(--color-bg-surface); color: var(--color-text-muted);" @click="showCreateDialog = false">取消</button>
          <button class="px-4 py-2 rounded-lg text-sm text-white transition-colors" style="background-color: var(--color-primary);" @click="confirmCreate">创建</button>
        </div>
      </div>
    </div>
  </div>
</template>
