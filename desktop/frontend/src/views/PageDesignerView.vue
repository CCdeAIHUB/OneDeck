<script setup lang="ts">
import { Icon } from '@iconify/vue'
import { useDesignStore, type PageCell } from '@/stores/design'
import { useDeviceStore } from '@/stores/devices'
import { useRoute, useRouter } from 'vue-router'
import { computed, ref, watch } from 'vue'

const route = useRoute()
const router = useRouter()
const designStore = useDesignStore()
const deviceStore = useDeviceStore()

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

function applyGridChange() {
  if (!page.value) return
  designStore.regenerateCells(pageId.value, pageRows.value, pageColumns.value)
}

function savePage() {
  if (!page.value) return
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

function goBack() { router.push('/pages') }

const availableComponents = computed(() => designStore.components)

// 设备比例预览
const devicePreviewStyle = computed(() => {
  const device = deviceStore.selectedDevice
  if (device && device.screenWidth && device.screenHeight) {
    const ratio = device.screenWidth / device.screenHeight
    const maxW = 480
    const maxH = 700
    let w, h
    if (ratio > 1) {
      w = maxW
      h = maxW / ratio
    } else {
      h = maxH
      w = maxH * ratio
    }
    return { width: `${Math.round(w)}px`, height: `${Math.round(h)}px` }
  }
  // 默认根据方向
  if (pageOrientation.value === 'vertical') {
    return { width: '360px', height: '640px' }
  }
  return { width: '640px', height: '360px' }
})

const deviceLabel = computed(() => {
  const device = deviceStore.selectedDevice
  if (device) return `${device.deviceName} (${device.screenWidth}x${device.screenHeight})`
  return pageOrientation.value === 'vertical' ? '默认竖屏 9:16' : '默认横屏 16:9'
})
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
            <button
              class="flex-1 px-3 py-1.5 text-sm rounded-lg transition-colors"
              :style="pageOrientation === 'vertical' ? 'background-color: var(--color-primary); color: white;' : 'background-color: var(--color-bg-surface); color: var(--color-text-muted);'"
              @click="pageOrientation = 'vertical'; savePage()"
            >竖屏</button>
            <button
              class="flex-1 px-3 py-1.5 text-sm rounded-lg transition-colors"
              :style="pageOrientation === 'horizontal' ? 'background-color: var(--color-primary); color: white;' : 'background-color: var(--color-bg-surface); color: var(--color-text-muted);'"
              @click="pageOrientation = 'horizontal'; savePage()"
            >横屏</button>
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
          <button v-for="t in (['color', 'image', 'video'] as const)" :key="t"
            class="flex-1 px-2 py-1 text-xs rounded-lg transition-colors"
            :style="bgType === t ? 'background-color: var(--color-primary); color: white;' : 'background-color: var(--color-bg-surface); color: var(--color-text-muted);'"
            @click="bgType = t; savePage()"
          >{{ t === 'color' ? '颜色' : t === 'image' ? '图片' : '视频' }}</button>
        </div>
        <div v-if="bgType === 'color'">
          <input v-model="bgColor" type="color" @change="savePage" class="w-full h-8 rounded cursor-pointer" />
        </div>
        <div v-else-if="bgType === 'image'">
          <input v-model="bgImageUrl" @change="savePage" placeholder="图片URL" class="w-full px-3 py-1.5 rounded-lg text-sm focus:outline-none" style="background-color: var(--color-bg-surface); border: 1px solid var(--color-border); color: var(--color-text);" />
        </div>
        <div v-else>
          <input v-model="bgVideoUrl" @change="savePage" placeholder="视频URL" class="w-full px-3 py-1.5 rounded-lg text-sm focus:outline-none" style="background-color: var(--color-bg-surface); border: 1px solid var(--color-border); color: var(--color-text);" />
        </div>
      </div>

      <!-- 页边距 -->
      <div class="rounded-xl p-4 space-y-3 border" style="background-color: var(--color-bg-card); border-color: var(--color-border);">
        <div class="flex items-center justify-between">
          <h3 class="text-sm font-semibold" style="color: var(--color-text-muted);">页边距</h3>
          <button
            class="relative w-9 h-5 rounded-full shrink-0 transition-colors"
            :style="pageCustomPadding ? 'background-color: var(--color-primary);' : 'background-color: var(--color-bg-hover);'"
            @click="pageCustomPadding = !pageCustomPadding; savePage()"
          >
            <span class="absolute top-0.5 left-0.5 w-4 h-4 bg-white rounded-full transition-transform"
              :class="pageCustomPadding ? 'translate-x-4' : ''" />
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
          <button @click="showCellEditor = false; selectedCellId = null" style="color: var(--color-text-dim);">
            <Icon icon="solar:close-circle-bold" class="text-sm" />
          </button>
        </div>
        <div class="text-xs" style="color: var(--color-text-dim);">位置：第 {{ selectedCell.row + 1 }} 行 第 {{ selectedCell.column + 1 }} 列</div>
        <div class="grid grid-cols-2 gap-2">
          <div>
            <label class="text-xs" style="color: var(--color-text-dim);">跨行数</label>
            <input :value="selectedCell.rowSpan" type="number" min="1" :max="page.rows - selectedCell.row"
              @input="updateCellProp('rowSpan', +($event.target as HTMLInputElement).value)"
              class="w-full mt-0.5 px-2 py-1 rounded text-xs text-right" style="background-color: var(--color-bg-surface); border: 1px solid var(--color-border); color: var(--color-text);" />
          </div>
          <div>
            <label class="text-xs" style="color: var(--color-text-dim);">跨列数</label>
            <input :value="selectedCell.columnSpan" type="number" min="1" :max="page.columns - selectedCell.column"
              @input="updateCellProp('columnSpan', +($event.target as HTMLInputElement).value)"
              class="w-full mt-0.5 px-2 py-1 rounded text-xs text-right" style="background-color: var(--color-bg-surface); border: 1px solid var(--color-border); color: var(--color-text);" />
          </div>
        </div>
        <div>
          <label class="text-xs" style="color: var(--color-text-dim);">关联组件</label>
          <select
            :value="selectedCell.componentId ?? ''"
            @change="updateCellProp('componentId', ($event.target as HTMLSelectElement).value || null)"
            class="w-full mt-0.5 px-2 py-1.5 rounded text-xs focus:outline-none"
            style="background-color: var(--color-bg-surface); border: 1px solid var(--color-border); color: var(--color-text);"
          >
            <option value="">无</option>
            <option v-for="comp in availableComponents" :key="comp.id" :value="comp.id">{{ comp.name }}</option>
          </select>
        </div>
      </div>
    </div>

    <!-- 中间：画布预览 -->
    <div class="flex-1 flex flex-col items-center justify-center rounded-xl border overflow-hidden" style="background-color: var(--color-bg); border-color: var(--color-border);">
      <div
        class="relative shadow-2xl"
        :style="{
          ...devicePreviewStyle,
          backgroundColor: bgType === 'color' ? bgColor : 'var(--color-bg-surface)'
        }"
      >
        <img v-if="bgType === 'image' && bgImageUrl" :src="bgImageUrl" class="absolute inset-0 w-full h-full object-cover" />
        <video v-if="bgType === 'video' && bgVideoUrl" :src="bgVideoUrl" autoplay loop muted class="absolute inset-0 w-full h-full object-cover" />

        <div
          class="absolute inset-0 grid gap-1"
          :style="{
            gridTemplateColumns: `repeat(${page.columns}, 1fr)`,
            gridTemplateRows: `repeat(${page.rows}, 1fr)`,
            padding: pageCustomPadding
              ? `${paddingTop}px ${paddingRight}px ${paddingBottom}px ${paddingLeft}px`
              : '16px',
          }"
        >
          <div
            v-for="cell in page.cells"
            :key="cell.id"
            class="rounded-md border-2 flex items-center justify-center cursor-pointer transition-colors"
            :style="{
              gridColumn: `${cell.column + 1} / span ${cell.columnSpan}`,
              gridRow: `${cell.row + 1} / span ${cell.rowSpan}`,
              borderColor: cell.id === selectedCellId ? 'var(--color-primary)' : cell.componentId ? 'var(--color-border)' : 'var(--color-border-subtle)',
              backgroundColor: cell.id === selectedCellId ? 'rgba(59,130,246,0.15)' : cell.componentId ? 'var(--color-bg-surface)' : 'transparent',
            }"
            @click="selectCell(cell)"
          >
            <div v-if="cell.componentId" class="text-center">
              <Icon icon="solar:widget-2-bold" class="text-lg" style="color: var(--color-primary);" />
              <p class="text-[10px] mt-0.5" style="color: var(--color-primary-light);">
                {{ availableComponents.find(c => c.id === cell.componentId)?.name ?? '组件' }}
              </p>
            </div>
            <Icon v-else icon="solar:add-circle-linear" class="text-lg" style="color: var(--color-text-dim);" />
          </div>
        </div>
      </div>
      <!-- 设备比例标签 -->
      <div class="mt-3 text-xs" style="color: var(--color-text-dim);">{{ deviceLabel }}</div>
    </div>
  </div>

  <div v-else class="text-center py-20" style="color: var(--color-text-dim);">
    <p>页面未找到</p>
    <button class="mt-4 px-4 py-2 rounded-lg text-sm" style="background-color: var(--color-bg-surface);" @click="goBack">返回</button>
  </div>
</template>
