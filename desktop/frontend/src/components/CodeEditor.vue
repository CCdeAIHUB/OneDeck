<script setup lang="ts">
import { ref, onMounted, onUnmounted, watch, shallowRef } from 'vue'
import { EditorState } from '@codemirror/state'
import { EditorView, keymap, lineNumbers, highlightActiveLineGutter, highlightSpecialChars, drawSelection, highlightActiveLine } from '@codemirror/view'
import { defaultKeymap, indentWithTab, history, historyKeymap } from '@codemirror/commands'
import { syntaxHighlighting, defaultHighlightStyle, bracketMatching, foldGutter, indentOnInput } from '@codemirror/language'
import { javascript } from '@codemirror/lang-javascript'
import { css } from '@codemirror/lang-css'
import { html } from '@codemirror/lang-html'
import { json } from '@codemirror/lang-json'
import { oneDark } from '@codemirror/theme-one-dark'
import { useThemeStore } from '@/stores/theme'

const props = withDefaults(defineProps<{
  modelValue: string
  language?: 'javascript' | 'typescript' | 'css' | 'html' | 'json' | 'vue'
  readOnly?: boolean
  placeholder?: string
}>(), {
  language: 'javascript',
  readOnly: false,
  placeholder: '',
})

const emit = defineEmits<{
  'update:modelValue': [value: string]
}>()

const editorRef = ref<HTMLElement | null>(null)
const editorView = shallowRef<EditorView | null>(null)
const themeStore = useThemeStore()

function getLanguageExtension() {
  switch (props.language) {
    case 'typescript': return javascript({ typescript: true })
    case 'javascript': return javascript()
    case 'css': return css()
    case 'html': return html()
    case 'json': return json()
    case 'vue': return html() // Vue SFC uses HTML highlighting
    default: return javascript()
  }
}

function createState(content: string): EditorState {
  const isDark = document.documentElement.classList.contains('dark')
  return EditorState.create({
    doc: content,
    extensions: [
      lineNumbers(),
      highlightActiveLineGutter(),
      highlightSpecialChars(),
      history(),
      foldGutter(),
      drawSelection(),
      indentOnInput(),
      bracketMatching(),
      highlightActiveLine(),
      keymap.of([...defaultKeymap, ...historyKeymap, indentWithTab]),
      syntaxHighlighting(defaultHighlightStyle, { fallback: true }),
      getLanguageExtension(),
      isDark ? oneDark : [],
      EditorView.updateListener.of((update) => {
        if (update.docChanged) {
          emit('update:modelValue', update.state.doc.toString())
        }
      }),
      EditorView.theme({
        '&': { height: '100%', fontSize: '13px' },
        '.cm-scroller': { overflow: 'auto' },
        '.cm-gutters': { borderRight: '1px solid var(--color-border-subtle)' },
        ...(isDark ? {} : {
          '&': { backgroundColor: 'var(--color-bg-card)' },
          '.cm-gutters': { backgroundColor: 'var(--color-bg-surface)', color: 'var(--color-text-dim)' },
          '.cm-activeLineGutter': { backgroundColor: 'var(--color-bg-hover)' },
          '.cm-activeLine': { backgroundColor: 'rgba(59,130,246,0.05)' },
        }),
      }),
      props.readOnly ? EditorState.readOnly.of(true) : [],
      EditorView.lineWrapping,
    ],
  })
}

onMounted(() => {
  if (editorRef.value) {
    editorView.value = new EditorView({
      state: createState(props.modelValue),
      parent: editorRef.value,
    })
  }
})

onUnmounted(() => {
  editorView.value?.destroy()
})

// 外部 modelValue 变更时更新编辑器（避免光标重置）
watch(() => props.modelValue, (newValue) => {
  if (editorView.value && editorView.value.state.doc.toString() !== newValue) {
    editorView.value.dispatch({
      changes: {
        from: 0,
        to: editorView.value.state.doc.length,
        insert: newValue,
      },
    })
  }
})

// 语言变更时重建编辑器
watch(() => props.language, () => {
  if (editorView.value && editorRef.value) {
    const content = editorView.value.state.doc.toString()
    editorView.value.destroy()
    editorView.value = new EditorView({
      state: createState(content),
      parent: editorRef.value,
    })
  }
})

// 主题变更时重建编辑器（同步深浅色）
watch(() => themeStore.getEffectiveTheme(), () => {
  if (editorView.value && editorRef.value) {
    const content = editorView.value.state.doc.toString()
    editorView.value.destroy()
    editorView.value = new EditorView({
      state: createState(content),
      parent: editorRef.value,
    })
  }
})

defineExpose({ editorView })
</script>

<template>
  <div ref="editorRef" class="code-editor w-full h-full rounded-xl overflow-hidden" style="border: 1px solid var(--color-border);" />
</template>

<style>
.code-editor .cm-editor {
  height: 100%;
  border-radius: 0.75rem;
}

.code-editor .cm-editor .cm-scroller {
  font-family: 'Cascadia Code', 'Fira Code', 'JetBrains Mono', 'Consolas', monospace;
}

.code-editor .cm-editor.cm-focused {
  outline: none;
}
</style>
