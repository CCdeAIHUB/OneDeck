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
const scriptSyntax = ref<'options' | 'setup'>('setup')

// 数据引入
interface DataBinding {
  id: string
  name: string
  source: 'pc' | 'mobile'
  api: string
  interval: number
}
const dataBindings = ref<DataBinding[]>([])

// JSAPI 数据源列表
const jsApiSources = [
  { value: 'device.battery', label: '电池信息' },
  { value: 'device.network', label: '网络状态' },
  { value: 'device.screen', label: '屏幕信息' },
  { value: 'device.storage', label: '存储信息' },
  { value: 'device.memory', label: '内存信息' },
  { value: 'device.model', label: '设备型号' },
  { value: 'location.current', label: '当前位置' },
  { value: 'clipboard.read', label: '剪贴板内容' },
  { value: 'network.status', label: '网络连接' },
  { value: 'storage.get', label: '存储数据' },
]

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

/** 选择本地文件 */
function selectLocalFile(assetId: string) {
  const input = document.createElement('input')
  input.type = 'file'
  input.accept = 'image/*,video/*'
  input.onchange = (e) => {
    const file = (e.target as HTMLInputElement).files?.[0]
    if (!file) return
    const reader = new FileReader()
    reader.onload = (ev) => {
      const dataUrl = ev.target?.result as string
      updateAsset(assetId, 'url', dataUrl)
      updateAsset(assetId, 'name', file.name)
      updateAsset(assetId, 'size', file.size)
    }
    reader.readAsDataURL(file)
  }
  input.click()
}

// 数据绑定
function addDataBinding() {
  dataBindings.value.push({
    id: crypto.randomUUID().slice(0, 8),
    name: 'newData',
    source: 'mobile',
    api: 'device.battery',
    interval: 5000,
  })
}

function removeDataBinding(id: string) {
  dataBindings.value = dataBindings.value.filter(d => d.id !== id)
}

/** 生成数据引入代码 */
function generateDataImportCode(): string {
  if (dataBindings.value.length === 0) return ''
  const lines = dataBindings.value.map(d => {
    const varName = d.name.replace(/[^a-zA-Z0-9_]/g, '_')
    const refName = `${varName}Ref`
    if (scriptSyntax.value === 'setup') {
      return `// ${d.source === 'pc' ? 'PC端' : '移动端'} ${d.api} (每${d.interval}ms刷新)
const ${refName} = ref(null)
async function fetch${varName}() {
  try {
    ${refName}.value = await context.callApi('${d.api}')
  } catch (e) { console.error('获取${d.name}失败:', e) }
}
const ${varName}Timer = setInterval(fetch${varName}, ${d.interval})
fetch${varName}()
onUnmounted(() => clearInterval(${varName}Timer))`
    } else {
      return `// ${d.source === 'pc' ? 'PC端' : '移动端'} ${d.api}`
    }
  })
  return lines.join('\n\n')
}

/** 简单预览：将模板+样式组合为预览 HTML */
function generatePreview() {
  const html = `
    <div style="display:flex;align-items:center;justify-content:center;width:100%;height:100%;background:#111827;color:white;">
      <style>${styleCode.value}</style>
      <div class="comp">${templateCode.value.replace(/\{\{.*?\}\}/g, '预览')}</div>
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
    <!-- 左侧：属性 & 资源 & 数据绑定 -->
    <div class="w-72 shrink-0 overflow-y-auto space-y-4">
      <div class="flex items-center gap-2 mb-2">
        <button class="p-1.5 rounded-lg transition-colors" style="color: var(--color-text-muted);" @click="goBack">
          <Icon icon="solar:alt-arrow-left-bold" class="text-lg" />
        </button>
        <h2 class="text-lg font-bold truncate">{{ comp.name }}</h2>
      </div>

      <!-- 基本属性 -->
      <div class="rounded-xl p-4 space-y-3 border" style="background-color: var(--color-bg-card); border-color: var(--color-border);">
        <h3 class="text-sm font-semibold" style="color: var(--color-text-muted);">基本属性</h3>
        <div>
          <label class="text-xs" style="color: var(--color-text-dim);">组件名称</label>
          <input v-model="compName" @change="save" class="w-full mt-1 px-3 py-1.5 rounded-lg text-sm focus:outline-none" style="background-color: var(--color-bg-surface); border: 1px solid var(--color-border); color: var(--color-text);" />
        </div>
        <div>
          <label class="text-xs" style="color: var(--color-text-dim);">描述</label>
          <textarea v-model="compDescription" @change="save" rows="2" class="w-full mt-1 px-3 py-1.5 rounded-lg text-sm focus:outline-none resize-none" style="background-color: var(--color-bg-surface); border: 1px solid var(--color-border); color: var(--color-text);" />
        </div>
        <div>
          <label class="text-xs" style="color: var(--color-text-dim);">Script 语法</label>
          <div class="flex items-center gap-2 mt-1">
            <button
              class="px-3 py-1 text-xs rounded-lg transition-colors"
              :style="scriptSyntax === 'setup' ? 'background-color: var(--color-primary); color: white;' : 'background-color: var(--color-bg-surface); color: var(--color-text-muted);'"
              @click="scriptSyntax = 'setup'"
            >setup 语法糖</button>
            <button
              class="px-3 py-1 text-xs rounded-lg transition-colors"
              :style="scriptSyntax === 'options' ? 'background-color: var(--color-primary); color: white;' : 'background-color: var(--color-bg-surface); color: var(--color-text-muted);'"
              @click="scriptSyntax = 'options'"
            >Options API</button>
          </div>
        </div>
      </div>

      <!-- 数据引入 -->
      <div class="rounded-xl p-4 space-y-3 border" style="background-color: var(--color-bg-card); border-color: var(--color-border);">
        <div class="flex items-center justify-between">
          <h3 class="text-sm font-semibold" style="color: var(--color-text-muted);">数据引入</h3>
          <button @click="addDataBinding" class="text-xs" style="color: var(--color-primary);">+ 添加</button>
        </div>
        <p class="text-xs" style="color: var(--color-text-dim);">通过 JSAPI 获取设备数据并注入组件</p>

        <div v-if="dataBindings.length === 0" class="text-xs text-center py-2" style="color: var(--color-text-dim);">
          暂无数据绑定
        </div>
        <div v-for="db in dataBindings" :key="db.id" class="rounded-lg p-2 space-y-1.5" style="background-color: var(--color-bg-surface);">
          <div class="flex items-center justify-between">
            <input v-model="db.name" class="flex-1 px-2 py-0.5 rounded text-xs focus:outline-none" style="background-color: var(--color-bg); border: 1px solid var(--color-border); color: var(--color-text);" placeholder="变量名" />
            <button @click="removeDataBinding(db.id)" class="ml-1 hover:text-red-400" style="color: var(--color-text-dim);">
              <Icon icon="solar:close-circle-bold" class="text-sm" />
            </button>
          </div>
          <div class="flex gap-1">
            <select v-model="db.source" class="px-1 py-0.5 rounded text-xs" style="background-color: var(--color-bg); border: 1px solid var(--color-border); color: var(--color-text);">
              <option value="pc">PC端</option>
              <option value="mobile">移动端</option>
            </select>
            <select v-model="db.api" class="flex-1 px-1 py-0.5 rounded text-xs" style="background-color: var(--color-bg); border: 1px solid var(--color-border); color: var(--color-text);">
              <option v-for="s in jsApiSources" :key="s.value" :value="s.value">{{ s.label }}</option>
            </select>
          </div>
          <div class="flex items-center gap-1">
            <label class="text-xs" style="color: var(--color-text-dim);">刷新(ms)</label>
            <input v-model.number="db.interval" type="number" class="flex-1 px-2 py-0.5 rounded text-xs focus:outline-none" style="background-color: var(--color-bg); border: 1px solid var(--color-border); color: var(--color-text);" />
          </div>
        </div>

        <button
          v-if="dataBindings.length > 0"
          @click="scriptCode = generateDataImportCode()"
          class="w-full px-2 py-1.5 rounded text-xs text-white"
          style="background-color: var(--color-primary);"
        >
          生成数据引入代码
        </button>
      </div>

      <!-- 静态资源 -->
      <div class="rounded-xl p-4 space-y-3 border" style="background-color: var(--color-bg-card); border-color: var(--color-border);">
        <div class="flex items-center justify-between">
          <h3 class="text-sm font-semibold" style="color: var(--color-text-muted);">静态资源</h3>
          <button @click="addAsset" class="text-xs" style="color: var(--color-primary);">+ 添加</button>
        </div>
        <div v-if="comp.assets.length === 0" class="text-xs text-center py-2" style="color: var(--color-text-dim);">
          暂无资源
        </div>
        <div v-for="asset in comp.assets" :key="asset.id" class="rounded-lg p-2 space-y-1" style="background-color: var(--color-bg-surface);">
          <div class="flex items-center justify-between">
            <input :value="asset.name" @change="updateAsset(asset.id, 'name', ($event.target as HTMLInputElement).value)"
              class="flex-1 px-2 py-0.5 rounded text-xs focus:outline-none" style="background-color: var(--color-bg); color: var(--color-text);" />
            <button @click="removeAsset(asset.id)" class="ml-1 hover:text-red-400" style="color: var(--color-text-dim);">
              <Icon icon="solar:close-circle-bold" class="text-sm" />
            </button>
          </div>
          <div class="flex gap-1">
            <select :value="asset.type" @change="updateAsset(asset.id, 'type', ($event.target as HTMLSelectElement).value)"
              class="px-1 py-0.5 rounded text-xs" style="background-color: var(--color-bg); border: 1px solid var(--color-border); color: var(--color-text);">
              <option value="image">图片</option>
              <option value="video">视频</option>
            </select>
          </div>
          <div class="flex gap-1">
            <input :value="asset.url" @change="updateAsset(asset.id, 'url', ($event.target as HTMLInputElement).value)"
              placeholder="URL 或本地文件" class="flex-1 px-2 py-0.5 rounded text-xs focus:outline-none" style="background-color: var(--color-bg); border: 1px solid var(--color-border); color: var(--color-text);" />
            <button @click="selectLocalFile(asset.id)" class="px-2 py-0.5 rounded text-xs" style="background-color: var(--color-primary); color: white;" title="选择本地文件">
              <Icon icon="solar:folder-open-bold" class="text-xs" />
            </button>
          </div>
          <!-- 本地文件预览 -->
          <div v-if="asset.url && asset.url.startsWith('data:')" class="mt-1">
            <img v-if="asset.type === 'image'" :src="asset.url" class="w-full h-16 object-cover rounded" />
            <p v-else class="text-xs" style="color: var(--color-text-dim);">已加载本地文件</p>
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
          :style="activeTab === tab ? 'background-color: var(--color-primary); color: white;' : 'color: var(--color-text-muted);'"
          @click="activeTab = tab"
        >
          {{ tab === 'template' ? '模板' : tab === 'script' ? `逻辑 (${scriptSyntax === 'setup' ? 'setup' : 'options'})` : '样式' }}
        </button>
        <div class="flex-1" />
        <button @click="save" class="px-3 py-1.5 rounded-lg text-xs text-white transition-colors" style="background-color: var(--color-primary);">
          保存
        </button>
        <button @click="generatePreview" class="px-3 py-1.5 rounded-lg text-xs text-white transition-colors bg-emerald-600 hover:bg-emerald-500">
          预览
        </button>
      </div>

      <!-- 代码编辑区 -->
      <div class="flex-1 min-h-0">
        <textarea
          v-model="templateCode"
          v-show="activeTab === 'template'"
          class="w-full h-full rounded-xl p-4 font-mono text-sm resize-none focus:outline-none"
          style="background-color: var(--color-bg-card); border: 1px solid var(--color-border); color: var(--color-text);"
          spellcheck="false"
          placeholder="<div class='comp'>...</div>"
        />
        <textarea
          v-model="scriptCode"
          v-show="activeTab === 'script'"
          class="w-full h-full rounded-xl p-4 font-mono text-sm resize-none focus:outline-none"
          style="background-color: var(--color-bg-card); border: 1px solid var(--color-border); color: var(--color-text);"
          spellcheck="false"
          :placeholder="scriptSyntax === 'setup' ? '&lt;script setup lang=ts&gt;...' : 'export default { ... }'"
        />
        <textarea
          v-model="styleCode"
          v-show="activeTab === 'style'"
          class="w-full h-full rounded-xl p-4 font-mono text-sm resize-none focus:outline-none"
          style="background-color: var(--color-bg-card); border: 1px solid var(--color-border); color: var(--color-text);"
          spellcheck="false"
          placeholder=".comp { ... }"
        />
      </div>
    </div>

    <!-- 右侧：实时预览 -->
    <div class="w-80 shrink-0">
      <div class="rounded-xl overflow-hidden border" style="background-color: var(--color-bg-card); border-color: var(--color-border);">
        <div class="flex items-center justify-between px-3 py-2 border-b" style="border-color: var(--color-border-subtle);">
          <h3 class="text-xs font-semibold" style="color: var(--color-text-dim);">预览</h3>
          <button @click="generatePreview" style="color: var(--color-primary);">
            <Icon icon="solar:refresh-bold" class="text-sm" />
          </button>
        </div>
        <div class="aspect-[9/16] flex items-center justify-center" style="background-color: var(--color-bg);">
          <div v-if="previewHtml" class="w-full h-full" v-html="previewHtml" />
          <div v-else class="text-center" style="color: var(--color-text-dim);">
            <Icon icon="solar:eye-bold" class="text-3xl mb-2 mx-auto block" />
            <p class="text-xs">点击"预览"按钮查看效果</p>
          </div>
        </div>
      </div>
    </div>
  </div>

  <div v-else class="text-center py-20" style="color: var(--color-text-dim);">
    <p>组件未找到</p>
    <button class="mt-4 px-4 py-2 rounded-lg text-sm" style="background-color: var(--color-bg-surface);" @click="goBack">返回</button>
  </div>
</template>
