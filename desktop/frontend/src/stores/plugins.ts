import { defineStore } from 'pinia'
import { ref, computed } from 'vue'

export interface PluginInfo {
  id: string
  name: string
  version: string
  description: string
  author: string
  icon: string
  isEnabled: boolean
  installedAt: string
}

export const usePluginStore = defineStore('plugins', () => {
  const plugins = ref<PluginInfo[]>([])
  const loading = ref(false)

  const enabledPlugins = computed(() => plugins.value.filter((p) => p.isEnabled))
  const disabledPlugins = computed(() => plugins.value.filter((p) => !p.isEnabled))

  function setPlugins(list: PluginInfo[]) {
    plugins.value = list
  }

  function addPlugin(plugin: PluginInfo) {
    plugins.value.push(plugin)
  }

  function removePlugin(pluginId: string) {
    plugins.value = plugins.value.filter((p) => p.id !== pluginId)
  }

  function togglePlugin(pluginId: string) {
    const plugin = plugins.value.find((p) => p.id === pluginId)
    if (plugin) {
      plugin.isEnabled = !plugin.isEnabled
    }
  }

  function setLoading(val: boolean) {
    loading.value = val
  }

  return {
    plugins,
    loading,
    enabledPlugins,
    disabledPlugins,
    setPlugins,
    addPlugin,
    removePlugin,
    togglePlugin,
    setLoading,
  }
})
