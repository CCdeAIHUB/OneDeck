<script setup lang="ts">
import { Icon } from '@iconify/vue'
import PageHeader from '@/components/PageHeader.vue'
import { useSchemeStore } from '@/stores/schemes'
import { useRouter } from 'vue-router'

const schemeStore = useSchemeStore()
const router = useRouter()

function createScheme() {
  // TODO: 打开创建方案弹窗
}

function editScheme(id: string) {
  router.push(`/schemes/${id}/editor`)
}
</script>

<template>
  <div>
    <PageHeader title="方案管理" subtitle="设计移动端界面方案" icon="solar:widget-bold">
      <template #actions>
        <button
          class="flex items-center gap-2 px-4 py-2 bg-indigo-600 hover:bg-indigo-500 rounded-lg text-sm transition-colors"
          @click="createScheme"
        >
          <Icon icon="solar:add-circle-bold" class="text-base" />
          新建方案
        </button>
      </template>
    </PageHeader>

    <div v-if="schemeStore.schemes.length === 0" class="text-center py-20">
      <Icon icon="solar:widget-bold" class="text-6xl text-gray-700 mb-4 mx-auto block" />
      <h3 class="text-lg font-semibold text-gray-400 mb-2">暂无方案</h3>
      <p class="text-sm text-gray-500">创建一个方案，为移动端设计界面布局和交互逻辑</p>
      <button
        class="mt-4 px-4 py-2 bg-indigo-600 hover:bg-indigo-500 rounded-lg text-sm transition-colors"
        @click="createScheme"
      >
        创建第一个方案
      </button>
    </div>

    <div v-else class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
      <div
        v-for="scheme in schemeStore.schemes"
        :key="scheme.id"
        class="bg-gray-900 border border-gray-800 rounded-xl p-5 hover:border-indigo-500/50 transition-all duration-200 cursor-pointer"
        @click="editScheme(scheme.id)"
      >
        <div class="flex items-start justify-between mb-3">
          <h3 class="font-semibold">{{ scheme.name }}</h3>
          <span class="text-xs text-gray-500">v{{ scheme.version }}</span>
        </div>

        <div class="text-xs text-gray-400 space-y-1">
          <p>布局：{{ scheme.layout.type }}（{{ scheme.layout.columns }}×{{ scheme.layout.rows }}）</p>
          <p>页面数：{{ scheme.layout.pages.length }}</p>
          <p>插件数：{{ scheme.plugins.length }}</p>
        </div>

        <div class="flex items-center justify-between mt-4 pt-3 border-t border-gray-800 text-xs text-gray-500">
          <span>更新于 {{ new Date(scheme.updatedAt).toLocaleDateString() }}</span>
          <Icon icon="solar:pen-bold" class="text-sm text-indigo-400" />
        </div>
      </div>
    </div>
  </div>
</template>
