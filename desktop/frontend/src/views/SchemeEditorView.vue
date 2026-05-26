<script setup lang="ts">
import { Icon } from '@iconify/vue'
import PageHeader from '@/components/PageHeader.vue'
import { useDesignStore, GESTURE_LABELS, type GestureType, type SchemeGestureConfig } from '@/stores/design'
import { useRoute, useRouter } from 'vue-router'
import { computed, ref } from 'vue'

const route = useRoute()
const router = useRouter()
const designStore = useDesignStore()

const schemeId = computed(() => route.params.id as string)

// 方案数据
const schemeName = ref('新方案')
const schemePages = ref<Array<{ pageId: string; order: number }>>([])
const gestures = ref<SchemeGestureConfig[]>([])
const currentPageIndex = ref(0)
const editorMode = ref<'visual' | 'code'>('visual')
const showGestureDialog = ref(false)

// 新增手势
const newGestureType = ref<GestureType>('swipe-up')
const newGestureDirection = ref<'next' | 'prev' | 'specific'>('next')
const newGestureTargetPageId = ref<string | null>(null)

// 可用页面列表
const availablePages = computed(() => designStore.pages)

// 可用手势类型
const gestureTypes = Object.entries(GESTURE_LABELS).map(([type, label]) => ({
  type: type as GestureType,
  label,
}))

function addPageToScheme() {
  // 弹出选择页面对话框
  const pageId = prompt('输入页面ID（从页面列表中复制）')
  if (!pageId) return
  schemePages.value.push({
    pageId,
    order: schemePages.value.length,
  })
}

function removePageFromScheme(index: number) {
  schemePages.value.splice(index, 1)
  // 重新排序
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
  // TODO: 保存方案到后端
}

function pushToDevice() {
  // TODO: 推送方案到设备
}

function goBack() {
  router.push('/schemes')
}
</script>

<template>
  <div class="flex gap-6 h-[calc(100vh-120px)]">
    <!-- 左侧：页面列表 & 手势配置 -->
    <div class="w-72 shrink-0 overflow-y-auto space-y-4">
      <div class="flex items-center gap-2 mb-2">
        <button class="p-1.5 hover:bg-gray-800 rounded-lg transition-colors" @click="goBack">
          <Icon icon="solar:alt-arrow-left-bold" class="text-lg" />
        </button>
        <input v-model="schemeName" class="text-lg font-bold bg-transparent border-b border-gray-700 focus:border-indigo-500 focus:outline-none" />
      </div>

      <!-- 方案页面列表 -->
      <div class="bg-gray-900 border border-gray-800 rounded-xl p-4 space-y-3">
        <div class="flex items-center justify-between">
          <h3 class="text-sm font-semibold text-gray-300">页面列表</h3>
          <button @click="addPageToScheme" class="text-indigo-400 hover:text-indigo-300 text-xs">+ 添加页面</button>
        </div>

        <div v-if="schemePages.length === 0" class="text-xs text-gray-600 text-center py-4">
          暂无页面，请添加
        </div>

        <div v-for="(sp, idx) in schemePages" :key="idx"
          class="flex items-center gap-2 p-2 rounded-lg cursor-pointer transition-colors"
          :class="idx === currentPageIndex ? 'bg-indigo-600/30 border border-indigo-500/50' : 'bg-gray-800/50 hover:bg-gray-800'"
          @click="currentPageIndex = idx"
        >
          <span class="text-xs text-gray-500 w-5 text-center">{{ idx + 1 }}</span>
          <span class="text-sm flex-1 truncate">{{ availablePages.find(p => p.id === sp.pageId)?.name ?? sp.pageId }}</span>
          <div class="flex items-center gap-0.5">
            <button @click.stop="movePageUp(idx)" class="p-0.5 text-gray-500 hover:text-white"><Icon icon="solar:alt-arrow-up-bold" class="text-xs" /></button>
            <button @click.stop="movePageDown(idx)" class="p-0.5 text-gray-500 hover:text-white"><Icon icon="solar:alt-arrow-down-bold" class="text-xs" /></button>
            <button @click.stop="removePageFromScheme(idx)" class="p-0.5 text-gray-500 hover:text-red-400"><Icon icon="solar:close-circle-bold" class="text-xs" /></button>
          </div>
        </div>
      </div>

      <!-- 手势配置 -->
      <div class="bg-gray-900 border border-gray-800 rounded-xl p-4 space-y-3">
        <div class="flex items-center justify-between">
          <h3 class="text-sm font-semibold text-gray-300">页面切换手势</h3>
          <button @click="showGestureDialog = true" class="text-indigo-400 hover:text-indigo-300 text-xs">+ 添加手势</button>
        </div>

        <div v-if="gestures.length === 0" class="text-xs text-gray-600 text-center py-4">
          暂无手势配置
        </div>

        <div v-for="(g, idx) in gestures" :key="idx"
          class="flex items-center gap-2 p-2 bg-gray-800/50 rounded-lg"
        >
          <Icon icon="solar:hand-shake-bold" class="text-indigo-400 text-sm" />
          <div class="flex-1 min-w-0">
            <p class="text-xs">{{ GESTURE_LABELS[g.gestureType] }}</p>
            <p class="text-[10px] text-gray-500">
              {{ g.direction === 'next' ? '下一页' : g.direction === 'prev' ? '上一页' : `跳转至 ${g.targetPageId ?? ''}` }}
            </p>
          </div>
          <button @click="removeGesture(idx)" class="text-gray-500 hover:text-red-400">
            <Icon icon="solar:close-circle-bold" class="text-xs" />
          </button>
        </div>

        <!-- 添加手势对话框 -->
        <div v-if="showGestureDialog" class="bg-gray-800 rounded-lg p-3 space-y-2 mt-2">
          <h4 class="text-xs font-semibold text-gray-300">新增手势</h4>
          <select v-model="newGestureType" class="w-full px-2 py-1 bg-gray-700 rounded text-xs">
            <option v-for="g in gestureTypes" :key="g.type" :value="g.type">{{ g.label }}</option>
          </select>
          <select v-model="newGestureDirection" class="w-full px-2 py-1 bg-gray-700 rounded text-xs">
            <option value="next">下一页</option>
            <option value="prev">上一页</option>
            <option value="specific">指定页面</option>
          </select>
          <select v-if="newGestureDirection === 'specific'" v-model="newGestureTargetPageId" class="w-full px-2 py-1 bg-gray-700 rounded text-xs">
            <option v-for="p in availablePages" :key="p.id" :value="p.id">{{ p.name }}</option>
          </select>
          <div class="flex gap-2">
            <button @click="addGesture" class="flex-1 px-2 py-1 bg-indigo-600 hover:bg-indigo-500 rounded text-xs">确定</button>
            <button @click="showGestureDialog = false" class="flex-1 px-2 py-1 bg-gray-700 hover:bg-gray-600 rounded text-xs">取消</button>
          </div>
        </div>
      </div>

      <!-- 操作按钮 -->
      <div class="flex gap-2">
        <button @click="saveScheme" class="flex-1 px-3 py-2 bg-indigo-600 hover:bg-indigo-500 rounded-lg text-sm transition-colors">
          <Icon icon="solar:diskette-bold" class="inline mr-1" />保存
        </button>
        <button @click="pushToDevice" class="flex-1 px-3 py-2 bg-emerald-600 hover:bg-emerald-500 rounded-lg text-sm transition-colors">
          <Icon icon="solar:upload-bold" class="inline mr-1" />推送
        </button>
      </div>
    </div>

    <!-- 中间：方案预览 -->
    <div class="flex-1 flex items-center justify-center bg-gray-950 rounded-xl border border-gray-800 overflow-hidden">
      <div v-if="schemePages.length > 0" class="relative" style="width:360px; height:640px;">
        <div class="absolute inset-0 bg-gray-900 rounded-2xl border border-gray-700 overflow-hidden flex items-center justify-center">
          <div class="text-center text-gray-500">
            <Icon icon="solar:smartphone-bold" class="text-4xl mb-2 mx-auto block" />
            <p class="text-sm">页面预览</p>
            <p class="text-xs text-gray-600 mt-1">{{ availablePages.find(p => p.id === schemePages[currentPageIndex]?.pageId)?.name ?? '空页面' }}</p>
            <p class="text-xs text-gray-700 mt-1">{{ currentPageIndex + 1 }} / {{ schemePages.length }}</p>
          </div>
        </div>

        <!-- 页面切换指示器 -->
        <div v-if="schemePages.length > 1" class="absolute bottom-4 left-0 right-0 flex justify-center gap-1.5">
          <div
            v-for="(_, idx) in schemePages"
            :key="idx"
            class="w-2 h-2 rounded-full cursor-pointer transition-colors"
            :class="idx === currentPageIndex ? 'bg-indigo-400' : 'bg-gray-600'"
            @click="currentPageIndex = idx"
          />
        </div>
      </div>

      <div v-else class="text-center text-gray-600">
        <Icon icon="solar:widget-bold" class="text-5xl mb-3 mx-auto block" />
        <p>请添加页面到方案</p>
      </div>
    </div>
  </div>
</template>
