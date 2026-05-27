<script setup lang="ts">
import { Icon } from '@iconify/vue'
import { useDesignStore, GESTURE_LABELS, type GestureType, type SchemeGestureConfig } from '@/stores/design'
import { useSchemeStore } from '@/stores/schemes'
import { useRoute, useRouter } from 'vue-router'
import { computed, ref, reactive } from 'vue'

const route = useRoute()
const router = useRouter()
const designStore = useDesignStore()
const schemeStore = useSchemeStore()

const schemeId = computed(() => route.params.id as string)
const scheme = computed(() => schemeStore.schemes.find(s => s.id === schemeId.value))

// 方案数据
const schemeName = ref('新方案')
const schemeHomePageId = ref('')
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

// ==================== 节点式工作流 ====================

interface FlowNode {
  id: string
  type: 'page' | 'condition'
  pageId?: string
  label: string
  x: number
  y: number
}

interface FlowEdge {
  id: string
  from: string
  to: string
  label: string
  gestureType?: GestureType
}

// 手势方向映射到空间位置
function getGestureDirection(gestureType: GestureType): 'top' | 'bottom' | 'left' | 'right' | 'other' {
  if (gestureType.includes('down')) return 'top'    // 下滑 → 上方页面
  if (gestureType.includes('up')) return 'bottom'   // 上滑 → 下方页面
  if (gestureType.includes('left')) return 'right'  // 左滑 → 右方页面
  if (gestureType.includes('right')) return 'left'  // 右滑 → 左方页面
  return 'other'
}

// 计算工作流节点和连线
const flowData = computed(() => {
  const nodes: FlowNode[] = []
  const edges: FlowEdge[] = []

  if (schemePages.value.length === 0) return { nodes, edges }

  // 主页面在中心
  const mainPageId = schemePages.value[0]?.pageId
  const mainPage = availablePages.value.find(p => p.id === mainPageId)
  if (!mainPage) return { nodes, edges }

  const mainNodeId = `node-${mainPageId}`
  nodes.push({
    id: mainNodeId,
    type: 'page',
    pageId: mainPageId,
    label: mainPage.name,
    x: 400,
    y: 300,
  })

  // 跟踪已放置的页面位置（避免重叠）
  const placedPositions = new Map<string, { x: number; y: number }>()
  placedPositions.set(mainPageId, { x: 400, y: 300 })

  // 方向偏移量
  const directionOffset: Record<string, { dx: number; dy: number }> = {
    top: { dx: 0, dy: -200 },
    bottom: { dx: 0, dy: 200 },
    left: { dx: -250, dy: 0 },
    right: { dx: 250, dy: 0 },
    other: { dx: 0, dy: 0 },
  }

  // 各方向已有页面数量（用于偏移避免重叠）
  const directionCounts: Record<string, number> = { top: 0, bottom: 0, left: 0, right: 0, other: 0 }

  // 处理手势连线
  for (const gesture of gestures.value) {
    const sourcePageId = gesture.pageId
    const targetPageId = gesture.targetPageId ?? (gesture.direction === 'next' ? schemePages.value.find((sp, idx) => schemePages.value[idx]?.pageId === sourcePageId && idx + 1 < schemePages.value.length)?.pageId : undefined)
    
    if (!targetPageId) continue

    const targetPage = availablePages.value.find(p => p.id === targetPageId)
    if (!targetPage) continue

    // 确定方向
    const direction = getGestureDirection(gesture.gestureType)

    // 确保源页面有节点
    let sourceNode = nodes.find(n => n.pageId === sourcePageId)
    if (!sourceNode) {
      // 源页面不在主页面，随机放置
      const offsetX = (Math.random() - 0.5) * 200
      const offsetY = (Math.random() - 0.5) * 200
      const sourcePageData = availablePages.value.find(p => p.id === sourcePageId)
      sourceNode = {
        id: `node-${sourcePageId}`,
        type: 'page',
        pageId: sourcePageId,
        label: sourcePageData?.name ?? sourcePageId,
        x: 400 + offsetX,
        y: 300 + offsetY,
      }
      nodes.push(sourceNode)
      placedPositions.set(sourcePageId, { x: sourceNode.x, y: sourceNode.y })
    }

    // 确定目标页面位置
    if (!placedPositions.has(targetPageId)) {
      const sourcePos = placedPositions.get(sourcePageId) ?? { x: 400, y: 300 }
      const offset = directionOffset[direction]
      const count = directionCounts[direction] ?? 0
      const countOffset = count * 60

      let targetX = sourcePos.x + offset.dx
      let targetY = sourcePos.y + offset.dy

      // 非4方向手势，随机偏移
      if (direction === 'other') {
        targetX = sourcePos.x + (Math.random() - 0.5) * 300
        targetY = sourcePos.y + (Math.random() - 0.5) * 300
      }

      // 添加条件节点
      const conditionNodeId = `condition-${gesture.gestureType}-${sourcePageId}-${targetPageId}`
      nodes.push({
        id: conditionNodeId,
        type: 'condition',
        label: GESTURE_LABELS[gesture.gestureType],
        x: (sourcePos.x + targetX) / 2 + (direction === 'other' ? (Math.random() - 0.5) * 40 : 0),
        y: (sourcePos.y + targetY) / 2 + (direction === 'other' ? (Math.random() - 0.5) * 40 : 0),
      })

      // 添加目标页面节点
      nodes.push({
        id: `node-${targetPageId}`,
        type: 'page',
        pageId: targetPageId,
        label: targetPage.name,
        x: targetX,
        y: targetY,
      })
      placedPositions.set(targetPageId, { x: targetX, y: targetY })
      directionCounts[direction] = count + 1

      // 源页面 → 条件节点
      edges.push({
        id: `edge-${sourceNode.id}-${conditionNodeId}`,
        from: sourceNode.id,
        to: conditionNodeId,
        label: '',
        gestureType: gesture.gestureType,
      })

      // 条件节点 → 目标页面
      edges.push({
        id: `edge-${conditionNodeId}-node-${targetPageId}`,
        from: conditionNodeId,
        to: `node-${targetPageId}`,
        label: GESTURE_LABELS[gesture.gestureType],
        gestureType: gesture.gestureType,
      })
    } else {
      // 目标已存在，只添加条件节点
      const targetPos = placedPositions.get(targetPageId)!
      const sourcePos = placedPositions.get(sourcePageId) ?? { x: 400, y: 300 }
      const conditionNodeId = `condition-${gesture.gestureType}-${sourcePageId}-${targetPageId}`

      // 避免重复条件节点
      if (!nodes.find(n => n.id === conditionNodeId)) {
        nodes.push({
          id: conditionNodeId,
          type: 'condition',
          label: GESTURE_LABELS[gesture.gestureType],
          x: (sourcePos.x + targetPos.x) / 2,
          y: (sourcePos.y + targetPos.y) / 2,
        })

        edges.push({
          id: `edge-${sourceNode.id}-${conditionNodeId}`,
          from: sourceNode.id,
          to: conditionNodeId,
          label: '',
          gestureType: gesture.gestureType,
        })

        edges.push({
          id: `edge-${conditionNodeId}-node-${targetPageId}`,
          from: conditionNodeId,
          to: `node-${targetPageId}`,
          label: GESTURE_LABELS[gesture.gestureType],
          gestureType: gesture.gestureType,
        })
      }
    }
  }

  // 如果只有页面没有手势，则排列所有页面
  if (gestures.value.length === 0 && schemePages.value.length > 1) {
    for (let i = 1; i < schemePages.value.length; i++) {
      const pageId = schemePages.value[i].pageId
      const page = availablePages.value.find(p => p.id === pageId)
      if (!page || placedPositions.has(pageId)) continue

      const row = Math.floor((i - 1) / 3)
      const col = (i - 1) % 3
      nodes.push({
        id: `node-${pageId}`,
        type: 'page',
        pageId,
        label: page.name,
        x: 150 + col * 250,
        y: 100 + row * 180,
      })
      placedPositions.set(pageId, { x: 150 + col * 250, y: 100 + row * 180 })
    }
  }

  return { nodes, edges }
})

// 拖拽节点 - 使用持久化位置覆盖
const nodeOverrides = reactive<Record<string, { x: number; y: number }>>({})

const dragState = reactive({
  dragging: false,
  nodeId: '',
  startX: 0,
  startY: 0,
  nodeStartX: 0,
  nodeStartY: 0,
})

function startDrag(nodeId: string, event: MouseEvent) {
  const pos = getNodePosition(nodeId)
  dragState.dragging = true
  dragState.nodeId = nodeId
  dragState.startX = event.clientX
  dragState.startY = event.clientY
  dragState.nodeStartX = pos.x
  dragState.nodeStartY = pos.y
}

function onDrag(event: MouseEvent) {
  if (!dragState.dragging) return
  const dx = event.clientX - dragState.startX
  const dy = event.clientY - dragState.startY
  nodeOverrides[dragState.nodeId] = {
    x: dragState.nodeStartX + dx,
    y: dragState.nodeStartY + dy,
  }
}

function endDrag() {
  dragState.dragging = false
}

function getNodePosition(nodeId: string) {
  // 优先使用拖拽覆盖位置
  if (nodeOverrides[nodeId]) return nodeOverrides[nodeId]
  const node = flowData.value.nodes.find(n => n.id === nodeId)
  return node ? { x: node.x, y: node.y } : { x: 0, y: 0 }
}

// 选中节点
const selectedNodeId = ref<string | null>(null)

function selectNode(nodeId: string) {
  selectedNodeId.value = selectedNodeId.value === nodeId ? null : nodeId
  const node = flowData.value.nodes.find(n => n.id === nodeId)
  if (node?.pageId) {
    const idx = schemePages.value.findIndex(sp => sp.pageId === node.pageId)
    if (idx >= 0) currentPageIndex.value = idx
  }
}

// 从 scheme store 初始化数据
if (scheme.value) {
  schemeName.value = scheme.value.name
  schemeHomePageId.value = scheme.value.homePageId ?? ''
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
  const removedId = schemePages.value[index]?.pageId
  schemePages.value.splice(index, 1)
  schemePages.value.forEach((p, i) => (p.order = i))
  // 如果移除的是主页面，重置
  if (removedId === schemeHomePageId.value) {
    schemeHomePageId.value = schemePages.value[0]?.pageId ?? ''
  }
}

function setHomePage(pageId: string) {
  schemeHomePageId.value = pageId
  saveScheme()
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
    homePageId: schemeHomePageId.value,
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
          :style="idx === currentPageIndex ? 'background-color: rgba(59,130,246,0.15);' : ''"
          @click="currentPageIndex = idx"
        >
          <span class="text-xs w-5 text-center" style="color: var(--color-text-dim);">{{ idx + 1 }}</span>
          <span class="text-sm flex-1 truncate">{{ availablePages.find(p => p.id === sp.pageId)?.name ?? sp.pageId }}</span>
          <!-- 主页面标记/设置按钮 -->
          <button v-if="sp.pageId === schemeHomePageId" @click.stop class="p-0.5" title="主页面（默认启动）" style="color: var(--color-primary);"><Icon icon="solar:home-2-bold" class="text-sm" /></button>
          <button v-else @click.stop="setHomePage(sp.pageId)" class="p-0.5" title="设为主页面" style="color: var(--color-text-dim);"><Icon icon="solar:home-2-linear" class="text-sm" /></button>
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
              {{ g.direction === 'next' ? '下一页' : g.direction === 'prev' ? '上一页' : `跳转至 ${availablePages.find(p => p.id === g.targetPageId)?.name ?? g.targetPageId ?? ''}` }}
            </p>
          </div>
          <button @click="removeGesture(idx)" class="hover:text-red-400" style="color: var(--color-text-dim);">
            <Icon icon="solar:close-circle-bold" class="text-xs" />
          </button>
        </div>

        <!-- 添加手势对话框 -->
        <div v-if="showGestureDialog" class="rounded-lg p-3 space-y-2 mt-2" style="background-color: var(--color-bg-surface);">
          <h4 class="text-xs font-semibold" style="color: var(--color-text-muted);">新增手势</h4>
          <div>
            <label class="text-xs" style="color: var(--color-text-dim);">源页面</label>
            <select disabled class="w-full px-2 py-1 rounded text-xs" style="background-color: var(--color-bg); border: 1px solid var(--color-border); color: var(--color-text);">
              <option>{{ availablePages.find(p => p.id === schemePages[currentPageIndex]?.pageId)?.name ?? '未选择' }}</option>
            </select>
          </div>
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

    <!-- 中间：节点式工作流预览 -->
    <div
      class="flex-1 relative rounded-xl border overflow-hidden"
      style="background-color: var(--color-bg); border-color: var(--color-border);"
      @mousemove="onDrag"
      @mouseup="endDrag"
      @mouseleave="endDrag"
    >
      <!-- 空状态 -->
      <div v-if="schemePages.length === 0" class="absolute inset-0 flex items-center justify-center">
        <div class="text-center" style="color: var(--color-text-dim);">
          <Icon icon="solar:layers-bold" class="text-5xl mb-3 mx-auto block" />
          <p>请添加页面到方案</p>
        </div>
      </div>

      <!-- SVG 连线层 -->
      <svg class="absolute inset-0 w-full h-full pointer-events-none" style="z-index: 1;">
        <defs>
          <marker id="arrowhead" markerWidth="8" markerHeight="6" refX="8" refY="3" orient="auto">
            <polygon points="0 0, 8 3, 0 6" fill="#3b82f6" />
          </marker>
        </defs>
        <line
          v-for="edge in flowData.edges"
          :key="edge.id"
          :x1="getNodePosition(edge.from).x"
          :y1="getNodePosition(edge.from).y"
          :x2="getNodePosition(edge.to).x"
          :y2="getNodePosition(edge.to).y"
          stroke="#3b82f6"
          stroke-width="2"
          opacity="0.7"
          marker-end="url(#arrowhead)"
        />
      </svg>

      <!-- 连线标签层 -->
      <div class="absolute inset-0 pointer-events-none" style="z-index: 2;">
        <div
          v-for="edge in flowData.edges.filter(e => e.label)"
          :key="`label-${edge.id}`"
          class="absolute text-[10px] px-1.5 py-0.5 rounded"
          :style="{
            left: `${(getNodePosition(edge.from).x + getNodePosition(edge.to).x) / 2 - 20}px`,
            top: `${(getNodePosition(edge.from).y + getNodePosition(edge.to).y) / 2 - 8}px`,
            backgroundColor: 'var(--color-bg-card)',
            color: 'var(--color-primary)',
            border: '1px solid var(--color-border-subtle)',
          }"
        >
          {{ edge.label }}
        </div>
      </div>

      <!-- 节点层 -->
      <div class="absolute inset-0" style="z-index: 3;">
        <div
          v-for="node in flowData.nodes"
          :key="node.id"
          class="absolute cursor-pointer select-none"
          :style="{
            left: `${node.x - (node.type === 'page' ? 60 : 40)}px`,
            top: `${node.y - (node.type === 'page' ? 35 : 16)}px`,
          }"
          @mousedown="startDrag(node.id, $event)"
          @click.stop="selectNode(node.id)"
        >
          <!-- 页面节点 -->
          <div
            v-if="node.type === 'page'"
            class="rounded-lg px-4 py-3 text-center transition-all border-2 min-w-[120px]"
            :style="{
              backgroundColor: selectedNodeId === node.id ? 'rgba(59,130,246,0.2)' : 'var(--color-bg-card)',
              borderColor: selectedNodeId === node.id ? 'var(--color-primary)' : 'var(--color-border)',
              boxShadow: selectedNodeId === node.id ? '0 0 12px rgba(59,130,246,0.3)' : '0 2px 8px rgba(0,0,0,0.3)',
            }"
          >
            <Icon icon="solar:clipboard-list-bold" class="text-lg mb-1" style="color: var(--color-primary);" />
            <p class="text-xs font-semibold truncate" style="max-width: 100px;">{{ node.label }}</p>
            <p v-if="node.pageId === schemePages[0]?.pageId" class="text-[10px] mt-1" style="color: var(--color-primary);">主页面</p>
          </div>

          <!-- 条件节点 -->
          <div
            v-else
            class="rounded-full px-3 py-1 text-center transition-all border"
            :style="{
              backgroundColor: selectedNodeId === node.id ? 'rgba(245,158,11,0.2)' : 'var(--color-bg-surface)',
              borderColor: selectedNodeId === node.id ? '#f59e0b' : 'var(--color-border-subtle)',
            }"
          >
            <p class="text-[10px] font-medium whitespace-nowrap" style="color: #f59e0b;">{{ node.label }}</p>
          </div>
        </div>
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
