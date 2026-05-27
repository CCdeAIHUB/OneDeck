<script setup lang="ts">
import { Icon } from '@iconify/vue'
import { useDesignStore, type PageCell, type CellTrigger, type CellStyle, defaultCellTrigger, defaultCellStyle, resolveParamRefs, hasParamRefs } from '@/stores/design'
import { useSharedParamsStore } from '@/stores/sharedParams'
import { useDeviceStore } from '@/stores/devices'
import { useRoute, useRouter } from 'vue-router'
import { computed, ref, watch, onMounted } from 'vue'
import ParamInput from '@/components/ParamInput.vue'

const route = useRoute()
const router = useRouter()
const designStore = useDesignStore()
const deviceStore = useDeviceStore()
const sharedParamsStore = useSharedParamsStore()

const pageId = computed(() => route.params.id as string)
const page = computed(() => designStore.pages.find((p) => p.id === pageId.value))

const selectedCellId = ref<string | null>(null)
const selectedCell = computed(() => page.value?.cells.find((c) => c.id === selectedCellId.value))
const showCellEditor = ref(false)

const pageName = ref('')
const pageOrientation = ref<'vertical' | 'horizontal'>('vertical')
const pageRows = ref(4)
const pageColumns = ref(3)
const pageCustomPadding = ref(false)
const paddingTop = ref(16)
const paddingRight = ref(16)
const paddingBottom = ref(16)
const paddingLeft = ref(16)
const bgType = ref<'color' | 'image' | 'video'>('color')
const bgColor = ref('#111827')
const bgImageUrl = ref('')
const bgVideoUrl = ref('')

// 格子样式编辑中的本地状态
const cellBgColor = ref('transparent')
const cellText = ref('')
const cellTextColor = ref('#ffffff')
const cellTextPosition = ref<'center' | 'top' | 'bottom'>('center')
const cellFontFamily = ref('system-ui')
const cellFontSize = ref(14)
const cellBold = ref(false)
const cellItalic = ref(false)
const cellUnderline = ref(false)
const cellStrikethrough = ref(false)
const cellBgImage = ref('')
const cellBgVideo = ref('')

// 触发器编辑中的本地状态
const triggerType = ref<CellTrigger['type']>('none')
const triggerTarget = ref('')
const triggerValue = ref('')

// 系统字体列表
const systemFonts = ref<string[]>(['system-ui', 'Inter', 'monospace', 'serif', 'cursive', 'sans-serif'])

// 从后端获取系统字体
onMounted(async () => {
  try {
    const res = await fetch('https://onedesk.local/api/jsapi', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ api: 'pc.fontList', args: {} }),
    })
    const data = await res.json()
    if (data?.success && Array.isArray(data.data?.fonts)) {
      systemFonts.value = ['system-ui', ...data.data.fonts]
    }
  } catch {
    // 使用默认字体列表
  }
})

// 解析公共参数
function getParam(key: string): string | undefined {
  return String(sharedParamsStore.get(key) ?? '')
}

// 解析格子显示文字（替换 &param 为实际值）
function resolveCellText(text: string): string {
  return resolveParamRefs(text, getParam)
}

// 解析格子背景 URL
function resolveCellUrl(url: string): string {
  return resolveParamRefs(url, getParam)
}

// 本地文件选择 - 格子背景图片
function pickCellBgImage() {
  const input = document.createElement('input')
  input.type = 'file'
  input.accept = 'image/*'
  input.onchange = () => {
    const file = input.files?.[0]
    if (!file) return
    const reader = new FileReader()
    reader.onload = (e) => {
      cellBgImage.value = e.target?.result as string
      applyCellStyle()
    }
    reader.readAsDataURL(file)
  }
  input.click()
}

function pickCellBgVideo() {
  const input = document.createElement('input')
  input.type = 'file'
  input.accept = 'video/*'
  input.onchange = () => {
    const file = input.files?.[0]
    if (!file) return
    cellBgVideo.value = URL.createObjectURL(file)
    applyCellStyle()
  }
  input.click()
}

function pickLocalImage() {
  const input = document.createElement('input')
  input.type = 'file'
  input.accept = 'image/*'
  input.onchange = () => {
    const file = input.files?.[0]
    if (!file) return
    const reader = new FileReader()
    reader.onload = (e) => {
      bgImageUrl.value = e.target?.result as string
      savePage()
    }
    reader.readAsDataURL(file)
  }
  input.click()
}

function pickLocalVideo() {
  const input = document.createElement('input')
  input.type = 'file'
  input.accept = 'video/*'
  input.onchange = () => {
    const file = input.files?.[0]
    if (!file) return
    bgVideoUrl.value = URL.createObjectURL(file)
    savePage()
  }
  input.click()
}

watch(page, (p) => {
  if (!p) return
  pageName.value = p.name
  pageOrientation.value = p.orientation
  pageRows.value = p.rows
  pageColumns.value = p.columns
  pageCustomPadding.value = p.customPadding
  paddingTop.value = p.padding.top
  paddingRight.value = p.padding.right
  paddingBottom.value = p.padding.bottom
  paddingLeft.value = p.padding.left
  bgType.value = p.background.type
  bgColor.value = p.background.color
  bgImageUrl.value = p.background.imageUrl
  bgVideoUrl.value = p.background.videoUrl
}, { immediate: true })

watch(selectedCell, (cell) => {
  if (!cell) return
  const s = cell.style ?? defaultCellStyle()
  cellBgColor.value = s.backgroundColor
  cellText.value = s.text
  cellTextColor.value = s.textColor
  cellTextPosition.value = s.textPosition
  cellFontFamily.value = s.fontFamily
  cellFontSize.value = s.fontSize
  cellBold.value = s.bold
  cellItalic.value = s.italic
  cellUnderline.value = s.underline
  cellStrikethrough.value = s.strikethrough
  cellBgImage.value = s.backgroundImage
  cellBgVideo.value = s.backgroundVideo
  const t = cell.trigger ?? defaultCellTrigger()
  triggerType.value = t.type
  triggerTarget.value = t.target
  triggerValue.value = t.value
}, { immediate: true })

function applyGridChange() {
  if (!page.value) return
  designStore.regenerateCells(pageId.value, pageRows.value, pageColumns.value)
}

function savePage() {
  if (!page.value) return
  if (designStore.checkDuplicateName(pageName.value, 'page', pageId.value)) {
    const existingNames = designStore.pages.map(p => p.name)
    pageName.value = designStore.generateUniqueName(pageName.value, existingNames)
  }
  designStore.updatePage({
    ...page.value,
    name: pageName.value,
    orientation: pageOrientation.value,
    rows: pageRows.value,
    columns: pageColumns.value,
    customPadding: pageCustomPadding.value,
    padding: { top: paddingTop.value, right: paddingRight.value, bottom: paddingBottom.value, left: paddingLeft.value },
    background: { type: bgType.value, color: bgColor.value, imageUrl: bgImageUrl.value, videoUrl: bgVideoUrl.value },
  })
}

function selectCell(cell: PageCell) {
  selectedCellId.value = cell.id
  showCellEditor.value = true
}

function updateCellProp(key: keyof PageCell, value: unknown) {
  if (!selectedCell.value || !page.value) return
  designStore.updateCell(pageId.value, selectedCell.value.id, { [key]: value })
}

function applyCellStyle() {
  if (!selectedCell.value || !page.value) return
  const style: CellStyle = {
    backgroundColor: cellBgColor.value,
    text: cellText.value,
    textColor: cellTextColor.value,
    textPosition: cellTextPosition.value,
    fontFamily: cellFontFamily.value,
    fontSize: cellFontSize.value,
    bold: cellBold.value,
    italic: cellItalic.value,
    underline: cellUnderline.value,
    strikethrough: cellStrikethrough.value,
    backgroundImage: cellBgImage.value,
    backgroundVideo: cellBgVideo.value,
  }
  designStore.updateCell(pageId.value, selectedCell.value.id, { style })
}

function applyTrigger() {
  if (!selectedCell.value || !page.value) return
  const trigger: CellTrigger = {
    type: triggerType.value,
    target: triggerTarget.value,
    value: triggerValue.value,
  }
  designStore.updateCell(pageId.value, selectedCell.value.id, { trigger })
}

function goBack() { router.push('/pages') }

const availableComponents = computed(() => designStore.components)
const availablePages = computed(() => designStore.pages.filter(p => p.id !== pageId.value))
const sharedParamKeys = computed(() => sharedParamsStore.keys())

const enablePreview = ref(false)

const devicePreviewStyle = computed(() => {
  const device = deviceStore.selectedDevice
  if (device && device.screenWidth && device.screenHeight) {
    const ratio = device.screenWidth / device.screenHeight
    const maxW = 480, maxH = 700
    let w, h
    if (ratio > 1) { w = maxW; h = maxW / ratio }
    else { h = maxH; w = maxH * ratio }
    return { width: `${Math.round(w)}px`, height: `${Math.round(h)}px` }
  }
  if (pageOrientation.value === 'vertical') return { width: '360px', height: '640px' }
  return { width: '640px', height: '360px' }
})

const gridSizeStyle = computed(() => {
  const device = deviceStore.selectedDevice
  let containerW: number, containerH: number
  if (device && device.screenWidth && device.screenHeight) {
    const ratio = device.screenWidth / device.screenHeight
    const maxW = 480, maxH = 700
    if (ratio > 1) { containerW = maxW; containerH = maxW / ratio }
    else { containerH = maxH; containerW = maxH * ratio }
  } else {
    containerW = pageOrientation.value === 'vertical' ? 360 : 640
    containerH = pageOrientation.value === 'vertical' ? 640 : 360
  }
  const pad = pageCustomPadding.value ? paddingTop.value + paddingBottom.value : 32
  const padH = pageCustomPadding.value ? paddingLeft.value + paddingRight.value : 32
  const availW = containerW - padH
  const availH = containerH - pad
  return Math.floor(Math.min(availW / pageColumns.value, availH / pageRows.value))
})

const deviceLabel = computed(() => {
  const device = deviceStore.selectedDevice
  if (device) return `${device.deviceName} (${device.screenWidth}x${device.screenHeight})`
  return pageOrientation.value === 'vertical' ? '默认竖屏 9:16' : '默认横屏 16:9'
})

function textPositionStyle(pos: string) {
  switch (pos) {
    case 'top': return { alignItems: 'flex-start' as const, paddingTop: '4px' }
    case 'bottom': return { alignItems: 'flex-end' as const, paddingBottom: '4px' }
    default: return { alignItems: 'center' as const }
  }
}

function textDecorationStyle(style: CellStyle): string {
  const deco: string[] = []
  if (style.underline) deco.push('underline')
  if (style.strikethrough) deco.push('line-through')
  return deco.length > 0 ? deco.join(' ') : 'none'
}

// 触发器类型标签
const triggerTypeLabels: Record<CellTrigger['type'], string> = {
  none: '无',
  setParam: '修改公共参数',
  navigatePage: '跳转页面',
  callApi: '调用 API',
  openFile: '打开文件',
  openUrl: '打开网址',
  closeProcess: '关闭进程',
  systemCommand: '系统命令',
}
</script>

<template>
  <div v-if="page" class="flex gap-6 h-[calc(100vh-120px)]">
    <!-- 左侧：属性面板 -->
    <div class="w-72 shrink-0 overflow-y-auto space-y-4">
      <div class="flex items-center gap-2 mb-2">
        <button class="p-1.5 rounded-lg transition-colors" style="color: var(--color-text-muted);" @click="goBack">
          <Icon icon="solar:alt-arrow-left-bold" class="text-lg" />
        </button>
        <h2 class="text-lg font-bold truncate">{{ page.name }}</h2>
      </div>

      <!-- 基本属性 -->
      <div class="rounded-xl p-4 space-y-3 border" style="background-color: var(--color-bg-card); border-color: var(--color-border);">
        <h3 class="text-sm font-semibold" style="color: var(--color-text-muted);">基本属性</h3>
        <div>
          <label class="text-xs" style="color: var(--color-text-dim);">页面名称</label>
          <input v-model="pageName" @change="savePage" class="w-full mt-1 px-3 py-1.5 rounded-lg text-sm focus:outline-none" style="background-color: var(--color-bg-surface); border: 1px solid var(--color-border); color: var(--color-text);" />
        </div>
        <div>
          <label class="text-xs" style="color: var(--color-text-dim);">方向</label>
          <div class="flex gap-2 mt-1">
            <button class="flex-1 px-3 py-1.5 text-sm rounded-lg transition-colors" :style="pageOrientation === 'vertical' ? 'background-color: var(--color-primary); color: white;' : 'background-color: var(--color-bg-surface); color: var(--color-text-muted);'" @click="pageOrientation = 'vertical'; savePage()">竖屏</button>
            <button class="flex-1 px-3 py-1.5 text-sm rounded-lg transition-colors" :style="pageOrientation === 'horizontal' ? 'background-color: var(--color-primary); color: white;' : 'background-color: var(--color-bg-surface); color: var(--color-text-muted);'" @click="pageOrientation = 'horizontal'; savePage()">横屏</button>
          </div>
        </div>
        <div class="grid grid-cols-2 gap-2">
          <div>
            <label class="text-xs" style="color: var(--color-text-dim);">行数</label>
            <input v-model.number="pageRows" type="number" min="1" max="10" @change="applyGridChange" class="w-full mt-1 px-3 py-1.5 rounded-lg text-sm focus:outline-none" style="background-color: var(--color-bg-surface); border: 1px solid var(--color-border); color: var(--color-text);" />
          </div>
          <div>
            <label class="text-xs" style="color: var(--color-text-dim);">列数</label>
            <input v-model.number="pageColumns" type="number" min="1" max="10" @change="applyGridChange" class="w-full mt-1 px-3 py-1.5 rounded-lg text-sm focus:outline-none" style="background-color: var(--color-bg-surface); border: 1px solid var(--color-border); color: var(--color-text);" />
          </div>
        </div>
      </div>

      <!-- 背景设置 -->
      <div class="rounded-xl p-4 space-y-3 border" style="background-color: var(--color-bg-card); border-color: var(--color-border);">
        <h3 class="text-sm font-semibold" style="color: var(--color-text-muted);">背景</h3>
        <div class="flex gap-2">
          <button v-for="t in (['color', 'image', 'video'] as const)" :key="t" class="flex-1 px-2 py-1 text-xs rounded-lg transition-colors" :style="bgType === t ? 'background-color: var(--color-primary); color: white;' : 'background-color: var(--color-bg-surface); color: var(--color-text-muted);'" @click="bgType = t; savePage()">{{ t === 'color' ? '颜色' : t === 'image' ? '图片' : '视频' }}</button>
        </div>
        <div v-if="bgType === 'color'">
          <input v-model="bgColor" type="color" @change="savePage" class="w-full h-8 rounded cursor-pointer" />
        </div>
        <div v-else-if="bgType === 'image'" class="space-y-2">
          <ParamInput v-model="bgImageUrl" placeholder="图片URL 或 &参数名" @update:modelValue="savePage" />
          <div class="flex gap-1">
            <button class="px-2 py-1 rounded text-xs" style="background-color: var(--color-bg-surface); border: 1px solid var(--color-border); color: var(--color-text-muted);" @click="pickLocalImage" title="选择本地图片"><Icon icon="solar:folder-open-bold" class="text-xs" /></button>
          </div>
          <div v-if="bgImageUrl" class="rounded-lg overflow-hidden" style="max-height: 80px;">
            <img :src="resolveCellUrl(bgImageUrl)" class="w-full h-full object-cover" />
          </div>
        </div>
        <div v-else class="space-y-2">
          <ParamInput v-model="bgVideoUrl" placeholder="视频URL 或 &参数名" @update:modelValue="savePage" />
          <div class="flex gap-1">
            <button class="px-2 py-1 rounded text-xs" style="background-color: var(--color-bg-surface); border: 1px solid var(--color-border); color: var(--color-text-muted);" @click="pickLocalVideo" title="选择本地视频"><Icon icon="solar:folder-open-bold" class="text-xs" /></button>
          </div>
        </div>
      </div>

      <!-- 页边距 -->
      <div class="rounded-xl p-4 space-y-3 border" style="background-color: var(--color-bg-card); border-color: var(--color-border);">
        <div class="flex items-center justify-between">
          <h3 class="text-sm font-semibold" style="color: var(--color-text-muted);">页边距</h3>
          <button class="relative w-9 h-5 rounded-full shrink-0 transition-colors" :style="pageCustomPadding ? 'background-color: var(--color-primary);' : 'background-color: var(--color-bg-hover);'" @click="pageCustomPadding = !pageCustomPadding; savePage()">
            <span class="absolute top-0.5 left-0.5 w-4 h-4 bg-white rounded-full transition-transform" :class="pageCustomPadding ? 'translate-x-4' : ''" />
          </button>
        </div>
        <p class="text-xs" style="color: var(--color-text-dim);">{{ pageCustomPadding ? '自定义' : '自动居中' }}</p>
        <div v-if="pageCustomPadding" class="grid grid-cols-2 gap-2">
          <div><label class="text-xs" style="color: var(--color-text-dim);">上</label><input v-model.number="paddingTop" @change="savePage" type="number" class="w-full mt-0.5 px-2 py-1 rounded text-xs text-right" style="background-color: var(--color-bg-surface); border: 1px solid var(--color-border); color: var(--color-text);" /></div>
          <div><label class="text-xs" style="color: var(--color-text-dim);">右</label><input v-model.number="paddingRight" @change="savePage" type="number" class="w-full mt-0.5 px-2 py-1 rounded text-xs text-right" style="background-color: var(--color-bg-surface); border: 1px solid var(--color-border); color: var(--color-text);" /></div>
          <div><label class="text-xs" style="color: var(--color-text-dim);">下</label><input v-model.number="paddingBottom" @change="savePage" type="number" class="w-full mt-0.5 px-2 py-1 rounded text-xs text-right" style="background-color: var(--color-bg-surface); border: 1px solid var(--color-border); color: var(--color-text);" /></div>
          <div><label class="text-xs" style="color: var(--color-text-dim);">左</label><input v-model.number="paddingLeft" @change="savePage" type="number" class="w-full mt-0.5 px-2 py-1 rounded text-xs text-right" style="background-color: var(--color-bg-surface); border: 1px solid var(--color-border); color: var(--color-text);" /></div>
        </div>
      </div>

      <!-- 选中格子编辑 -->
      <div v-if="selectedCell && showCellEditor" class="rounded-xl p-4 space-y-3 border-2" style="background-color: var(--color-bg-card); border-color: var(--color-primary);">
        <div class="flex items-center justify-between">
          <h3 class="text-sm font-semibold" style="color: var(--color-primary-light);">格子属性</h3>
          <button @click="showCellEditor = false; selectedCellId = null" style="color: var(--color-text-dim);"><Icon icon="solar:close-circle-bold" class="text-sm" /></button>
        </div>
        <div class="text-xs" style="color: var(--color-text-dim);">位置：第 {{ selectedCell.row + 1 }} 行 第 {{ selectedCell.column + 1 }} 列</div>
        <div class="grid grid-cols-2 gap-2">
          <div>
            <label class="text-xs" style="color: var(--color-text-dim);">跨行数</label>
            <input :value="selectedCell.rowSpan" type="number" min="1" :max="page.rows - selectedCell.row" @input="updateCellProp('rowSpan', +($event.target as HTMLInputElement).value)" class="w-full mt-0.5 px-2 py-1 rounded text-xs text-right" style="background-color: var(--color-bg-surface); border: 1px solid var(--color-border); color: var(--color-text);" />
          </div>
          <div>
            <label class="text-xs" style="color: var(--color-text-dim);">跨列数</label>
            <input :value="selectedCell.columnSpan" type="number" min="1" :max="page.columns - selectedCell.column" @input="updateCellProp('columnSpan', +($event.target as HTMLInputElement).value)" class="w-full mt-0.5 px-2 py-1 rounded text-xs text-right" style="background-color: var(--color-bg-surface); border: 1px solid var(--color-border); color: var(--color-text);" />
          </div>
        </div>
        <div>
          <label class="text-xs" style="color: var(--color-text-dim);">关联组件</label>
          <select :value="selectedCell.componentId ?? ''" @change="updateCellProp('componentId', ($event.target as HTMLSelectElement).value || null)" class="w-full mt-0.5 px-2 py-1.5 rounded text-xs focus:outline-none" style="background-color: var(--color-bg-surface); border: 1px solid var(--color-border); color: var(--color-text);">
            <option value="">无</option>
            <option v-for="comp in availableComponents" :key="comp.id" :value="comp.id">{{ comp.name }}</option>
          </select>
        </div>

        <!-- 触发器设置 -->
        <div class="pt-2 mt-2" style="border-top: 1px solid var(--color-border-subtle);">
          <h4 class="text-xs font-semibold mb-2 flex items-center gap-1" style="color: var(--color-text-muted);">
            <Icon icon="solar:bolt-bold" class="text-sm" style="color: var(--color-primary);" />
            触发动作
          </h4>
          <div class="space-y-2">
            <div>
              <label class="text-xs" style="color: var(--color-text-dim);">动作类型</label>
              <select v-model="triggerType" @change="applyTrigger" class="w-full mt-0.5 px-2 py-1.5 rounded text-xs focus:outline-none" style="background-color: var(--color-bg-surface); border: 1px solid var(--color-border); color: var(--color-text);">
                <option v-for="(label, key) in triggerTypeLabels" :key="key" :value="key">{{ label }}</option>
              </select>
            </div>
            <!-- 修改公共参数 -->
            <div v-if="triggerType === 'setParam'">
              <label class="text-xs" style="color: var(--color-text-dim);">参数键名</label>
              <select v-model="triggerTarget" @change="applyTrigger" class="w-full mt-0.5 px-2 py-1.5 rounded text-xs focus:outline-none" style="background-color: var(--color-bg-surface); border: 1px solid var(--color-border); color: var(--color-text);">
                <option value="">新建参数</option>
                <option v-for="key in sharedParamKeys" :key="key" :value="key">{{ key }}</option>
              </select>
              <label class="text-xs mt-1 block" style="color: var(--color-text-dim);">新值</label>
              <ParamInput v-model="triggerValue" placeholder="参数值" @update:modelValue="applyTrigger" />
            </div>
            <!-- 跳转页面 -->
            <div v-if="triggerType === 'navigatePage'">
              <label class="text-xs" style="color: var(--color-text-dim);">目标页面</label>
              <select v-model="triggerTarget" @change="applyTrigger" class="w-full mt-0.5 px-2 py-1.5 rounded text-xs focus:outline-none" style="background-color: var(--color-bg-surface); border: 1px solid var(--color-border); color: var(--color-text);">
                <option value="">请选择</option>
                <option v-for="p in availablePages" :key="p.id" :value="p.id">{{ p.name }}</option>
              </select>
            </div>
            <!-- 调用 API -->
            <div v-if="triggerType === 'callApi'">
              <label class="text-xs" style="color: var(--color-text-dim);">API 端点</label>
              <input v-model="triggerTarget" @change="applyTrigger" placeholder="如 pc.app.launch" class="w-full mt-0.5 px-2 py-1.5 rounded text-xs focus:outline-none" style="background-color: var(--color-bg-surface); border: 1px solid var(--color-border); color: var(--color-text);" />
              <label class="text-xs mt-1 block" style="color: var(--color-text-dim);">参数</label>
              <ParamInput v-model="triggerValue" placeholder="如 notepad.exe" @update:modelValue="applyTrigger" />
            </div>
            <!-- 打开文件 -->
            <div v-if="triggerType === 'openFile'">
              <label class="text-xs" style="color: var(--color-text-dim);">文件路径</label>
              <ParamInput v-model="triggerTarget" placeholder="如 C:\\Program Files\\app.exe" @update:modelValue="applyTrigger" />
              <p class="text-[10px] mt-1" style="color: var(--color-text-dim);">将调用 pc.app.launch 打开该文件</p>
            </div>
            <!-- 打开网址 -->
            <div v-if="triggerType === 'openUrl'">
              <label class="text-xs" style="color: var(--color-text-dim);">网址</label>
              <ParamInput v-model="triggerTarget" placeholder="如 https://example.com" @update:modelValue="applyTrigger" />
              <p class="text-[10px] mt-1" style="color: var(--color-text-dim);">将使用默认浏览器打开</p>
            </div>
            <!-- 关闭进程 -->
            <div v-if="triggerType === 'closeProcess'">
              <label class="text-xs" style="color: var(--color-text-dim);">进程名称</label>
              <ParamInput v-model="triggerTarget" placeholder="如 notepad" @update:modelValue="applyTrigger" />
              <p class="text-[10px] mt-1" style="color: var(--color-text-dim);">将调用 pc.processKill 关闭该进程</p>
            </div>
            <!-- 系统命令 -->
            <div v-if="triggerType === 'systemCommand'">
              <label class="text-xs" style="color: var(--color-text-dim);">命令</label>
              <ParamInput v-model="triggerTarget" placeholder="如 shutdown /s /t 0" @update:modelValue="applyTrigger" />
              <label class="text-xs mt-1 block" style="color: var(--color-text-dim);">参数 (可选)</label>
              <ParamInput v-model="triggerValue" placeholder="命令参数" @update:modelValue="applyTrigger" />
            </div>
          </div>
        </div>

        <!-- 格子样式设置 -->
        <div class="pt-2 mt-2" style="border-top: 1px solid var(--color-border-subtle);">
          <h4 class="text-xs font-semibold mb-2 flex items-center gap-1" style="color: var(--color-text-muted);">
            <Icon icon="solar:palette-bold" class="text-sm" style="color: var(--color-primary);" />
            格子样式
          </h4>
          <div class="space-y-2">
            <!-- 背景颜色 -->
            <div>
              <label class="text-xs" style="color: var(--color-text-dim);">背景颜色</label>
              <div class="flex gap-1 mt-0.5">
                <input v-model="cellBgColor" type="color" @change="applyCellStyle" class="w-8 h-7 rounded cursor-pointer shrink-0" />
                <input v-model="cellBgColor" @change="applyCellStyle" placeholder="transparent" class="flex-1 px-2 py-1 rounded text-xs focus:outline-none" style="background-color: var(--color-bg-surface); border: 1px solid var(--color-border); color: var(--color-text);" />
              </div>
            </div>
            <!-- 文字内容（支持 &param 绑定） -->
            <div>
              <label class="text-xs flex items-center gap-1" style="color: var(--color-text-dim);">
                文字内容
                <span class="text-[10px] px-1 rounded" style="background-color: rgba(59,130,246,0.15); color: var(--color-primary);">&name 可引用公共参数</span>
              </label>
              <div class="mt-0.5">
                <ParamInput v-model="cellText" placeholder="文字内容 或 &参数名" @update:modelValue="applyCellStyle" />
              </div>
            </div>
            <div v-if="cellText" class="space-y-2">
              <div class="flex gap-2">
                <div class="flex-1">
                  <label class="text-xs" style="color: var(--color-text-dim);">文字颜色</label>
                  <div class="flex gap-1 mt-0.5">
                    <input v-model="cellTextColor" type="color" @change="applyCellStyle" class="w-8 h-7 rounded cursor-pointer shrink-0" />
                    <input v-model="cellTextColor" @change="applyCellStyle" class="flex-1 px-2 py-1 rounded text-xs focus:outline-none" style="background-color: var(--color-bg-surface); border: 1px solid var(--color-border); color: var(--color-text);" />
                  </div>
                </div>
                <div class="flex-1">
                  <label class="text-xs" style="color: var(--color-text-dim);">字号</label>
                  <input v-model.number="cellFontSize" type="number" min="8" max="72" @change="applyCellStyle" class="w-full mt-0.5 px-2 py-1 rounded text-xs focus:outline-none" style="background-color: var(--color-bg-surface); border: 1px solid var(--color-border); color: var(--color-text);" />
                </div>
              </div>
              <div>
                <label class="text-xs" style="color: var(--color-text-dim);">文字位置</label>
                <div class="flex gap-1 mt-0.5">
                  <button v-for="pos in (['center', 'top', 'bottom'] as const)" :key="pos" class="flex-1 px-2 py-1 text-xs rounded-lg transition-colors" :style="cellTextPosition === pos ? 'background-color: var(--color-primary); color: white;' : 'background-color: var(--color-bg-surface); color: var(--color-text-muted);'" @click="cellTextPosition = pos; applyCellStyle()">{{ pos === 'center' ? '居中' : pos === 'top' ? '顶部' : '底部' }}</button>
                </div>
              </div>
              <div>
                <label class="text-xs" style="color: var(--color-text-dim);">字体</label>
                <select v-model="cellFontFamily" @change="applyCellStyle" class="w-full mt-0.5 px-2 py-1.5 rounded text-xs focus:outline-none" style="background-color: var(--color-bg-surface); border: 1px solid var(--color-border); color: var(--color-text);">
                  <option v-for="font in systemFonts" :key="font" :value="font">{{ font }}</option>
                </select>
              </div>
              <div>
                <label class="text-xs" style="color: var(--color-text-dim);">文字样式</label>
                <div class="flex gap-1 mt-0.5">
                  <button class="px-2.5 py-1 text-xs rounded-lg transition-colors font-bold" :style="cellBold ? 'background-color: var(--color-primary); color: white;' : 'background-color: var(--color-bg-surface); color: var(--color-text-muted);'" @click="cellBold = !cellBold; applyCellStyle()">B</button>
                  <button class="px-2.5 py-1 text-xs rounded-lg transition-colors italic" :style="cellItalic ? 'background-color: var(--color-primary); color: white;' : 'background-color: var(--color-bg-surface); color: var(--color-text-muted);'" @click="cellItalic = !cellItalic; applyCellStyle()">I</button>
                  <button class="px-2.5 py-1 text-xs rounded-lg transition-colors underline" :style="cellUnderline ? 'background-color: var(--color-primary); color: white;' : 'background-color: var(--color-bg-surface); color: var(--color-text-muted);'" @click="cellUnderline = !cellUnderline; applyCellStyle()">U</button>
                  <button class="px-2.5 py-1 text-xs rounded-lg transition-colors line-through" :style="cellStrikethrough ? 'background-color: var(--color-primary); color: white;' : 'background-color: var(--color-bg-surface); color: var(--color-text-muted);'" @click="cellStrikethrough = !cellStrikethrough; applyCellStyle()">S</button>
                </div>
              </div>
            </div>
            <!-- 背景图片（支持 &param） -->
            <div>
              <label class="text-xs flex items-center gap-1" style="color: var(--color-text-dim);">
                背景图片
                <span class="text-[10px] px-1 rounded" style="background-color: rgba(59,130,246,0.15); color: var(--color-primary);">支持 &参数</span>
              </label>
              <div class="flex gap-1 mt-0.5">
                <ParamInput v-model="cellBgImage" placeholder="URL / base64 / &参数名" @update:modelValue="applyCellStyle" />
                <button @click="pickCellBgImage" class="px-2 py-1 rounded text-xs shrink-0" style="background-color: var(--color-bg-surface); border: 1px solid var(--color-border); color: var(--color-text-muted);" title="选择本地图片"><Icon icon="solar:folder-open-bold" class="text-xs" /></button>
              </div>
            </div>
            <!-- 背景视频（支持 &param） -->
            <div>
              <label class="text-xs flex items-center gap-1" style="color: var(--color-text-dim);">
                背景视频
                <span class="text-[10px] px-1 rounded" style="background-color: rgba(59,130,246,0.15); color: var(--color-primary);">支持 &参数</span>
              </label>
              <div class="flex gap-1 mt-0.5">
                <ParamInput v-model="cellBgVideo" placeholder="URL / &参数名" @update:modelValue="applyCellStyle" />
                <button @click="pickCellBgVideo" class="px-2 py-1 rounded text-xs shrink-0" style="background-color: var(--color-bg-surface); border: 1px solid var(--color-border); color: var(--color-text-muted);" title="选择本地视频"><Icon icon="solar:folder-open-bold" class="text-xs" /></button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- 中间：画布预览 -->
    <div class="flex-1 flex flex-col items-center justify-center rounded-xl border overflow-hidden" style="background-color: var(--color-bg); border-color: var(--color-border);">
      <div class="flex items-center gap-2 py-2 px-4 w-full shrink-0" style="border-bottom: 1px solid var(--color-border-subtle);">
        <button class="px-3 py-1 text-xs rounded-lg transition-colors" :style="enablePreview ? 'background-color: var(--color-primary); color: white;' : 'background-color: var(--color-bg-surface); color: var(--color-text-muted);'" @click="enablePreview = !enablePreview">
          <Icon icon="solar:eye-bold" class="inline mr-1" />{{ enablePreview ? '预览中' : '预览' }}
        </button>
        <span class="text-xs" style="color: var(--color-text-dim);">开启后将用组件代码渲染页面</span>
      </div>
      <div class="relative shadow-2xl" :style="{ ...devicePreviewStyle, backgroundColor: bgType === 'color' ? bgColor : 'var(--color-bg-surface)' }">
        <img v-if="bgType === 'image' && bgImageUrl" :src="resolveCellUrl(bgImageUrl)" class="absolute inset-0 w-full h-full object-cover" />
        <video v-if="bgType === 'video' && bgVideoUrl" :src="resolveCellUrl(bgVideoUrl)" autoplay loop muted class="absolute inset-0 w-full h-full object-cover" />
        <div class="absolute inset-0 grid gap-1" :style="{ gridTemplateColumns: `repeat(${page.columns}, ${gridSizeStyle}px)`, gridTemplateRows: `repeat(${page.rows}, ${gridSizeStyle}px)`, placeContent: 'center', padding: pageCustomPadding ? `${paddingTop}px ${paddingRight}px ${paddingBottom}px ${paddingLeft}px` : '16px' }">
          <div v-for="cell in page.cells" :key="cell.id" class="rounded-md border-2 flex items-center justify-center cursor-pointer transition-colors overflow-hidden relative" :style="{ gridColumn: `${cell.column + 1} / span ${cell.columnSpan}`, gridRow: `${cell.row + 1} / span ${cell.rowSpan}`, borderColor: cell.id === selectedCellId ? 'var(--color-primary)' : cell.componentId || cell.style?.text || cell.style?.backgroundImage ? 'var(--color-border)' : 'var(--color-border-subtle)', backgroundColor: cell.style?.backgroundColor && cell.style.backgroundColor !== 'transparent' ? cell.style.backgroundColor : cell.id === selectedCellId ? 'rgba(59,130,246,0.15)' : cell.componentId ? 'var(--color-bg-surface)' : 'transparent' }" @click="selectCell(cell)">
            <!-- 格子背景图片（解析 &param） -->
            <img v-if="cell.style?.backgroundImage" :src="resolveCellUrl(cell.style.backgroundImage)" class="absolute inset-0 w-full h-full object-cover" />
            <!-- 格子背景视频（解析 &param） -->
            <video v-if="cell.style?.backgroundVideo" :src="resolveCellUrl(cell.style.backgroundVideo)" autoplay loop muted class="absolute inset-0 w-full h-full object-cover" />
            <!-- 触发器标识 -->
            <div v-if="cell.trigger && cell.trigger.type !== 'none'" class="absolute top-0.5 right-0.5" style="z-index: 3;"><Icon icon="solar:bolt-bold" class="text-xs" style="color: var(--color-primary);" /></div>
            <!-- &param 绑定标识 -->
            <div v-if="hasParamRefs(cell.style?.text ?? '') || hasParamRefs(cell.style?.backgroundImage ?? '') || hasParamRefs(cell.style?.backgroundVideo ?? '')" class="absolute top-0.5 left-0.5" style="z-index: 3;"><Icon icon="solar:link-bold" class="text-xs" style="color: #10b981;" /></div>
            <!-- 自定义文字（解析 &param） -->
            <span v-if="cell.style?.text" class="relative z-[1] text-center px-1 break-words" :style="{ color: cell.style.textColor || '#fff', fontFamily: cell.style.fontFamily || 'system-ui', fontSize: (cell.style.fontSize || 14) + 'px', fontWeight: cell.style.bold ? 'bold' : 'normal', fontStyle: cell.style.italic ? 'italic' : 'normal', textDecoration: textDecorationStyle(cell.style), ...textPositionStyle(cell.style.textPosition || 'center') }">{{ resolveCellText(cell.style.text) }}</span>
            <!-- 组件预览 -->
            <div v-else-if="cell.componentId" class="text-center w-full h-full overflow-hidden relative z-[1]">
              <img v-if="availableComponents.find(c => c.id === cell.componentId)?.previewImage" :src="availableComponents.find(c => c.id === cell.componentId)!.previewImage" class="w-full h-full object-cover rounded-sm" />
              <template v-else>
                <Icon icon="solar:widget-2-bold" class="text-lg" style="color: var(--color-primary);" />
                <p class="text-[10px] mt-0.5" style="color: var(--color-primary-light);">{{ availableComponents.find(c => c.id === cell.componentId)?.name ?? '组件' }}</p>
              </template>
            </div>
            <Icon v-if="!cell.componentId && !cell.style?.text && !cell.style?.backgroundImage && !cell.style?.backgroundVideo" icon="solar:add-circle-linear" class="text-lg" style="color: var(--color-text-dim);" />
          </div>
        </div>
      </div>
      <div class="mt-3 text-xs" style="color: var(--color-text-dim);">{{ deviceLabel }}</div>
    </div>
  </div>

  <div v-else class="text-center py-20" style="color: var(--color-text-dim);">
    <p>页面未找到</p>
    <button class="mt-4 px-4 py-2 rounded-lg text-sm" style="background-color: var(--color-bg-surface);" @click="goBack">返回</button>
  </div>
</template>
