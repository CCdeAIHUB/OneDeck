<script setup lang="ts">
import { Icon } from '@iconify/vue'
import { useDesignStore, GESTURE_LABELS, type GestureType, type SchemeGestureConfig } from '@/stores/design'
import { useSchemeStore } from '@/stores/schemes'
import { useRoute, useRouter } from 'vue-router'
import { computed, ref } from 'vue'

const route = useRoute()
const router = useRouter()
const designStore = useDesignStore()
const schemeStore = useSchemeStore()

const schemeId = computed(() => route.params.id as string)
const scheme = computed(() => schemeStore.schemes.find(s => s.id === schemeId.value))

// 方案数据
const schemeName = ref('新方案')
const schemePages = ref<Array<{ pageId: string; order: number }>>([])
const gestures = ref<SchemeGestureConfig[]>([])
const currentPageIndex = ref(0)
const showGestureDialog = ref(false)
const showPageSelectDialog = ref(false)

// 新增手势
const newGestureType = ref<GestureType>('swipe-up')
const newGestureDirection = ref<'next' | 'prev' | 'specific'>('next')
const newGestureTargetPageId = ref<string | null>(null)

// 可用页面列表
const availablePages = computed(() => designStore.pages)

// 已添加的页面ID集合，用于筛选可选页面
const addedPageIds = computed(() => new Set(schemePages.value.map(sp => sp.pageId)))

// 未添加的页面
const availablePagesToAdd = computed(() => designStore.pages.filter(p => !addedPageIds.value.has(p.id)))

// 可用手势类型
const gestureTypes = Object.entries(GESTURE_LABELS).map(([type, label]) => ({
  type: type as GestureType,
  label,
}))

// 从 scheme store 初始化数据
if (scheme.value) {
  schemeName.value = scheme.value.name
  schemePages.value = [...scheme.value.layout.pages]
}

function addPageToScheme(pageId: string) {
  schemePages.value.push({
    pageId,
    order: schemePages.value.length,
  })
  showPageSelectDialog.value = false
}

function removePageFromScheme(index: number) {
  schemePages.value.splice(index, 1)
  schemePages.value.forEach((p, i) => (p.order = i))
}

function movePageUp(index: number) {
  if (index <= 0) return
  const temp = schemePages.value[index]
  schemePages.value[index] = schemePages.value[index - 1]
  schemePages.value[index - 1] = temp
  schemePages.value.forEach((p, i) => (p.order = i))
}

function movePageDown(index: number) {
  if (index >= schemePages.value.length - 1) return
  const temp = schemePages.value[index]
  schemePages.value[index] = schemePages.value[index + 1]
  schemePages.value[index + 1] = temp
  schemePages.value.forEach((p, i) => (p.order = i))
}

function addGesture() {
  const gesture: SchemeGestureConfig = {
    pageId: schemePages.value[currentPageIndex.value]?.pageId ?? '',
    gestureType: newGestureType.value,
    targetPageId: newGestureDirection.value === 'specific' ? newGestureTargetPageId.value : null,
    direction: newGestureDirection.value,
  }
  gestures.value.push(gesture)
  showGestureDialog.value = false
}

function removeGesture(index: number) {
  gestures.value.splice(index, 1)
}

function saveScheme() {
  if (!scheme.value) return
  schemeStore.updateScheme({
    ...scheme.value,
    name: schemeName.value,
    layout: {
      ...scheme.value.layout,
      pages: schemePages.value,
    },
    updatedAt: new Date().toISOString(),
  })
}

function pushToDevice() {
  // TODO: 推送方案到设备
}

function goBack() {
  router.push('/schemes')
}

function exportScheme() {
  if (!scheme.value) return
  const data = {
    ...scheme.value,
    name: schemeName.value,
    layout: { ...scheme.value.layout, pages: schemePages.value },
    gestures: gestures.value,
  }
  const blob = new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = `scheme-${schemeName.value}-${Date.now()}.json`
  a.click()
  URL.revokeObjectURL(url)
}
</script>

<template>
  <div class="flex gap-6 h-[calc(100vh-120px)]">
    <!-- 左侧：页面列表 & 手势配置 -->
    <div class="w-72 shrink-0 overflow-y-auto space-y-4">
      <div class="flex items-center gap-2 mb-2">
        <button class="p-1.5 rounded-lg transition-colors" style="color: var(--color-text-muted);" @click="goBack">
          <Icon icon="solar:alt-arrow-left-bold" class="text-lg" />
        </button>
        <input v-model="schemeName" class="text-lg font-bold bg-transparent border-b focus:outline-none" style="border-color: var(--color-border); color: var(--color-text);" />
      </div>

      <!-- 方案页面列表 -->
      <div class="rounded-xl p-4 space-y-3 border" style="background-color: var(--color-bg-card); border-color: var(--color-border);">
        <div class="flex items-center justify-between">
          <h3 class="text-sm font-semibold" style="color: var(--color-text-muted);">页面列表</h3>
          <button @click="showPageSelectDialog = true" class="text-xs" style="color: var(--color-primary);">+ 添加页面</button>
        </div>

        <div v-if="schemePages.length === 0" class="text-xs text-center py-4" style="color: var(--color-text-dim);">
          暂无页面，请添加
        </div>

        <div v-for="(sp, idx) in schemePages" :key="idx"
          class="flex items-center gap-2 p-2 rounded-lg cursor-pointer transition-colors"
          :style="idx === currentPageIndex ? 'background-color: var(--color-primary); opacity: 0.2;' : ''"
          @click="currentPageIndex = idx"
        >
          <span class="text-xs w-5 text-center" style="color: var(--color-text-dim);">{{ idx + 1 }}</span>
          <span class="text-sm flex-1 truncate">{{ availablePages.find(p => p.id === sp.pageId)?.name ?? sp.pageId }}</span>
          <div class="flex items-center gap-0.5">
            <button @click.stop="movePageUp(idx)" class="p-0.5" style="color: var(--color-text-dim);"><Icon icon="solar:alt-arrow-up-bold" class="text-xs" /></button>
            <button @click.stop="movePageDown(idx)" class="p-0.5" style="color: var(--color-text-dim);"><Icon icon="solar:alt-arrow-down-bold" class="text-xs" /></button>
            <button @click.stop="removePageFromScheme(idx)" class="p-0.5 hover:text-red-400" style="color: var(--color-text-dim);"><Icon icon="solar:close-circle-bold" class="text-xs" /></button>
          </div>
        </div>
      </div>

      <!-- 手势配置 -->
      <div class="rounded-xl p-4 space-y-3 border" style="background-color: var(--color-bg-card); border-color: var(--color-border);">
        <div class="flex items-center justify-between">
          <h3 class="text-sm font-semibold" style="color: var(--color-text-muted);">页面切换手势</h3>
          <button @click="showGestureDialog = true" class="text-xs" style="color: var(--color-primary);">+ 添加手势</button>
        </div>

        <div v-if="gestures.length === 0" class="text-xs text-center py-4" style="color: var(--color-text-dim);">
          暂无手势配置
        </div>

        <div v-for="(g, idx) in gestures" :key="idx" class="flex items-center gap-2 p-2 rounded-lg" style="background-color: var(--color-bg-surface);">
          <Icon icon="solar:hand-shake-bold" class="text-sm" style="color: var(--color-primary);" />
          <div class="flex-1 min-w-0">
            <p class="text-xs">{{ GESTURE_LABELS[g.gestureType] }}</p>
            <p class="text-[10px]" style="color: var(--color-text-dim);">
              {{ g.direction === 'next' ? '下一页' : g.direction === 'prev' ? '上一页' : `跳转至 ${g.targetPageId ?? ''}` }}
            </p>
          </div>
          <button @click="removeGesture(idx)" class="hover:text-red-400" style="color: var(--color-text-dim);">
            <Icon icon="solar:close-circle-bold" class="text-xs" />
          </button>
        </div>

        <!-- 添加手势对话框 -->
        <div v-if="showGestureDialog" class="rounded-lg p-3 space-y-2 mt-2" style="background-color: var(--color-bg-surface);">
          <h4 class="text-xs font-semibold" style="color: var(--color-text-muted);">新增手势</h4>
          <select v-model="newGestureType" class="w-full px-2 py-1 rounded text-xs" style="background-color: var(--color-bg); border: 1px solid var(--color-border); color: var(--color-text);">
            <option v-for="g in gestureTypes" :key="g.type" :value="g.type">{{ g.label }}</option>
          </select>
          <select v-model="newGestureDirection" class="w-full px-2 py-1 rounded text-xs" style="background-color: var(--color-bg); border: 1px solid var(--color-border); color: var(--color-text);">
            <option value="next">下一页</option>
            <option value="prev">上一页</option>
            <option value="specific">指定页面</option>
          </select>
          <select v-if="newGestureDirection === 'specific'" v-model="newGestureTargetPageId" class="w-full px-2 py-1 rounded text-xs" style="background-color: var(--color-bg); border: 1px solid var(--color-border); color: var(--color-text);">
            <option v-for="p in availablePages" :key="p.id" :value="p.id">{{ p.name }}</option>
          </select>
          <div class="flex gap-2">
            <button @click="addGesture" class="flex-1 px-2 py-1 rounded text-xs text-white" style="background-color: var(--color-primary);">确定</button>
            <button @click="showGestureDialog = false" class="flex-1 px-2 py-1 rounded text-xs" style="background-color: var(--color-bg-hover); color: var(--color-text-muted);">取消</button>
          </div>
        </div>
      </div>

      <!-- 操作按钮 -->
      <div class="flex gap-2">
        <button @click="saveScheme" class="flex-1 px-3 py-2 rounded-lg text-sm text-white transition-colors" style="background-color: var(--color-primary);">
          <Icon icon="solar:diskette-bold" class="inline mr-1" />保存
        </button>
        <button @click="pushToDevice" class="flex-1 px-3 py-2 rounded-lg text-sm text-white transition-colors bg-emerald-600 hover:bg-emerald-500">
          <Icon icon="solar:upload-bold" class="inline mr-1" />推送
        </button>
      </div>
      <button @click="exportScheme" class="w-full px-3 py-2 rounded-lg text-sm transition-colors border" style="border-color: var(--color-border); color: var(--color-text-muted);">
        <Icon icon="solar:download-bold" class="inline mr-1" />导出方案
      </button>
    </div>

    <!-- 中间：方案预览 -->
    <div class="flex-1 flex items-center justify-center rounded-xl border overflow-hidden" style="background-color: var(--color-bg); border-color: var(--color-border);">
      <div v-if="schemePages.length > 0" class="relative" style="width:360px; height:640px;">
        <div class="absolute inset-0 rounded-2xl border overflow-hidden flex items-center justify-center" style="background-color: var(--color-bg-card); border-color: var(--color-border);">
          <div class="text-center" style="color: var(--color-text-muted);">
            <Icon icon="solar:smartphone-bold" class="text-4xl mb-2 mx-auto block" />
            <p class="text-sm">页面预览</p>
            <p class="text-xs mt-1" style="color: var(--color-text-dim);">{{ availablePages.find(p => p.id === schemePages[currentPageIndex]?.pageId)?.name ?? '空页面' }}</p>
            <p class="text-xs mt-1" style="color: var(--color-text-dim);">{{ currentPageIndex + 1 }} / {{ schemePages.length }}</p>
          </div>
        </div>

        <!-- 页面切换指示器 -->
        <div v-if="schemePages.length > 1" class="absolute bottom-4 left-0 right-0 flex justify-center gap-1.5">
          <div
            v-for="(_, idx) in schemePages"
            :key="idx"
            class="w-2 h-2 rounded-full cursor-pointer transition-colors"
            :style="idx === currentPageIndex ? 'background-color: var(--color-primary);' : 'background-color: var(--color-text-dim);'"
            @click="currentPageIndex = idx"
          />
        </div>
      </div>

      <div v-else class="text-center" style="color: var(--color-text-dim);">
        <Icon icon="solar:layers-bold" class="text-5xl mb-3 mx-auto block" />
        <p>请添加页面到方案</p>
      </div>
    </div>

    <!-- 添加页面对话框 -->
    <div v-if="showPageSelectDialog" class="fixed inset-0 bg-black/50 flex items-center justify-center z-50" @click.self="showPageSelectDialog = false">
      <div class="rounded-xl p-6 w-96 space-y-4" style="background-color: var(--color-bg-card);">
        <h3 class="text-lg font-bold">添加页面</h3>
        <div v-if="availablePagesToAdd.length === 0" class="text-center py-8" style="color: var(--color-text-dim);">
          <p class="text-sm">暂无可用页面</p>
          <p class="text-xs mt-1">请先在"页面"管理中创建页面</p>
        </div>
        <div v-else class="space-y-2 max-h-64 overflow-y-auto">
          <div
            v-for="page in availablePagesToAdd"
            :key="page.id"
            class="flex items-center justify-between p-3 rounded-lg cursor-pointer transition-colors border"
            style="border-color: var(--color-border-subtle);"
            @click="addPageToScheme(page.id)"
          >
            <div>
              <p class="text-sm font-medium">{{ page.name }}</p>
              <p class="text-xs" style="color: var(--color-text-dim);">{{ page.rows }}×{{ page.columns }} · {{ page.orientation === 'vertical' ? '竖屏' : '横屏' }}</p>
            </div>
            <Icon icon="solar:add-circle-bold" style="color: var(--color-primary);" />
          </div>
        </div>
        <div class="flex justify-end">
          <button
            class="px-4 py-2 rounded-lg text-sm transition-colors"
            style="background-color: var(--color-bg-surface); color: var(--color-text-muted);"
            @click="showPageSelectDialog = false"
          >
            关闭
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
