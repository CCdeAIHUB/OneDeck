<script setup lang="ts">
import { Icon } from '@iconify/vue'
import PageHeader from '@/components/PageHeader.vue'
import { useDesignStore, type PageCell } from '@/stores/design'
import { useRoute, useRouter } from 'vue-router'
import { computed, ref, watch } from 'vue'

const route = useRoute()
const router = useRouter()
const designStore = useDesignStore()

const pageId = computed(() => route.params.id as string)
const page = computed(() => designStore.pages.find((p) => p.id === pageId.value))

// 选中的格子
const selectedCellId = ref<string | null>(null)
const selectedCell = computed(() =>
  page.value?.cells.find((c) => c.id === selectedCellId.value)
)

// 编辑面板
const showCellEditor = ref(false)

// 页面属性
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

// 监听页面数据变化，同步到编辑器
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
    background: {
      type: bgType.value,
      color: bgColor.value,
      imageUrl: bgImageUrl.value,
      videoUrl: bgVideoUrl.value,
    },
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

function goBack() {
  router.push('/pages')
}

// 可用组件列表
const availableComponents = computed(() => designStore.components)
</script>

<template>
  <div v-if="page" class="flex gap-6 h-[calc(100vh-120px)]">
    <!-- 左侧：属性面板 -->
    <div class="w-72 shrink-0 overflow-y-auto space-y-4">
      <div class="flex items-center gap-2 mb-2">
        <button class="p-1.5 hover:bg-gray-800 rounded-lg transition-colors" @click="goBack">
          <Icon icon="solar:alt-arrow-left-bold" class="text-lg" />
        </button>
        <h2 class="text-lg font-bold truncate">{{ page.name }}</h2>
      </div>

      <!-- 基本属性 -->
      <div class="bg-gray-900 border border-gray-800 rounded-xl p-4 space-y-3">
        <h3 class="text-sm font-semibold text-gray-300">基本属性</h3>
        <div>
          <label class="text-xs text-gray-500">页面名称</label>
          <input v-model="pageName" @change="savePage" class="w-full mt-1 px-3 py-1.5 bg-gray-800 border border-gray-700 rounded-lg text-sm focus:outline-none focus:border-indigo-500" />
        </div>
        <div>
          <label class="text-xs text-gray-500">方向</label>
          <div class="flex gap-2 mt-1">
            <button
              class="flex-1 px-3 py-1.5 text-sm rounded-lg transition-colors"
              :class="pageOrientation === 'vertical' ? 'bg-indigo-600' : 'bg-gray-800 hover:bg-gray-700'"
              @click="pageOrientation = 'vertical'; savePage()"
            >竖屏</button>
            <button
              class="flex-1 px-3 py-1.5 text-sm rounded-lg transition-colors"
              :class="pageOrientation === 'horizontal' ? 'bg-indigo-600' : 'bg-gray-800 hover:bg-gray-700'"
              @click="pageOrientation = 'horizontal'; savePage()"
            >横屏</button>
          </div>
        </div>
        <div class="grid grid-cols-2 gap-2">
          <div>
            <label class="text-xs text-gray-500">行数</label>
            <input v-model.number="pageRows" type="number" min="1" max="10" @change="applyGridChange" class="w-full mt-1 px-3 py-1.5 bg-gray-800 border border-gray-700 rounded-lg text-sm focus:outline-none focus:border-indigo-500" />
          </div>
          <div>
            <label class="text-xs text-gray-500">列数</label>
            <input v-model.number="pageColumns" type="number" min="1" max="10" @change="applyGridChange" class="w-full mt-1 px-3 py-1.5 bg-gray-800 border border-gray-700 rounded-lg text-sm focus:outline-none focus:border-indigo-500" />
          </div>
        </div>
      </div>

      <!-- 背景设置 -->
      <div class="bg-gray-900 border border-gray-800 rounded-xl p-4 space-y-3">
        <h3 class="text-sm font-semibold text-gray-300">背景</h3>
        <div class="flex gap-2">
          <button v-for="t in (['color', 'image', 'video'] as const)" :key="t"
            class="flex-1 px-2 py-1 text-xs rounded-lg transition-colors"
            :class="bgType === t ? 'bg-indigo-600' : 'bg-gray-800 hover:bg-gray-700'"
            @click="bgType = t; savePage()"
          >{{ t === 'color' ? '颜色' : t === 'image' ? '图片' : '视频' }}</button>
        </div>
        <div v-if="bgType === 'color'">
          <input v-model="bgColor" type="color" @change="savePage" class="w-full h-8 rounded cursor-pointer" />
        </div>
        <div v-else-if="bgType === 'image'">
          <input v-model="bgImageUrl" @change="savePage" placeholder="图片URL" class="w-full px-3 py-1.5 bg-gray-800 border border-gray-700 rounded-lg text-sm focus:outline-none focus:border-indigo-500" />
        </div>
        <div v-else>
          <input v-model="bgVideoUrl" @change="savePage" placeholder="视频URL" class="w-full px-3 py-1.5 bg-gray-800 border border-gray-700 rounded-lg text-sm focus:outline-none focus:border-indigo-500" />
        </div>
      </div>

      <!-- 页边距 -->
      <div class="bg-gray-900 border border-gray-800 rounded-xl p-4 space-y-3">
        <div class="flex items-center justify-between">
          <h3 class="text-sm font-semibold text-gray-300">页边距</h3>
          <button
            class="relative w-9 h-5 rounded-full shrink-0 transition-colors"
            :class="pageCustomPadding ? 'bg-indigo-600' : 'bg-gray-700'"
            @click="pageCustomPadding = !pageCustomPadding; savePage()"
          >
            <span class="absolute top-0.5 left-0.5 w-4 h-4 bg-white rounded-full transition-transform"
              :class="pageCustomPadding ? 'translate-x-4' : ''" />
          </button>
        </div>
        <p class="text-xs text-gray-500">{{ pageCustomPadding ? '自定义' : '自动居中' }}</p>
        <div v-if="pageCustomPadding" class="grid grid-cols-2 gap-2">
          <div><label class="text-xs text-gray-500">上</label><input v-model.number="paddingTop" @change="savePage" type="number" class="w-full mt-0.5 px-2 py-1 bg-gray-800 border border-gray-700 rounded text-xs text-right" /></div>
          <div><label class="text-xs text-gray-500">右</label><input v-model.number="paddingRight" @change="savePage" type="number" class="w-full mt-0.5 px-2 py-1 bg-gray-800 border border-gray-700 rounded text-xs text-right" /></div>
          <div><label class="text-xs text-gray-500">下</label><input v-model.number="paddingBottom" @change="savePage" type="number" class="w-full mt-0.5 px-2 py-1 bg-gray-800 border border-gray-700 rounded text-xs text-right" /></div>
          <div><label class="text-xs text-gray-500">左</label><input v-model.number="paddingLeft" @change="savePage" type="number" class="w-full mt-0.5 px-2 py-1 bg-gray-800 border border-gray-700 rounded text-xs text-right" /></div>
        </div>
      </div>

      <!-- 选中格子编辑 -->
      <div v-if="selectedCell && showCellEditor" class="bg-gray-900 border border-indigo-500/50 rounded-xl p-4 space-y-3">
        <div class="flex items-center justify-between">
          <h3 class="text-sm font-semibold text-indigo-300">格子属性</h3>
          <button @click="showCellEditor = false; selectedCellId = null" class="text-gray-500 hover:text-white">
            <Icon icon="solar:close-circle-bold" class="text-sm" />
          </button>
        </div>
        <div class="text-xs text-gray-500">位置：第 {{ selectedCell.row + 1 }} 行 第 {{ selectedCell.column + 1 }} 列</div>
        <div class="grid grid-cols-2 gap-2">
          <div>
            <label class="text-xs text-gray-500">跨行数</label>
            <input :value="selectedCell.rowSpan" type="number" min="1" :max="page.rows - selectedCell.row"
              @input="updateCellProp('rowSpan', +($event.target as HTMLInputElement).value)"
              class="w-full mt-0.5 px-2 py-1 bg-gray-800 border border-gray-700 rounded text-xs text-right" />
          </div>
          <div>
            <label class="text-xs text-gray-500">跨列数</label>
            <input :value="selectedCell.columnSpan" type="number" min="1" :max="page.columns - selectedCell.column"
              @input="updateCellProp('columnSpan', +($event.target as HTMLInputElement).value)"
              class="w-full mt-0.5 px-2 py-1 bg-gray-800 border border-gray-700 rounded text-xs text-right" />
          </div>
        </div>
        <div>
          <label class="text-xs text-gray-500">关联组件</label>
          <select
            :value="selectedCell.componentId ?? ''"
            @change="updateCellProp('componentId', ($event.target as HTMLSelectElement).value || null)"
            class="w-full mt-0.5 px-2 py-1.5 bg-gray-800 border border-gray-700 rounded text-xs focus:outline-none focus:border-indigo-500"
          >
            <option value="">无</option>
            <option v-for="comp in availableComponents" :key="comp.id" :value="comp.id">{{ comp.name }}</option>
          </select>
        </div>
      </div>
    </div>

    <!-- 中间：画布预览 -->
    <div class="flex-1 flex items-center justify-center bg-gray-950 rounded-xl border border-gray-800 overflow-hidden">
      <div
        class="relative shadow-2xl"
        :style="{
          width: pageOrientation === 'vertical' ? '360px' : '640px',
          height: pageOrientation === 'vertical' ? '640px' : '360px',
          backgroundColor: bgType === 'color' ? bgColor : '#1f2937'
        }"
      >
        <!-- 背景图片/视频 -->
        <img v-if="bgType === 'image' && bgImageUrl" :src="bgImageUrl" class="absolute inset-0 w-full h-full object-cover" />
        <video v-if="bgType === 'video' && bgVideoUrl" :src="bgVideoUrl" autoplay loop muted class="absolute inset-0 w-full h-full object-cover" />

        <!-- 格子网格 -->
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
            :class="cell.id === selectedCellId ? 'border-indigo-500 bg-indigo-500/20' : cell.componentId ? 'border-gray-600 bg-gray-800/60 hover:border-indigo-400/50' : 'border-gray-700/50 bg-gray-800/30 hover:border-gray-600'"
            :style="{
              gridColumn: `${cell.column + 1} / span ${cell.columnSpan}`,
              gridRow: `${cell.row + 1} / span ${cell.rowSpan}`,
            }"
            @click="selectCell(cell)"
          >
            <div v-if="cell.componentId" class="text-center">
              <Icon icon="solar:widget-2-bold" class="text-indigo-400 text-lg" />
              <p class="text-[10px] text-indigo-300 mt-0.5">
                {{ availableComponents.find(c => c.id === cell.componentId)?.name ?? '组件' }}
              </p>
            </div>
            <Icon v-else icon="solar:add-circle-linear" class="text-gray-600 text-lg" />
          </div>
        </div>
      </div>
    </div>
  </div>

  <div v-else class="text-center py-20 text-gray-500">
    <p>页面未找到</p>
    <button class="mt-4 px-4 py-2 bg-gray-700 hover:bg-gray-600 rounded-lg text-sm" @click="goBack">返回</button>
  </div>
</template>
