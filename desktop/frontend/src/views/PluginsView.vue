<script setup lang="ts">
import { Icon } from '@iconify/vue'
import PageHeader from '@/components/PageHeader.vue'
import { usePluginStore } from '@/stores/plugins'

const pluginStore = usePluginStore()

function installPlugin() {
  // TODO: 打开插件安装弹窗（选择 .zip 或目录）
}

function togglePlugin(pluginId: string) {
  pluginStore.togglePlugin(pluginId)
  // TODO: 通知后端启用/禁用插件
}

function uninstallPlugin(pluginId: string) {
  // TODO: 确认后卸载插件
}
</script>

<template>
  <div>
    <PageHeader title="插件管理" subtitle="安装、配置和管理插件" icon="solar:widget-2-bold">
      <template #actions>
        <button
          class="flex items-center gap-2 px-4 py-2 bg-indigo-600 hover:bg-indigo-500 rounded-lg text-sm transition-colors"
          @click="installPlugin"
        >
          <Icon icon="solar:install-bold" class="text-base" />
          安装插件
        </button>
      </template>
    </PageHeader>

    <div v-if="pluginStore.plugins.length === 0" class="text-center py-20">
      <Icon icon="solar:widget-2-bold" class="text-6xl text-gray-700 mb-4 mx-auto block" />
      <h3 class="text-lg font-semibold text-gray-400 mb-2">暂无插件</h3>
      <p class="text-sm text-gray-500">安装插件来扩展 OneDeck 的功能</p>
      <button
        class="mt-4 px-4 py-2 bg-indigo-600 hover:bg-indigo-500 rounded-lg text-sm transition-colors"
        @click="installPlugin"
      >
        安装第一个插件
      </button>
    </div>

    <div v-else class="space-y-3">
      <div
        v-for="plugin in pluginStore.plugins"
        :key="plugin.id"
        class="bg-gray-900 border border-gray-800 rounded-xl p-4 flex items-center justify-between hover:border-gray-700 transition-all duration-200"
      >
        <div class="flex items-center gap-4">
          <div class="w-12 h-12 bg-gray-800 rounded-lg flex items-center justify-center">
            <Icon v-if="plugin.icon" :icon="plugin.icon" class="text-2xl" />
            <Icon v-else icon="solar:widget-2-bold" class="text-2xl text-gray-500" />
          </div>
          <div>
            <h3 class="font-semibold">{{ plugin.name }}</h3>
            <p class="text-xs text-gray-400">{{ plugin.description || '暂无描述' }}</p>
            <p class="text-xs text-gray-500 mt-0.5">v{{ plugin.version }} · {{ plugin.author }}</p>
          </div>
        </div>

        <div class="flex items-center gap-3">
          <!-- 启用/禁用开关 -->
          <button
            class="relative w-10 h-5 rounded-full transition-colors duration-200"
            :class="plugin.isEnabled ? 'bg-indigo-600' : 'bg-gray-700'"
            @click="togglePlugin(plugin.id)"
          >
            <span
              class="absolute top-0.5 w-4 h-4 bg-white rounded-full transition-transform duration-200"
              :class="plugin.isEnabled ? 'translate-x-5' : 'translate-x-0.5'"
            />
          </button>

          <button
            class="p-2 text-gray-400 hover:text-red-400 transition-colors"
            title="卸载"
            @click="uninstallPlugin(plugin.id)"
          >
            <Icon icon="solar:trash-bin-trash-bold" class="text-base" />
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
