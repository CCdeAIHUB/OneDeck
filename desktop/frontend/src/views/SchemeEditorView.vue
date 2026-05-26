<script setup lang="ts">
import { Icon } from '@iconify/vue'
import PageHeader from '@/components/PageHeader.vue'
import { useSchemeStore } from '@/stores/schemes'
import { useRoute, useRouter } from 'vue-router'
import { computed, ref } from 'vue'

const route = useRoute()
const router = useRouter()
const schemeStore = useSchemeStore()

const scheme = computed(() =>
  schemeStore.schemes.find((s) => s.id === route.params.id)
)

const currentPageIndex = ref(0)
const currentPage = computed(() =>
  scheme.value?.layout.pages[currentPageIndex.value]
)

function toggleEditorMode() {
  schemeStore.setEditorMode(schemeStore.editorMode === 'visual' ? 'code' : 'visual')
}

function addPage() {
  if (!scheme.value) return
  // TODO: 添加新页面
}

function addSlot() {
  if (!scheme.value || !currentPage.value) return
  // TODO: 在当前页面添加新插槽
}

function goBack() {
  router.push('/schemes')
}

function saveScheme() {
  if (!scheme.value) return
  // TODO: 调用后端保存方案
}

function pushToDevice() {
  if (!scheme.value) return
  // TODO: 调用后端推送方案到设备
}
</script>

<template>
  <div v-if="scheme">
    <PageHeader
      :title="`编辑方案：${scheme.name}`"
      :subtitle="`版本 ${scheme.version} · ${schemeStore.editorMode === 'visual' ? '可视化编辑' : '代码编辑'}`"
      icon="solar:widget-bold"
    >
      <template #actions>
        <button
          class="flex items-center gap-2 px-3 py-2 bg-gray-700 hover:bg-gray-600 rounded-lg text-sm transition-colors"
          @click="toggleEditorMode"
        >
          <Icon :icon="schemeStore.editorMode === 'visual' ? 'solar:code-bold' : 'solar:eye-bold'" class="text-base" />
          {{ schemeStore.editorMode === 'visual' ? '代码模式' : '可视化模式' }}
        </button>
        <button
          class="flex items-center gap-2 px-3 py-2 bg-gray-700 hover:bg-gray-600 rounded-lg text-sm transition-colors"
          @click="goBack"
        >
          返回
        </button>
        <button
          class="flex items-center gap-2 px-4 py-2 bg-indigo-600 hover:bg-indigo-500 rounded-lg text-sm transition-colors"
          @click="saveScheme"
        >
          <Icon icon="solar:diskette-bold" class="text-base" />
          保存
        </button>
        <button
          class="flex items-center gap-2 px-4 py-2 bg-emerald-600 hover:bg-emerald-500 rounded-lg text-sm transition-colors"
          @click="pushToDevice"
        >
          <Icon icon="solar:upload-bold" class="text-base" />
          推送到设备
        </button>
      </template>
    </PageHeader>

    <!-- 可视化编辑器 -->
    <div v-if="schemeStore.editorMode === 'visual'" class="flex gap-6">
      <!-- 页面列表 -->
      <div class="w-48 shrink-0">
        <div class="flex items-center justify-between mb-3">
          <h3 class="text-sm font-semibold">页面</h3>
          <button
            class="w-6 h-6 flex items-center justify-center rounded bg-gray-800 hover:bg-gray-700 transition-colors"
            @click="addPage"
          >
            <Icon icon="solar:add-circle-bold" class="text-sm" />
          </button>
        </div>
        <div class="space-y-1">
          <button
            v-for="(page, idx) in scheme.layout.pages"
            :key="page.id"
            class="w-full px-3 py-2 text-left text-sm rounded-lg transition-colors"
            :class="idx === currentPageIndex ? 'bg-indigo-600 text-white' : 'bg-gray-800 hover:bg-gray-700 text-gray-300'"
            @click="currentPageIndex = idx"
          >
            {{ page.name }}
          </button>
        </div>
      </div>

      <!-- 画布区域 -->
      <div class="flex-1 bg-gray-900 border border-gray-800 rounded-xl p-6 min-h-[500px]">
        <div
          v-if="currentPage"
          class="grid gap-2"
          :style="{
            gridTemplateColumns: `repeat(${scheme.layout.columns}, 1fr)`,
            gridTemplateRows: `repeat(${scheme.layout.rows}, 1fr)`,
          }"
        >
          <template v-for="row in scheme.layout.rows" :key="row">
            <template v-for="col in scheme.layout.columns" :key="`${row}-${col}`">
              <div
                class="border border-dashed border-gray-700 rounded-lg min-h-[80px] flex items-center justify-center text-gray-600 hover:border-indigo-500/50 hover:text-indigo-400 transition-colors cursor-pointer"
                @click="addSlot"
              >
                <Icon icon="solar:add-circle-linear" class="text-xl" />
              </div>
            </template>
          </template>
        </div>
      </div>

      <!-- 属性面板 -->
      <div class="w-64 shrink-0">
        <h3 class="text-sm font-semibold mb-3">属性</h3>
        <div class="bg-gray-900 border border-gray-800 rounded-xl p-4 text-sm">
          <p class="text-gray-500">选中插槽后显示属性</p>
        </div>
      </div>
    </div>

    <!-- 代码编辑器 -->
    <div v-else class="bg-gray-900 border border-gray-800 rounded-xl p-4">
      <div class="font-mono text-sm text-gray-300 h-[600px] overflow-auto">
        <pre>{{ JSON.stringify(scheme, null, 2) }}</pre>
      </div>
    </div>
  </div>

  <div v-else class="text-center py-20 text-gray-500">
    <p>方案未找到</p>
    <button
      class="mt-4 px-4 py-2 bg-gray-700 hover:bg-gray-600 rounded-lg text-sm transition-colors"
      @click="goBack"
    >
      返回方案列表
    </button>
  </div>
</template>
