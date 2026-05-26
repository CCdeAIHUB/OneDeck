import { defineStore } from 'pinia'
import { ref, computed } from 'vue'

export interface SchemeSlot {
  id: string
  pluginId: string
  row: number
  column: number
  rowSpan: number
  columnSpan: number
}

export interface SchemePage {
  id: string
  name: string
  slots: SchemeSlot[]
}

export interface SchemeLayout {
  type: 'grid' | 'free' | 'tabs'
  columns: number
  rows: number
  pages: SchemePage[]
}

export interface SchemePluginInstance {
  pluginId: string
  instanceId: string
  config: Record<string, unknown>
}

export interface Scheme {
  id: string
  name: string
  targetDeviceId: string
  layout: SchemeLayout
  plugins: SchemePluginInstance[]
  version: number
  createdAt: string
  updatedAt: string
}

export const useSchemeStore = defineStore('schemes', () => {
  const schemes = ref<Scheme[]>([])
  const currentSchemeId = ref<string | null>(null)
  const editorMode = ref<'visual' | 'code'>('visual')

  const currentScheme = computed(() =>
    schemes.value.find((s) => s.id === currentSchemeId.value)
  )

  const schemesForDevice = computed(() => {
    return (deviceId: string) => schemes.value.filter((s) => s.targetDeviceId === deviceId)
  })

  function setSchemes(list: Scheme[]) {
    schemes.value = list
  }

  function addScheme(scheme: Scheme) {
    schemes.value.push(scheme)
  }

  function updateScheme(scheme: Scheme) {
    const idx = schemes.value.findIndex((s) => s.id === scheme.id)
    if (idx >= 0) {
      schemes.value[idx] = scheme
    }
  }

  function removeScheme(schemeId: string) {
    schemes.value = schemes.value.filter((s) => s.id !== schemeId)
    if (currentSchemeId.value === schemeId) {
      currentSchemeId.value = null
    }
  }

  function selectScheme(schemeId: string | null) {
    currentSchemeId.value = schemeId
  }

  function setEditorMode(mode: 'visual' | 'code') {
    editorMode.value = mode
  }

  return {
    schemes,
    currentSchemeId,
    editorMode,
    currentScheme,
    schemesForDevice,
    setSchemes,
    addScheme,
    updateScheme,
    removeScheme,
    selectScheme,
    setEditorMode,
  }
})
