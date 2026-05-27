<script setup lang="ts">
import { Icon } from '@iconify/vue'
import { usePluginStore } from '@/stores/plugins'
import { useNotificationStore } from '@/stores/notification'
import CodeEditor from '@/components/CodeEditor.vue'
import { useRoute, useRouter } from 'vue-router'
import { computed, ref, watch } from 'vue'

const route = useRoute()
const router = useRouter()
const pluginStore = usePluginStore()
const notify = useNotificationStore()

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

function addFolder() {
  const name = prompt('输入文件夹名称')
  if (!name) return
  // 创建一个占位文件来表示文件夹
  const file: ProjectFile = {
    id: crypto.randomUUID().slice(0, 8),
    name: `${name}/`,
    path: `/${name}/`,
    content: '',
    language: 'js',
  }
  files.value.push(file)
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
  notify.success('插件已保存')
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

/** 打开帮助窗口 */
function openHelp() {
  const apiItems = jsApiHelp.map(a => {
    const p = a.platform as string
    const tagColor = p === 'pc' ? '#3b82f6' : p === 'mobile' ? '#10b981' : '#f59e0b'
    const tagBg = p === 'pc' ? '#3b82f620' : p === 'mobile' ? '#10b98120' : '#f59e0b20'
    const tagText = p === 'pc' ? 'PC' : p === 'mobile' ? '移动端' : '双端'
    return `<div style="background:#1f2937;border-radius:8px;padding:10px 14px;margin:8px 0;display:flex;justify-content:space-between;align-items:center"><span style="font-family:monospace;color:#3b82f6;font-size:13px">${a.api}</span><span style="color:#9ca3af;font-size:12px">${a.desc} <span style="font-size:10px;padding:2px 6px;border-radius:4px;margin-left:6px;background:${tagBg};color:${tagColor}">${tagText}</span></span></div>`
  }).join('')

  const guideItems = devGuide.map(g => {
    return `<h4 style="color:#e5e7eb;margin:16px 0 4px">${g.title}</h4><pre style="background:#030712;border-radius:8px;padding:14px;overflow-x:auto;font-size:12px;color:#d1d5db">${g.code.replace(/</g, '&lt;').replace(/>/g, '&gt;')}</pre>`
  }).join('')

  const html = `<!DOCTYPE html><html><head><meta charset="utf-8"><title>OneDesk 插件开发帮助</title><style>body{font-family:system-ui,sans-serif;margin:0;padding:24px;background:#111827;color:#f9fafb;font-size:14px;line-height:1.6}h2{color:#3b82f6;margin-top:0}h3{color:#93c5fd}</style></head><body><h2>OneDesk 插件开发帮助</h2><h3>可用 JSAPI</h3>${apiItems}<h3>开发指引</h3>${guideItems}</body></html>`
  const w = window.open('', '_blank', 'width=600,height=700')
  if (w) { w.document.write(html); w.document.close() }
}

// JSAPI 帮助列表 — 区分 PC 端和移动端
const jsApiHelp = [
  // PC 端独有
  { api: 'pc.processList', desc: '获取进程列表', platform: 'pc' },
  { api: 'pc.processMemory', desc: '读取进程内存', platform: 'pc' },
  { api: 'pc.windowInfo', desc: '读取窗口信息', platform: 'pc' },
  { api: 'pc.clipboard.read', desc: '读取剪贴板', platform: 'pc' },
  { api: 'pc.clipboard.write', desc: '写入剪贴板', platform: 'pc' },
  { api: 'pc.screenshot', desc: '截取屏幕', platform: 'pc' },
  { api: 'pc.keyEvent', desc: '模拟按键事件', platform: 'pc' },
  { api: 'pc.mouseEvent', desc: '模拟鼠标事件', platform: 'pc' },
  { api: 'pc.systemInfo', desc: '系统信息', platform: 'pc' },
  { api: 'pc.file.read', desc: '读取本地文件', platform: 'pc' },
  { api: 'pc.file.write', desc: '写入本地文件', platform: 'pc' },
  { api: 'pc.app.launch', desc: '启动应用程序', platform: 'pc' },
  // 移动端独有
  { api: 'mobile.battery', desc: '电池信息', platform: 'mobile' },
  { api: 'mobile.screen', desc: '屏幕信息', platform: 'mobile' },
  { api: 'mobile.vibrate', desc: '震动控制', platform: 'mobile' },
  { api: 'mobile.flashlight', desc: '手电筒', platform: 'mobile' },
  { api: 'mobile.gyroscope', desc: '陀螺仪', platform: 'mobile' },
  { api: 'mobile.accelerometer', desc: '加速度计', platform: 'mobile' },
  { api: 'mobile.gps', desc: 'GPS定位', platform: 'mobile' },
  { api: 'mobile.camera', desc: '摄像头', platform: 'mobile' },
  { api: 'mobile.sensors', desc: '传感器', platform: 'mobile' },
  // 双端通用
  { api: 'device.info', desc: '设备基本信息', platform: 'both' },
  { api: 'device.network', desc: '网络状态', platform: 'both' },
  { api: 'storage.get', desc: '读取数据', platform: 'both' },
  { api: 'storage.set', desc: '写入数据', platform: 'both' },
  { api: 'notification.show', desc: '系统通知', platform: 'both' },
  { api: 'http.get', desc: 'HTTP GET', platform: 'both' },
  { api: 'http.post', desc: 'HTTP POST', platform: 'both' },
  { api: 'websocket.connect', desc: 'WebSocket连接', platform: 'both' },
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
            <button @click="addFolder" class="text-xs" style="color: var(--color-primary);" title="新建文件夹">
              <Icon icon="solar:folder-with-files-bold" class="text-sm" />
            </button>
            <button @click="importFolder" class="text-xs" style="color: var(--color-primary);" title="导入文件夹">
              <Icon icon="solar:import-bold" class="text-sm" />
            </button>
            <button @click="addFile" class="text-xs" style="color: var(--color-primary);" title="新建文件">
              <Icon icon="solar:add-circle-bold" class="text-sm" />
            </button>
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
        <button @click="save" class="flex-1 btn-primary">
          <Icon icon="solar:diskette-bold" class="text-base" />
          保存
        </button>
        <button @click="exportPlugin" class="btn-secondary">
          <Icon icon="solar:export-bold" class="text-base" />
          导出
        </button>
      </div>
      <button @click="openHelp" class="w-full px-3 py-2 rounded-lg text-sm transition-colors border flex items-center justify-center gap-1" style="border-color: var(--color-border); color: var(--color-primary);">
        <Icon icon="solar:question-circle-bold" class="text-base" />
        开发帮助
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
        <CodeEditor
          v-if="activeFile"
          v-model="activeFile.content"
          :language="activeFile.language === 'vue' ? 'vue' : activeFile.language === 'json' ? 'json' : activeFile.language === 'css' ? 'css' : activeFile.language === 'ts' ? 'typescript' : 'javascript'"
        />
        <div v-else class="flex items-center justify-center h-full rounded-xl" style="background-color: var(--color-bg-card); border: 1px solid var(--color-border); color: var(--color-text-dim);">
          <p class="text-sm">请选择或创建文件</p>
        </div>
      </div>
    </div>

    <!-- 右侧区域已移除，帮助改为独立窗口 -->
  </div>

  <div v-else class="text-center py-20" style="color: var(--color-text-dim);">
    <p>插件未找到</p>
    <button class="mt-4 px-4 py-2 rounded-lg text-sm" style="background-color: var(--color-bg-surface);" @click="goBack">返回</button>
  </div>
</template>
