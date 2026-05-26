import { defineStore } from 'pinia'
import { ref, computed } from 'vue'

// ==================== 页面设计模型 ====================

export interface PageDesign {
  id: string
  name: string
  /** 方向：horizontal(横屏) | vertical(竖屏) */
  orientation: 'horizontal' | 'vertical'
  /** 格子行数 */
  rows: number
  /** 格子列数 */
  columns: number
  /** 页边距 */
  padding: { top: number; right: number; bottom: number; left: number }
  /** 是否使用自定义边距，false 则居中 */
  customPadding: boolean
  /** 背景 */
  background: PageBackground
  /** 格子列表 */
  cells: PageCell[]
  createdAt: string
  updatedAt: string
}

export interface PageBackground {
  type: 'color' | 'image' | 'video'
  color: string
  imageUrl: string
  videoUrl: string
}

export interface PageCell {
  id: string
  row: number
  column: number
  rowSpan: number
  columnSpan: number
  /** 关联的组件ID */
  componentId: string | null
}

// ==================== 组件设计模型 ====================

export interface ComponentDesign {
  id: string
  name: string
  description: string
  /** Vue3 模板代码 */
  templateCode: string
  /** JS/TS 逻辑代码 */
  scriptCode: string
  /** CSS 样式代码 */
  styleCode: string
  /** 静态资源列表 */
  assets: ComponentAsset[]
  createdAt: string
  updatedAt: string
}

export interface ComponentAsset {
  id: string
  name: string
  type: 'image' | 'video'
  url: string
  size: number
}

// ==================== 手势触发模型 ====================

export interface GestureTrigger {
  id: string
  name: string
  type: GestureType
  /** 目标页面ID */
  targetPageId: string | null
  /** 目标页面方向：next/prev */
  direction: 'next' | 'prev' | 'specific'
  enabled: boolean
}

export type GestureType =
  | 'swipe-up'          // 三指上滑
  | 'swipe-down'        // 三指下滑
  | 'swipe-left'        // 三指左滑
  | 'swipe-right'       // 三指右滑
  | 'swipe-up-two'      // 双指上滑
  | 'swipe-down-two'    // 双指下滑
  | 'swipe-left-two'    // 双指左滑
  | 'swipe-right-two'   // 双指右滑
  | 'pinch-in'          // 捏合
  | 'pinch-out'         // 展开
  | 'rotate-cw'         // 顺时针旋转
  | 'rotate-ccw'        // 逆时针旋转
  | 'long-press'        // 长按
  | 'double-tap'         // 双击
  | 'three-finger-tap'  // 三指点击

export const GESTURE_LABELS: Record<GestureType, string> = {
  'swipe-up': '三指上滑',
  'swipe-down': '三指下滑',
  'swipe-left': '三指左滑',
  'swipe-right': '三指右滑',
  'swipe-up-two': '双指上滑',
  'swipe-down-two': '双指下滑',
  'swipe-left-two': '双指左滑',
  'swipe-right-two': '双指右滑',
  'pinch-in': '捏合',
  'pinch-out': '展开',
  'rotate-cw': '顺时针旋转',
  'rotate-ccw': '逆时针旋转',
  'long-press': '长按',
  'double-tap': '双击',
  'three-finger-tap': '三指点击',
}

// ==================== 方案扩展模型 ====================

export interface SchemePageRef {
  pageId: string
  /** 在方案中的顺序 */
  order: number
}

export interface SchemeGestureConfig {
  pageId: string
  gestureType: GestureType
  targetPageId: string | null
  direction: 'next' | 'prev' | 'specific'
}

// ==================== Store ====================

export const useDesignStore = defineStore('design', () => {
  // 页面列表
  const pages = ref<PageDesign[]>([])
  const currentPageId = ref<string | null>(null)

  // 组件列表
  const components = ref<ComponentDesign[]>([])
  const currentComponentId = ref<string | null>(null)

  const currentPage = computed(() =>
    pages.value.find((p) => p.id === currentPageId.value)
  )

  const currentComponent = computed(() =>
    components.value.find((c) => c.id === currentComponentId.value)
  )

  // ==================== 防重名 ====================

  function generateUniqueName(baseName: string, existingNames: string[]): string {
    if (!existingNames.includes(baseName)) return baseName
    let i = 1
    while (existingNames.includes(`${baseName} ${i}`)) i++
    return `${baseName} ${i}`
  }

  function checkDuplicateName(name: string, type: 'page' | 'component', excludeId?: string): boolean {
    const list = type === 'page' ? pages.value : components.value
    return list.some(item => item.name === name && item.id !== excludeId)
  }

  // ==================== 页面操作 ====================

  function createPage(partial?: Partial<PageDesign>): PageDesign {
    const existingNames = pages.value.map(p => p.name)
    const name = generateUniqueName(partial?.name ?? '新页面', existingNames)
    const page: PageDesign = {
      id: crypto.randomUUID().slice(0, 8),
      name: partial?.name ?? '新页面',
      orientation: partial?.orientation ?? 'vertical',
      rows: partial?.rows ?? 4,
      columns: partial?.columns ?? 3,
      padding: partial?.padding ?? { top: 16, right: 16, bottom: 16, left: 16 },
      customPadding: partial?.customPadding ?? false,
      background: partial?.background ?? { type: 'color', color: '#111827', imageUrl: '', videoUrl: '' },
      cells: partial?.cells ?? generateCells(partial?.rows ?? 4, partial?.columns ?? 3),
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    }
    pages.value.push(page)
    return page
  }

  function updatePage(page: PageDesign) {
    const idx = pages.value.findIndex((p) => p.id === page.id)
    if (idx >= 0) {
      page.updatedAt = new Date().toISOString()
      pages.value[idx] = { ...page }
    }
  }

  function deletePage(pageId: string) {
    pages.value = pages.value.filter((p) => p.id !== pageId)
    if (currentPageId.value === pageId) currentPageId.value = null
  }

  function selectPage(pageId: string | null) {
    currentPageId.value = pageId
  }

  /** 重新生成格子（当行列数改变时） */
  function regenerateCells(pageId: string, rows: number, columns: number) {
    const page = pages.value.find((p) => p.id === pageId)
    if (!page) return
    page.rows = rows
    page.columns = columns
    page.cells = generateCells(rows, columns)
    page.updatedAt = new Date().toISOString()
  }

  function generateCells(rows: number, columns: number): PageCell[] {
    const cells: PageCell[] = []
    for (let r = 0; r < rows; r++) {
      for (let c = 0; c < columns; c++) {
        cells.push({
          id: crypto.randomUUID().slice(0, 8),
          row: r,
          column: c,
          rowSpan: 1,
          columnSpan: 1,
          componentId: null,
        })
      }
    }
    return cells
  }

  /** 更新格子属性 */
  function updateCell(pageId: string, cellId: string, updates: Partial<PageCell>) {
    const page = pages.value.find((p) => p.id === pageId)
    if (!page) return
    const cell = page.cells.find((c) => c.id === cellId)
    if (!cell) return
    Object.assign(cell, updates)
    page.updatedAt = new Date().toISOString()
  }

  // ==================== 组件操作 ====================

  function createComponent(partial?: Partial<ComponentDesign>): ComponentDesign {
    const existingNames = components.value.map(c => c.name)
    const name = generateUniqueName(partial?.name ?? '新组件', existingNames)
    const comp: ComponentDesign = {
      id: crypto.randomUUID().slice(0, 8),
      name: partial?.name ?? '新组件',
      description: partial?.description ?? '',
      templateCode: partial?.templateCode ?? `<div class="comp">\n  <span>{{ message }}</span>\n</div>`,
      scriptCode: partial?.scriptCode ?? `export default {\n  data() {\n    return { message: 'Hello' }\n  }\n}`,
      styleCode: partial?.styleCode ?? `.comp {\n  display: flex;\n  align-items: center;\n  justify-content: center;\n}`,
      assets: partial?.assets ?? [],
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    }
    components.value.push(comp)
    return comp
  }

  function updateComponent(comp: ComponentDesign) {
    const idx = components.value.findIndex((c) => c.id === comp.id)
    if (idx >= 0) {
      comp.updatedAt = new Date().toISOString()
      components.value[idx] = { ...comp }
    }
  }

  function deleteComponent(compId: string) {
    components.value = components.value.filter((c) => c.id !== compId)
    if (currentComponentId.value === compId) currentComponentId.value = null
  }

  function selectComponent(compId: string | null) {
    currentComponentId.value = compId
  }

  return {
    pages, currentPageId, currentPage,
    components, currentComponentId, currentComponent,
    createPage, updatePage, deletePage, selectPage,
    regenerateCells, updateCell,
    createComponent, updateComponent, deleteComponent, selectComponent,
    generateUniqueName, checkDuplicateName,
  }
})
