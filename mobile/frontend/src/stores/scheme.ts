import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { SchemeLayout, SchemePage, SchemeSlot, SchemePluginInstance } from '@/runtime/PluginSandbox'

export const useSchemeStore = defineStore('scheme', () => {
  const schemeId = ref<string | null>(null)
  const schemeName = ref('')
  const layout = ref<SchemeLayout | null>(null)
  const plugins = ref<SchemePluginInstance[]>([])
  const version = ref(0)
  const currentPageIndex = ref(0)

  // 插件模块缓存：instanceId → 编译后的 JS 模块
  const loadedModules = ref<Map<string, any>>(new Map())
  // 插件状态缓存：instanceId → state
  const pluginStates = ref<Map<string, Record<string, unknown>>>(new Map())

  const currentPage = computed(() =>
    layout.value?.pages[currentPageIndex.value] ?? null
  )

  const isSchemeLoaded = computed(() => schemeId.value !== null)

  function loadScheme(payload: {
    schemeId: string
    schemeName: string
    layout: SchemeLayout
    plugins: SchemePluginInstance[]
    version: number
  }) {
    schemeId.value = payload.schemeId
    schemeName.value = payload.schemeName
    layout.value = payload.layout
    plugins.value = payload.plugins
    version.value = payload.version
    currentPageIndex.value = 0
  }

  function loadModule(payload: {
    pluginId: string
    instanceId: string
    moduleCode: string
    moduleId: string
    version: string
  }) {
    try {
      // 使用 Function 构造器在沙箱中执行模块代码
      // 模块代码格式：(function(exports, require, module, PluginContext) { ... })
      const moduleFactory = new Function('exports', 'require', 'module', 'PluginContext', payload.moduleCode)
      const moduleExports: any = {}
      const moduleObj = { exports: moduleExports }

      moduleFactory(moduleExports, () => ({}), moduleObj, null)

      loadedModules.value.set(payload.instanceId, moduleObj.exports)
    } catch (e) {
      console.error(`[OneDeck] Failed to load module ${payload.pluginId}:`, e)
    }
  }

  function loadStyle(payload: {
    pluginId: string
    css: string
    styleId: string
  }) {
    // 动态注入 Scoped CSS
    let styleEl = document.getElementById(payload.styleId)
    if (!styleEl) {
      styleEl = document.createElement('style')
      styleEl.id = payload.styleId
      document.head.appendChild(styleEl)
    }
    styleEl.textContent = payload.css
  }

  function updatePluginState(pluginId: string, instanceId: string, state: Record<string, unknown>) {
    pluginStates.value.set(instanceId, state)
  }

  function nextPage() {
    if (layout.value && currentPageIndex.value < layout.value.pages.length - 1) {
      currentPageIndex.value++
    }
  }

  function prevPage() {
    if (currentPageIndex.value > 0) {
      currentPageIndex.value--
    }
  }

  function goToPage(index: number) {
    if (layout.value && index >= 0 && index < layout.value.pages.length) {
      currentPageIndex.value = index
    }
  }

  return {
    schemeId,
    schemeName,
    layout,
    plugins,
    version,
    currentPageIndex,
    loadedModules,
    pluginStates,
    currentPage,
    isSchemeLoaded,
    loadScheme,
    loadModule,
    loadStyle,
    updatePluginState,
    nextPage,
    prevPage,
    goToPage,
  }
})
