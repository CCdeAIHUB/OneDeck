<script setup lang="ts">
import { Icon } from '@iconify/vue'
import { useDesignStore, type ComponentAsset } from '@/stores/design'
import { useDeviceStore } from '@/stores/devices'
import { useRoute, useRouter } from 'vue-router'
import { computed, ref, watch } from 'vue'

const route = useRoute()
const router = useRouter()
const designStore = useDesignStore()
const deviceStore = useDeviceStore()

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
const showHelp = ref(false)

// 默认代码模板
const SETUP_DEFAULT = `import { ref, onUnmounted } from 'vue'\nimport { useSharedParams } from '@/stores/sharedParams'\n\nconst message = ref('Hello')\nconst sharedParams = useSharedParams()\n\n// 通过 JSAPI 获取设备数据\n// const battery = ref(null)\n// async function fetchBattery() {\n//   battery.value = await context.callApi('device.battery')\n// }`

const OPTIONS_DEFAULT = `export default {\n  data() {\n    return { message: 'Hello' }\n  },\n  mounted() {\n    // 通过 JSAPI 获取设备数据\n    // this.$context.callApi('device.battery').then(data => { ... })\n  }\n}`

// 标记用户是否手动编辑过代码
const hasUserEdits = ref(false)

interface DataBinding {
  id: string
  name: string
  source: 'pc' | 'mobile'
  api: string
  interval: number
}
const dataBindings = ref<DataBinding[]>([])

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

// 监听代码变化标记用户编辑
watch([templateCode, scriptCode, styleCode], () => {
  hasUserEdits.value = true
}, { once: true })

function switchScriptSyntax(newSyntax: 'options' | 'setup') {
  if (scriptSyntax.value === newSyntax) return
  if (hasUserEdits.value && scriptCode.value.trim()) {
    if (!confirm('切换语法将替换当前逻辑代码内容，确定切换？')) return
  }
  scriptSyntax.value = newSyntax
  scriptCode.value = newSyntax === 'setup' ? SETUP_DEFAULT : OPTIONS_DEFAULT
  hasUserEdits.value = false
}

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
  const asset: ComponentAsset = { id: crypto.randomUUID().slice(0, 8), name: '新资源', type: 'image', url: '', size: 0 }
  designStore.updateComponent({ ...comp.value, assets: [...comp.value.assets, asset] })
}

function removeAsset(assetId: string) {
  if (!comp.value) return
  designStore.updateComponent({ ...comp.value, assets: comp.value.assets.filter((a) => a.id !== assetId) })
}

function updateAsset(assetId: string, key: string, value: unknown) {
  if (!comp.value) return
  const assets = comp.value.assets.map((a) => a.id === assetId ? { ...a, [key]: value } : a)
  designStore.updateComponent({ ...comp.value, assets })
}

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

function addDataBinding() {
  dataBindings.value.push({ id: crypto.randomUUID().slice(0, 8), name: 'newData', source: 'mobile', api: 'device.battery', interval: 5000 })
}

function removeDataBinding(id: string) {
  dataBindings.value = dataBindings.value.filter(d => d.id !== id)
}

function generateDataImportCode(): string {
  if (dataBindings.value.length === 0) return ''
  const lines = dataBindings.value.map(d => {
    const varName = d.name.replace(/[^a-zA-Z0-9_]/g, '_')
    const refName = `${varName}Ref`
    if (scriptSyntax.value === 'setup') {
      return `// ${d.source === 'pc' ? 'PC端' : '移动端'} ${d.api} (每${d.interval}ms刷新)\nconst ${refName} = ref(null)\nasync function fetch${varName}() {\n  try {\n    ${refName}.value = await context.callApi('${d.api}')\n  } catch (e) { console.error('获取${d.name}失败:', e) }\n}\nconst ${varName}Timer = setInterval(fetch${varName}, ${d.interval})\nfetch${varName}()\nonUnmounted(() => clearInterval(${varName}Timer))`
    } else {
      return `// ${d.source === 'pc' ? 'PC端' : '移动端'} ${d.api}`
    }
  })
  return lines.join('\n\n')
}

function generatePreview() {
  const html = `<div style="display:flex;align-items:center;justify-content:center;width:100%;height:100%;background:#111827;color:white;"><style>${styleCode.value}</style><div class="comp">${templateCode.value.replace(/\{\{.*?\}\}/g, '预览')}</div></div>`
  previewHtml.value = html
}

function goBack() { router.push('/components') }

// 预览比例 - 基于选中设备
const previewAspect = computed(() => {
  const device = deviceStore.selectedDevice
  if (device && device.screenWidth && device.screenHeight) {
    return `${device.screenWidth}/${device.screenHeight}`
  }
  return '9/16'
})

// JSAPI 帮助
const jsApiHelp = [
  { api: 'device.battery', desc: '电池信息' },
  { api: 'device.network', desc: '网络状态' },
  { api: 'device.screen', desc: '屏幕信息' },
  { api: 'device.storage', desc: '存储信息' },
  { api: 'device.memory', desc: '内存信息' },
  { api: 'device.model', desc: '设备型号' },
  { api: 'location.current', desc: '当前位置' },
  { api: 'clipboard.read', desc: '剪贴板' },
  { api: 'storage.get/set', desc: '存储数据' },
]

const devGuide = [
  { title: '引入公共参数', code: `import { useSharedParams } from '@/stores/sharedParams'\nconst sharedParams = useSharedParams()\nconst value = sharedParams.get('key')\nsharedParams.set('key', 'value')` },
  { title: '引入静态资源', code: `// 在 assets 中添加资源后\nconst url = assets.find(a => a.name === 'bg.png')?.url` },
  { title: '调用 JSAPI', code: `const data = await context.callApi('device.battery')` },
]
</script>

<template>
  <div v-if="comp" class="flex gap-6 h-[calc(100vh-120px)]">
    <!-- 左侧 -->
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
            <button class="px-3 py-1 text-xs rounded-lg transition-colors" :style="scriptSyntax === 'setup' ? 'background-color: var(--color-primary); color: white;' : 'background-color: var(--color-bg-surface); color: var(--color-text-muted);'" @click="switchScriptSyntax('setup')">setup 语法糖</button>
            <button class="px-3 py-1 text-xs rounded-lg transition-colors" :style="scriptSyntax === 'options' ? 'background-color: var(--color-primary); color: white;' : 'background-color: var(--color-bg-surface); color: var(--color-text-muted);'" @click="switchScriptSyntax('options')">Options API</button>
          </div>
        </div>
      </div>

      <!-- 数据引入 -->
      <div class="rounded-xl p-4 space-y-3 border" style="background-color: var(--color-bg-card); border-color: var(--color-border);">
        <div class="flex items-center justify-between">
          <h3 class="text-sm font-semibold" style="color: var(--color-text-muted);">数据引入</h3>
          <button @click="addDataBinding" class="text-xs" style="color: var(--color-primary);">+ 添加</button>
        </div>
        <p class="text-xs" style="color: var(--color-text-dim);">通过 JSAPI 获取设备数据</p>
        <div v-if="dataBindings.length === 0" class="text-xs text-center py-2" style="color: var(--color-text-dim);">暂无数据绑定</div>
        <div v-for="db in dataBindings" :key="db.id" class="rounded-lg p-2 space-y-1.5" style="background-color: var(--color-bg-surface);">
          <div class="flex items-center justify-between">
            <input v-model="db.name" class="flex-1 px-2 py-0.5 rounded text-xs focus:outline-none" style="background-color: var(--color-bg); border: 1px solid var(--color-border); color: var(--color-text);" placeholder="变量名" />
            <button @click="removeDataBinding(db.id)" class="ml-1 hover:text-red-400" style="color: var(--color-text-dim);"><Icon icon="solar:close-circle-bold" class="text-sm" /></button>
          </div>
          <div class="flex gap-1">
            <select v-model="db.source" class="px-1 py-0.5 rounded text-xs" style="background-color: var(--color-bg); border: 1px solid var(--color-border); color: var(--color-text);"><option value="pc">PC端</option><option value="mobile">移动端</option></select>
            <select v-model="db.api" class="flex-1 px-1 py-0.5 rounded text-xs" style="background-color: var(--color-bg); border: 1px solid var(--color-border); color: var(--color-text);"><option v-for="s in jsApiSources" :key="s.value" :value="s.value">{{ s.label }}</option></select>
          </div>
          <div class="flex items-center gap-1">
            <label class="text-xs" style="color: var(--color-text-dim);">刷新(ms)</label>
            <input v-model.number="db.interval" type="number" class="flex-1 px-2 py-0.5 rounded text-xs focus:outline-none" style="background-color: var(--color-bg); border: 1px solid var(--color-border); color: var(--color-text);" />
          </div>
        </div>
        <button v-if="dataBindings.length > 0" @click="scriptCode = generateDataImportCode()" class="w-full px-2 py-1.5 rounded text-xs text-white" style="background-color: var(--color-primary);">生成数据引入代码</button>
      </div>

      <!-- 静态资源 -->
      <div class="rounded-xl p-4 space-y-3 border" style="background-color: var(--color-bg-card); border-color: var(--color-border);">
        <div class="flex items-center justify-between">
          <h3 class="text-sm font-semibold" style="color: var(--color-text-muted);">静态资源</h3>
          <button @click="addAsset" class="text-xs" style="color: var(--color-primary);">+ 添加</button>
        </div>
        <div v-if="comp.assets.length === 0" class="text-xs text-center py-2" style="color: var(--color-text-dim);">暂无资源</div>
        <div v-for="asset in comp.assets" :key="asset.id" class="rounded-lg p-2 space-y-1" style="background-color: var(--color-bg-surface);">
          <div class="flex items-center justify-between">
            <input :value="asset.name" @change="updateAsset(asset.id, 'name', ($event.target as HTMLInputElement).value)" class="flex-1 px-2 py-0.5 rounded text-xs focus:outline-none" style="background-color: var(--color-bg); color: var(--color-text);" />
            <button @click="removeAsset(asset.id)" class="ml-1 hover:text-red-400" style="color: var(--color-text-dim);"><Icon icon="solar:close-circle-bold" class="text-sm" /></button>
          </div>
          <div class="flex gap-1">
            <select :value="asset.type" @change="updateAsset(asset.id, 'type', ($event.target as HTMLSelectElement).value)" class="px-1 py-0.5 rounded text-xs" style="background-color: var(--color-bg); border: 1px solid var(--color-border); color: var(--color-text);"><option value="image">图片</option><option value="video">视频</option></select>
          </div>
          <div class="flex gap-1">
            <input :value="asset.url" @change="updateAsset(asset.id, 'url', ($event.target as HTMLInputElement).value)" placeholder="URL 或本地文件" class="flex-1 px-2 py-0.5 rounded text-xs focus:outline-none" style="background-color: var(--color-bg); border: 1px solid var(--color-border); color: var(--color-text);" />
            <button @click="selectLocalFile(asset.id)" class="px-2 py-0.5 rounded text-xs" style="background-color: var(--color-primary); color: white;" title="选择本地文件"><Icon icon="solar:folder-open-bold" class="text-xs" /></button>
          </div>
          <div v-if="asset.url && asset.url.startsWith('data:')" class="mt-1">
            <img v-if="asset.type === 'image'" :src="asset.url" class="w-full h-16 object-cover rounded" />
            <p v-else class="text-xs" style="color: var(--color-text-dim);">已加载本地文件</p>
          </div>
        </div>
      </div>

      <!-- 帮助按钮 -->
      <button @click="showHelp = !showHelp" class="w-full px-3 py-2 rounded-lg text-sm transition-colors border flex items-center justify-center gap-1" style="border-color: var(--color-border); color: var(--color-primary);">
        <Icon icon="solar:question-circle-bold" class="text-base" />
        {{ showHelp ? '关闭帮助' : '开发帮助' }}
      </button>
    </div>

    <!-- 中间：代码编辑器 -->
    <div class="flex-1 flex flex-col min-w-0">
      <div class="flex items-center gap-1 mb-2 shrink-0">
        <button v-for="tab in (['template', 'script', 'style'] as const)" :key="tab" class="px-3 py-1.5 text-xs rounded-lg transition-colors" :style="activeTab === tab ? 'background-color: var(--color-primary); color: white;' : 'color: var(--color-text-muted);'" @click="activeTab = tab">
          {{ tab === 'template' ? '模板' : tab === 'script' ? `逻辑 (${scriptSyntax === 'setup' ? 'setup' : 'options'})` : '样式' }}
        </button>
        <div class="flex-1" />
        <button @click="save" class="px-3 py-1.5 rounded-lg text-xs text-white transition-colors" style="background-color: var(--color-primary);">保存</button>
        <button @click="generatePreview" class="px-3 py-1.5 rounded-lg text-xs text-white transition-colors bg-emerald-600 hover:bg-emerald-500">预览</button>
      </div>
      <div class="flex-1 min-h-0">
        <textarea v-model="templateCode" v-show="activeTab === 'template'" class="w-full h-full rounded-xl p-4 font-mono text-sm resize-none focus:outline-none" style="background-color: var(--color-bg-card); border: 1px solid var(--color-border); color: var(--color-text);" spellcheck="false" placeholder="<div class='comp'>...</div>" />
        <textarea v-model="scriptCode" v-show="activeTab === 'script'" class="w-full h-full rounded-xl p-4 font-mono text-sm resize-none focus:outline-none" style="background-color: var(--color-bg-card); border: 1px solid var(--color-border); color: var(--color-text);" spellcheck="false" :placeholder="scriptSyntax === 'setup' ? 'setup syntax...' : 'export default { ... }'" />
        <textarea v-model="styleCode" v-show="activeTab === 'style'" class="w-full h-full rounded-xl p-4 font-mono text-sm resize-none focus:outline-none" style="background-color: var(--color-bg-card); border: 1px solid var(--color-border); color: var(--color-text);" spellcheck="false" placeholder=".comp { ... }" />
      </div>
    </div>

    <!-- 右侧 -->
    <div class="w-80 shrink-0 overflow-y-auto space-y-4">
      <!-- 预览 -->
      <div class="rounded-xl overflow-hidden border" style="background-color: var(--color-bg-card); border-color: var(--color-border);">
        <div class="flex items-center justify-between px-3 py-2 border-b" style="border-color: var(--color-border-subtle);">
          <h3 class="text-xs font-semibold" style="color: var(--color-text-dim);">预览</h3>
          <button @click="generatePreview" style="color: var(--color-primary);"><Icon icon="solar:refresh-bold" class="text-sm" /></button>
        </div>
        <div :style="{ aspectRatio: previewAspect }" class="flex items-center justify-center" style="background-color: var(--color-bg);">
          <div v-if="previewHtml" class="w-full h-full" v-html="previewHtml" />
          <div v-else class="text-center" style="color: var(--color-text-dim);">
            <Icon icon="solar:eye-bold" class="text-3xl mb-2 mx-auto block" />
            <p class="text-xs">点击"预览"查看效果</p>
          </div>
        </div>
        <div class="px-3 py-1.5 text-xs" style="color: var(--color-text-dim); border-top: 1px solid var(--color-border-subtle);">
          {{ deviceStore.selectedDevice ? `${deviceStore.selectedDevice.deviceName} (${deviceStore.selectedDevice.screenWidth}x${deviceStore.selectedDevice.screenHeight})` : '默认比例 9:16' }}
        </div>
      </div>

      <!-- 帮助面板 -->
      <div v-if="showHelp" class="rounded-xl p-4 space-y-3 border" style="background-color: var(--color-bg-card); border-color: var(--color-border);">
        <h3 class="text-sm font-semibold" style="color: var(--color-text-muted);">可用 JSAPI</h3>
        <div v-for="api in jsApiHelp" :key="api.api" class="px-2 py-1 rounded" style="background-color: var(--color-bg-surface);">
          <p class="text-xs font-mono" style="color: var(--color-primary);">{{ api.api }}</p>
          <p class="text-xs" style="color: var(--color-text-dim);">{{ api.desc }}</p>
        </div>
        <h3 class="text-sm font-semibold pt-2" style="color: var(--color-text-muted);">开发指引</h3>
        <div v-for="guide in devGuide" :key="guide.title" class="space-y-1">
          <p class="text-xs font-semibold" style="color: var(--color-text);">{{ guide.title }}</p>
          <pre class="text-[10px] font-mono p-2 rounded overflow-x-auto" style="background-color: var(--color-bg-surface); color: var(--color-text-muted);">{{ guide.code }}</pre>
        </div>
      </div>
    </div>
  </div>

  <div v-else class="text-center py-20" style="color: var(--color-text-dim);">
    <p>组件未找到</p>
    <button class="mt-4 px-4 py-2 rounded-lg text-sm" style="background-color: var(--color-bg-surface);" @click="goBack">返回</button>
  </div>
</template>
