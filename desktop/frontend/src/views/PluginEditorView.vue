<script setup lang="ts">
import { Icon } from '@iconify/vue'
import { usePluginStore } from '@/stores/plugins'
import { useRoute, useRouter } from 'vue-router'
import { computed, ref, watch } from 'vue'

const route = useRoute()
const router = useRouter()
const pluginStore = usePluginStore()

const pluginId = computed(() => route.params.id as string)
const plugin = computed(() => pluginStore.plugins.find(p => p.id === pluginId.value))

// 项目文件模型
interface ProjectFile {
  id: string
  name: string
  path: string
  content: string
  language: 'vue' | 'js' | 'ts' | 'css' | 'json'
}

const files = ref<ProjectFile[]>([])
const activeFileId = ref<string | null>(null)
const activeFile = computed(() => files.value.find(f => f.id === activeFileId.value))

const pluginName = ref('')
const pluginDescription = ref('')
const pluginAuthor = ref('')
const showHelp = ref(false)

// 初始化默认文件
watch(plugin, (p) => {
  if (!p) return
  pluginName.value = p.name
  pluginDescription.value = p.description
  pluginAuthor.value = p.author
  // 如果还没有文件，创建默认文件结构
  if (files.value.length === 0) {
    files.value = [
      { id: 'f1', name: 'index.vue', path: '/index.vue', content: '<template>\n  <div class="plugin-container">\n    <span>{{ message }}</span>\n  </div>\n<\/template>\n\n<script setup lang="ts">\nimport { ref } from \'vue\'\nimport { useSharedParams } from \'@/stores/sharedParams\'\n\nconst message = ref(\'Hello Plugin\')\nconst sharedParams = useSharedParams()\n<\/script>\n\n<style scoped>\n.plugin-container {\n  display: flex;\n  align-items: center;\n  justify-content: center;\n  height: 100%;\n}\n<\/style>', language: 'vue' },
      { id: 'f2', name: 'manifest.json', path: '/manifest.json', content: `{\n  "name": "${p.name}",\n  "version": "1.0.0",\n  "description": "",\n  "author": "",\n  "main": "index.vue",\n  "permissions": []\n}`, language: 'json' },
    ]
    activeFileId.value = 'f1'
  }
}, { immediate: true })

function addFile() {
  const name = prompt('输入文件名（如 utils.ts）')
  if (!name) return
  const ext = name.split('.').pop() ?? ''
  const langMap: Record<string, ProjectFile['language']> = { vue: 'vue', js: 'js', ts: 'ts', css: 'css', json: 'json' }
  const file: ProjectFile = {
    id: crypto.randomUUID().slice(0, 8),
    name,
    path: `/${name}`,
    content: '',
    language: langMap[ext] ?? 'js',
  }
  files.value.push(file)
  activeFileId.value = file.id
}

function deleteFile(fileId: string) {
  if (files.value.length <= 1) return alert('至少保留一个文件')
  files.value = files.value.filter(f => f.id !== fileId)
  if (activeFileId.value === fileId) {
    activeFileId.value = files.value[0]?.id ?? null
  }
}

function importFolder() {
  const input = document.createElement('input')
  input.type = 'file'
  input.webkitdirectory = true
  input.onchange = (e) => {
    const fileList = (e.target as HTMLInputElement).files
    if (!fileList) return
    for (const file of Array.from(fileList)) {
      const reader = new FileReader()
      reader.onload = (ev) => {
        const ext = file.name.split('.').pop() ?? ''
        const langMap: Record<string, ProjectFile['language']> = { vue: 'vue', js: 'js', ts: 'ts', css: 'css', json: 'json' }
        const pf: ProjectFile = {
          id: crypto.randomUUID().slice(0, 8),
          name: file.name,
          path: `/${file.webkitRelativePath}`,
          content: ev.target?.result as string,
          language: langMap[ext] ?? 'js',
        }
        files.value.push(pf)
      }
      reader.readAsText(file)
    }
  }
  input.click()
}

function save() {
  if (!plugin.value) return
  // 更新插件元数据
  const idx = pluginStore.plugins.findIndex(p => p.id === pluginId.value)
  if (idx >= 0) {
    pluginStore.plugins[idx] = {
      ...pluginStore.plugins[idx],
      name: pluginName.value,
      description: pluginDescription.value,
      author: pluginAuthor.value,
    }
  }
  // TODO: 保存文件内容到后端
}

function exportPlugin() {
  if (!plugin.value) return
  const data = {
    ...plugin.value,
    name: pluginName.value,
    description: pluginDescription.value,
    author: pluginAuthor.value,
    files: files.value.map(f => ({ name: f.name, path: f.path, content: f.content })),
  }
  const blob = new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = `plugin-${pluginName.value}-${Date.now()}.json`
  a.click()
  URL.revokeObjectURL(url)
}

function goBack() {
  router.push('/plugins')
}

// JSAPI 帮助列表
const jsApiHelp = [
  { api: 'device.battery', desc: '电池信息（电量、充电状态）' },
  { api: 'device.network', desc: '网络状态（类型、信号）' },
  { api: 'device.screen', desc: '屏幕信息（宽高、DPI）' },
  { api: 'device.storage', desc: '存储信息（总量、可用）' },
  { api: 'device.memory', desc: '内存信息（总量、可用）' },
  { api: 'device.model', desc: '设备型号' },
  { api: 'location.current', desc: '当前位置（经纬度）' },
  { api: 'clipboard.read', desc: '读取剪贴板内容' },
  { api: 'network.status', desc: '网络连接状态' },
  { api: 'storage.get', desc: '读取存储数据' },
  { api: 'storage.set', desc: '写入存储数据' },
  { api: 'notification.send', desc: '发送通知' },
  { api: 'file.read', desc: '读取文件' },
  { api: 'file.write', desc: '写入文件' },
]

const devGuide = [
  { title: '引入公共参数', code: `import { useSharedParams } from '@/stores/sharedParams'\nconst sharedParams = useSharedParams()\nconst value = sharedParams.get('key')\nsharedParams.set('key', 'value')` },
  { title: '引入静态资源', code: `// 在 template 中直接引用资源 URL\n&lt;img :src="assetUrl" /&gt;\n// 或在 script 中获取\nconst assetUrl = assets.find(a => a.name === 'bg.png')?.url` },
  { title: '调用 JSAPI', code: `// 通过 context.callApi 调用\nconst battery = await context.callApi('device.battery')\nconsole.log(battery.level)` },
  { title: '组件间通信', code: `// 通过公共参数库\nsharedParams.set('myData', data)\n// 在其他组件中监听\nwatch(() => sharedParams.get('myData'), (val) => { ... })` },
]
</script>

<template>
  <div v-if="plugin" class="flex gap-6 h-[calc(100vh-120px)]">
    <!-- 左侧：属性 & 文件树 -->
    <div class="w-64 shrink-0 overflow-y-auto space-y-4">
      <div class="flex items-center gap-2 mb-2">
        <button class="p-1.5 rounded-lg transition-colors" style="color: var(--color-text-muted);" @click="goBack">
          <Icon icon="solar:alt-arrow-left-bold" class="text-lg" />
        </button>
        <h2 class="text-lg font-bold truncate">{{ plugin.name }}</h2>
      </div>

      <!-- 基本属性 -->
      <div class="rounded-xl p-4 space-y-3 border" style="background-color: var(--color-bg-card); border-color: var(--color-border);">
        <h3 class="text-sm font-semibold" style="color: var(--color-text-muted);">基本属性</h3>
        <div>
          <label class="text-xs" style="color: var(--color-text-dim);">插件名称</label>
          <input v-model="pluginName" @change="save" class="w-full mt-1 px-3 py-1.5 rounded-lg text-sm focus:outline-none" style="background-color: var(--color-bg-surface); border: 1px solid var(--color-border); color: var(--color-text);" />
        </div>
        <div>
          <label class="text-xs" style="color: var(--color-text-dim);">描述</label>
          <textarea v-model="pluginDescription" @change="save" rows="2" class="w-full mt-1 px-3 py-1.5 rounded-lg text-sm focus:outline-none resize-none" style="background-color: var(--color-bg-surface); border: 1px solid var(--color-border); color: var(--color-text);" />
        </div>
        <div>
          <label class="text-xs" style="color: var(--color-text-dim);">作者</label>
          <input v-model="pluginAuthor" @change="save" class="w-full mt-1 px-3 py-1.5 rounded-lg text-sm focus:outline-none" style="background-color: var(--color-bg-surface); border: 1px solid var(--color-border); color: var(--color-text);" />
        </div>
      </div>

      <!-- 项目文件 -->
      <div class="rounded-xl p-4 space-y-3 border" style="background-color: var(--color-bg-card); border-color: var(--color-border);">
        <div class="flex items-center justify-between">
          <h3 class="text-sm font-semibold" style="color: var(--color-text-muted);">项目文件</h3>
          <div class="flex items-center gap-1">
            <button @click="importFolder" class="text-xs" style="color: var(--color-primary);" title="导入文件夹">
              <Icon icon="solar:folder-open-bold" class="text-sm" />
            </button>
            <button @click="addFile" class="text-xs" style="color: var(--color-primary);" title="新建文件">+</button>
          </div>
        </div>

        <div v-for="file in files" :key="file.id"
          class="flex items-center gap-2 px-2 py-1.5 rounded-lg cursor-pointer transition-colors"
          :style="activeFileId === file.id ? 'background-color: var(--color-primary); color: white;' : 'color: var(--color-text-muted);'"
          @click="activeFileId = file.id"
        >
          <Icon :icon="file.language === 'vue' ? 'solar:file-text-bold' : file.language === 'json' ? 'solar:settings-bold' : 'solar:code-bold'" class="text-sm" />
          <span class="text-xs flex-1 truncate">{{ file.name }}</span>
          <button v-if="files.length > 1" @click.stop="deleteFile(file.id)" class="opacity-0 hover:opacity-100 transition-opacity" style="color: inherit;">
            <Icon icon="solar:close-circle-bold" class="text-xs" />
          </button>
        </div>
      </div>

      <!-- 操作按钮 -->
      <div class="flex gap-2">
        <button @click="save" class="flex-1 px-3 py-2 rounded-lg text-sm text-white transition-colors" style="background-color: var(--color-primary);">
          <Icon icon="solar:diskette-bold" class="inline mr-1" />保存
        </button>
        <button @click="exportPlugin" class="px-3 py-2 rounded-lg text-sm transition-colors border" style="border-color: var(--color-border); color: var(--color-text-muted);">
          <Icon icon="solar:download-bold" class="inline mr-1" />导出
        </button>
      </div>
      <button @click="showHelp = !showHelp" class="w-full px-3 py-2 rounded-lg text-sm transition-colors border flex items-center justify-center gap-1" style="border-color: var(--color-border); color: var(--color-primary);">
        <Icon icon="solar:question-circle-bold" class="text-base" />
        {{ showHelp ? '关闭帮助' : '开发帮助' }}
      </button>
    </div>

    <!-- 中间：代码编辑器 -->
    <div class="flex-1 flex flex-col min-w-0">
      <div class="flex items-center gap-2 mb-2 shrink-0">
        <span class="text-xs px-2 py-1 rounded" style="background-color: var(--color-bg-surface); color: var(--color-text-muted);">
          {{ activeFile?.name ?? '未选择文件' }}
        </span>
        <div class="flex-1" />
        <button @click="save" class="px-3 py-1.5 rounded-lg text-xs text-white transition-colors" style="background-color: var(--color-primary);">保存</button>
      </div>

      <div class="flex-1 min-h-0">
        <textarea
          v-if="activeFile"
          v-model="activeFile.content"
          class="w-full h-full rounded-xl p-4 font-mono text-sm resize-none focus:outline-none"
          style="background-color: var(--color-bg-card); border: 1px solid var(--color-border); color: var(--color-text);"
          spellcheck="false"
        />
        <div v-else class="flex items-center justify-center h-full rounded-xl" style="background-color: var(--color-bg-card); border: 1px solid var(--color-border); color: var(--color-text-dim);">
          <p class="text-sm">请选择或创建文件</p>
        </div>
      </div>
    </div>

    <!-- 右侧：帮助面板 -->
    <div v-if="showHelp" class="w-80 shrink-0 overflow-y-auto space-y-4">
      <!-- JSAPI 列表 -->
      <div class="rounded-xl p-4 space-y-2 border" style="background-color: var(--color-bg-card); border-color: var(--color-border);">
        <h3 class="text-sm font-semibold" style="color: var(--color-text-muted);">可用 JSAPI</h3>
        <div v-for="api in jsApiHelp" :key="api.api" class="px-2 py-1.5 rounded" style="background-color: var(--color-bg-surface);">
          <p class="text-xs font-mono" style="color: var(--color-primary);">{{ api.api }}</p>
          <p class="text-xs" style="color: var(--color-text-dim);">{{ api.desc }}</p>
        </div>
      </div>

      <!-- 开发指引 -->
      <div class="rounded-xl p-4 space-y-3 border" style="background-color: var(--color-bg-card); border-color: var(--color-border);">
        <h3 class="text-sm font-semibold" style="color: var(--color-text-muted);">开发指引</h3>
        <div v-for="guide in devGuide" :key="guide.title" class="space-y-1">
          <p class="text-xs font-semibold" style="color: var(--color-text);">{{ guide.title }}</p>
          <pre class="text-[10px] font-mono p-2 rounded overflow-x-auto" style="background-color: var(--color-bg-surface); color: var(--color-text-muted);">{{ guide.code }}</pre>
        </div>
      </div>
    </div>
  </div>

  <div v-else class="text-center py-20" style="color: var(--color-text-dim);">
    <p>插件未找到</p>
    <button class="mt-4 px-4 py-2 rounded-lg text-sm" style="background-color: var(--color-bg-surface);" @click="goBack">返回</button>
  </div>
</template>
