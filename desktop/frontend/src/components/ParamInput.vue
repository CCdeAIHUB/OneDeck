<script setup lang="ts">
import { Icon } from '@iconify/vue'
import { useSharedParamsStore } from '@/stores/sharedParams'
import { computed, ref, watch, nextTick } from 'vue'

const props = defineProps<{
  modelValue: string
  placeholder?: string
}>()

const emit = defineEmits<{
  'update:modelValue': [value: string]
}>()

const sharedParamsStore = useSharedParamsStore()
const inputRef = ref<HTMLInputElement | null>(null)
const showDropdown = ref(false)
const showPicker = ref(false)
const cursorPos = ref(0)

const paramKeys = computed(() => sharedParamsStore.keys())

// 根据 & 后面的文字过滤参数
const filterText = computed(() => {
  const val = props.modelValue ?? ''
  const pos = cursorPos.value
  // 找到光标前面最近的 & 符号
  const beforeCursor = val.slice(0, pos)
  const ampIndex = beforeCursor.lastIndexOf('&')
  if (ampIndex === -1) return null
  const afterAmp = beforeCursor.slice(ampIndex + 1)
  // 如果 & 后面有空格或非单词字符，不显示联想
  if (/\s/.test(afterAmp)) return null
  return afterAmp
})

const filteredParams = computed(() => {
  if (filterText.value === null) return []
  const ft = filterText.value.toLowerCase()
  return paramKeys.value
    .filter(k => k.toLowerCase().includes(ft) && k.toLowerCase() !== ft)
    .slice(0, 8)
})

function onInput(e: Event) {
  const target = e.target as HTMLInputElement
  cursorPos.value = target.selectionStart ?? target.value.length
  emit('update:modelValue', target.value)
  showDropdown.value = filteredParams.value.length > 0
}

function onFocus() {
  cursorPos.value = inputRef.value?.selectionStart ?? props.modelValue?.length ?? 0
  if (filteredParams.value.length > 0) showDropdown.value = true
}

function selectParam(key: string) {
  const val = props.modelValue ?? ''
  const pos = cursorPos.value
  const beforeCursor = val.slice(0, pos)
  const ampIndex = beforeCursor.lastIndexOf('&')
  if (ampIndex === -1) return
  const newVal = val.slice(0, ampIndex) + '&' + key + val.slice(pos)
  emit('update:modelValue', newVal)
  showDropdown.value = false
  nextTick(() => {
    const newPos = ampIndex + 1 + key.length
    inputRef.value?.setSelectionRange(newPos, newPos)
    inputRef.value?.focus()
  })
}

function onKeydown(e: KeyboardEvent) {
  if (e.key === 'Escape') {
    showDropdown.value = false
  }
}

// 绑定选择器
function selectPickerParam(key: string) {
  const val = props.modelValue ?? ''
  emit('update:modelValue', val + '&' + key)
  showPicker.value = false
  nextTick(() => {
    const newPos = (props.modelValue ?? '').length + 1 + key.length
    inputRef.value?.setSelectionRange(newPos, newPos)
    inputRef.value?.focus()
  })
}

function closeDropdown() {
  // 延迟关闭，以便点击下拉项
  setTimeout(() => { showDropdown.value = false }, 150)
}
</script>

<template>
  <div class="relative flex gap-1">
    <div class="relative flex-1">
      <input
        ref="inputRef"
        :value="modelValue"
        :placeholder="placeholder"
        @input="onInput"
        @focus="onFocus"
        @blur="closeDropdown"
        @keydown="onKeydown"
        class="w-full px-2 py-1 rounded text-xs focus:outline-none"
        style="background-color: var(--color-bg-surface); border: 1px solid var(--color-border); color: var(--color-text);"
      />
      <!-- 参数联想下拉 -->
      <div
        v-if="showDropdown && filteredParams.length > 0"
        class="absolute left-0 top-full mt-1 w-48 rounded-lg shadow-lg border overflow-hidden z-50"
        style="background-color: var(--color-bg-card); border-color: var(--color-border);"
      >
        <div class="px-2 py-1 text-[10px] font-semibold" style="color: var(--color-text-dim); background-color: var(--color-bg-surface);">
          公共参数 — 输入 & 引用
        </div>
        <div
          v-for="key in filteredParams"
          :key="key"
          class="px-2 py-1.5 text-xs cursor-pointer flex items-center gap-2 transition-colors"
          style="color: var(--color-text);"
          @mousedown.prevent="selectParam(key)"
          @mouseenter="($event.target as HTMLElement).style.backgroundColor = 'var(--color-bg-hover)'"
          @mouseleave="($event.target as HTMLElement).style.backgroundColor = ''"
        >
          <Icon icon="solar:link-bold" class="text-xs" style="color: var(--color-primary);" />
          <span style="color: var(--color-primary);">&{{ key }}</span>
          <span class="text-[10px] ml-auto" style="color: var(--color-text-dim);">{{ String(sharedParamsStore.get(key) ?? '').slice(0, 20) }}</span>
        </div>
      </div>
    </div>
    <!-- 绑定按钮 -->
    <div class="relative">
      <button
        @click="showPicker = !showPicker"
        class="px-1.5 py-1 rounded text-xs transition-colors"
        style="background-color: var(--color-bg-surface); border: 1px solid var(--color-border); color: var(--color-primary);"
        title="绑定公共参数"
      >
        <Icon icon="solar:link-bold" class="text-xs" />
      </button>
      <!-- 参数选择弹窗 -->
      <div
        v-if="showPicker"
        class="absolute right-0 top-full mt-1 w-52 rounded-lg shadow-lg border overflow-hidden z-50"
        style="background-color: var(--color-bg-card); border-color: var(--color-border);"
      >
        <div class="px-2 py-1.5 text-[10px] font-semibold flex items-center gap-1" style="color: var(--color-text-dim); background-color: var(--color-bg-surface);">
          <Icon icon="solar:database-bold" class="text-xs" style="color: var(--color-primary);" />
          选择公共参数绑定
        </div>
        <div v-if="paramKeys.length === 0" class="px-2 py-3 text-xs text-center" style="color: var(--color-text-dim);">
          暂无公共参数
        </div>
        <div
          v-for="key in paramKeys"
          :key="key"
          class="px-2 py-1.5 text-xs cursor-pointer flex items-center gap-2 transition-colors"
          style="color: var(--color-text);"
          @click="selectPickerParam(key)"
          @mouseenter="($event.target as HTMLElement).style.backgroundColor = 'var(--color-bg-hover)'"
          @mouseleave="($event.target as HTMLElement).style.backgroundColor = ''"
        >
          <div class="w-5 h-5 rounded flex items-center justify-center shrink-0" style="background-color: rgba(59,130,246,0.15);">
            <Icon icon="solar:link-bold" class="text-[10px]" style="color: var(--color-primary);" />
          </div>
          <div class="min-w-0 flex-1">
            <p class="font-medium truncate" style="color: var(--color-primary);">&{{ key }}</p>
            <p class="text-[10px] truncate" style="color: var(--color-text-dim);">{{ String(sharedParamsStore.get(key) ?? '').slice(0, 30) }}</p>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
