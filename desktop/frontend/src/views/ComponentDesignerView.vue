<script setup lang="ts">
import { Icon } from '@iconify/vue'
import { useDesignStore, type ComponentAsset } from '@/stores/design'
import { useRoute, useRouter } from 'vue-router'
import { computed, ref, watch } from 'vue'

const route = useRoute()
const router = useRouter()
const designStore = useDesignStore()

const compId = computed(() => route.params.id as string)
const comp = computed(() => designStore.components.find((c) => c.id === compId.value))

const compName = ref('')
const compDescription = ref('')
const templateCode = ref('')
const scriptCode = ref('')
const styleCode = ref('')
const activeTab = ref<'template' | 'script' | 'style'>('template')
const previewHtml = ref('')

watch(comp, (c) => {
  if (!c) return
  compName.value = c.name
  compDescription.value = c.description
  templateCode.value = c.templateCode
  scriptCode.value = c.scriptCode
  styleCode.value = c.styleCode
}, { immediate: true })

function save() {
  if (!comp.value) return
  designStore.updateComponent({
    ...comp.value,
    name: compName.value,
    description: compDescription.value,
    templateCode: templateCode.value,
    scriptCode: scriptCode.value,
    styleCode: styleCode.value,
  })
}

function addAsset() {
  if (!comp.value) return
  const asset: ComponentAsset = {
    id: crypto.randomUUID().slice(0, 8),
    name: '新资源',
    type: 'image',
    url: '',
    size: 0,
  }
  designStore.updateComponent({
    ...comp.value,
    assets: [...comp.value.assets, asset],
  })
}

function removeAsset(assetId: string) {
  if (!comp.value) return
  designStore.updateComponent({
    ...comp.value,
    assets: comp.value.assets.filter((a) => a.id !== assetId),
  })
}

function updateAsset(assetId: string, key: string, value: unknown) {
  if (!comp.value) return
  const assets = comp.value.assets.map((a) =>
    a.id === assetId ? { ...a, [key]: value } : a
  )
  designStore.updateComponent({ ...comp.value, assets })
}

/** 简单预览：将模板+样式组合为预览 HTML */
function generatePreview() {
  const html = `
    <div style="display:flex;align-items:center;justify-content:center;width:100%;height:100%;background:#111827;color:white;">
      <style>${styleCode.value}</style>
      <div class="comp">${templateCode.value.replace(/<[^>]+>/g, (m) => m).replace(/\{\{.*?\}\}/g, '预览')}</div>
    </div>
  `
  previewHtml.value = html
}

function goBack() {
  router.push('/components')
}
</script>

<template>
  <div v-if="comp" class="flex gap-6 h-[calc(100vh-120px)]">
    <!-- 左侧：属性 & 资源 -->
    <div class="w-72 shrink-0 overflow-y-auto space-y-4">
      <div class="flex items-center gap-2 mb-2">
        <button class="p-1.5 hover:bg-gray-800 rounded-lg transition-colors" @click="goBack">
          <Icon icon="solar:alt-arrow-left-bold" class="text-lg" />
        </button>
        <h2 class="text-lg font-bold truncate">{{ comp.name }}</h2>
      </div>

      <!-- 基本属性 -->
      <div class="bg-gray-900 border border-gray-800 rounded-xl p-4 space-y-3">
        <h3 class="text-sm font-semibold text-gray-300">基本属性</h3>
        <div>
          <label class="text-xs text-gray-500">组件名称</label>
          <input v-model="compName" @change="save" class="w-full mt-1 px-3 py-1.5 bg-gray-800 border border-gray-700 rounded-lg text-sm focus:outline-none focus:border-indigo-500" />
        </div>
        <div>
          <label class="text-xs text-gray-500">描述</label>
          <textarea v-model="compDescription" @change="save" rows="2" class="w-full mt-1 px-3 py-1.5 bg-gray-800 border border-gray-700 rounded-lg text-sm focus:outline-none focus:border-indigo-500 resize-none" />
        </div>
      </div>

      <!-- 静态资源 -->
      <div class="bg-gray-900 border border-gray-800 rounded-xl p-4 space-y-3">
        <div class="flex items-center justify-between">
          <h3 class="text-sm font-semibold text-gray-300">静态资源</h3>
          <button @click="addAsset" class="text-indigo-400 hover:text-indigo-300 text-xs">+ 添加</button>
        </div>
        <div v-if="comp.assets.length === 0" class="text-xs text-gray-600 text-center py-2">
          暂无资源
        </div>
        <div v-for="asset in comp.assets" :key="asset.id" class="bg-gray-800 rounded-lg p-2 space-y-1">
          <div class="flex items-center justify-between">
            <input :value="asset.name" @change="updateAsset(asset.id, 'name', ($event.target as HTMLInputElement).value)"
              class="flex-1 px-2 py-0.5 bg-gray-700 rounded text-xs focus:outline-none" />
            <button @click="removeAsset(asset.id)" class="ml-1 text-gray-500 hover:text-red-400">
              <Icon icon="solar:close-circle-bold" class="text-sm" />
            </button>
          </div>
          <div class="flex gap-1">
            <select :value="asset.type" @change="updateAsset(asset.id, 'type', ($event.target as HTMLSelectElement).value)"
              class="px-1 py-0.5 bg-gray-700 rounded text-xs">
              <option value="image">图片</option>
              <option value="video">视频</option>
            </select>
            <input :value="asset.url" @change="updateAsset(asset.id, 'url', ($event.target as HTMLInputElement).value)"
              placeholder="URL" class="flex-1 px-2 py-0.5 bg-gray-700 rounded text-xs focus:outline-none" />
          </div>
        </div>
      </div>
    </div>

    <!-- 中间：代码编辑器 -->
    <div class="flex-1 flex flex-col min-w-0">
      <!-- Tab 栏 -->
      <div class="flex items-center gap-1 mb-2 shrink-0">
        <button v-for="tab in (['template', 'script', 'style'] as const)" :key="tab"
          class="px-3 py-1.5 text-xs rounded-lg transition-colors"
          :class="activeTab === tab ? 'bg-indigo-600 text-white' : 'bg-gray-800 text-gray-400 hover:bg-gray-700'"
          @click="activeTab = tab"
        >
          {{ tab === 'template' ? '模板' : tab === 'script' ? '逻辑' : '样式' }}
        </button>
        <div class="flex-1" />
        <button @click="save" class="px-3 py-1.5 bg-indigo-600 hover:bg-indigo-500 rounded-lg text-xs transition-colors">
          保存
        </button>
        <button @click="generatePreview" class="px-3 py-1.5 bg-emerald-600 hover:bg-emerald-500 rounded-lg text-xs transition-colors">
          预览
        </button>
      </div>

      <!-- 代码编辑区 -->
      <div class="flex-1 min-h-0">
        <textarea
          v-model="templateCode"
          v-show="activeTab === 'template'"
          class="w-full h-full bg-gray-900 border border-gray-800 rounded-xl p-4 font-mono text-sm text-gray-300 resize-none focus:outline-none focus:border-indigo-500"
          spellcheck="false"
          placeholder="<div class='comp'>...</div>"
        />
        <textarea
          v-model="scriptCode"
          v-show="activeTab === 'script'"
          class="w-full h-full bg-gray-900 border border-gray-800 rounded-xl p-4 font-mono text-sm text-gray-300 resize-none focus:outline-none focus:border-indigo-500"
          spellcheck="false"
          placeholder="export default { ... }"
        />
        <textarea
          v-model="styleCode"
          v-show="activeTab === 'style'"
          class="w-full h-full bg-gray-900 border border-gray-800 rounded-xl p-4 font-mono text-sm text-gray-300 resize-none focus:outline-none focus:border-indigo-500"
          spellcheck="false"
          placeholder=".comp { ... }"
        />
      </div>
    </div>

    <!-- 右侧：实时预览 -->
    <div class="w-80 shrink-0">
      <div class="bg-gray-900 border border-gray-800 rounded-xl overflow-hidden">
        <div class="flex items-center justify-between px-3 py-2 border-b border-gray-800">
          <h3 class="text-xs font-semibold text-gray-400">预览</h3>
          <button @click="generatePreview" class="text-xs text-indigo-400 hover:text-indigo-300">
            <Icon icon="solar:refresh-bold" class="text-sm" />
          </button>
        </div>
        <div class="aspect-[9/16] bg-gray-950 flex items-center justify-center">
          <div v-if="previewHtml" class="w-full h-full" v-html="previewHtml" />
          <div v-else class="text-center text-gray-600">
            <Icon icon="solar:eye-bold" class="text-3xl mb-2 mx-auto block" />
            <p class="text-xs">点击"预览"按钮查看效果</p>
          </div>
        </div>
      </div>
    </div>
  </div>

  <div v-else class="text-center py-20 text-gray-500">
    <p>组件未找到</p>
    <button class="mt-4 px-4 py-2 bg-gray-700 hover:bg-gray-600 rounded-lg text-sm" @click="goBack">返回</button>
  </div>
</template>
