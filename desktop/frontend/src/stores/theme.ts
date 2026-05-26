import { defineStore } from 'pinia'
import { ref, watch } from 'vue'

export type ThemeMode = 'light' | 'dark' | 'system'

export const useThemeStore = defineStore('theme', () => {
  const mode = ref<ThemeMode>('dark')

  function getSystemTheme(): 'light' | 'dark' {
    if (window.matchMedia('(prefers-color-scheme: dark)').matches) {
      return 'dark'
    }
    return 'light'
  }

  function getEffectiveTheme(): 'light' | 'dark' {
    if (mode.value === 'system') return getSystemTheme()
    return mode.value
  }

  function applyTheme() {
    const effective = getEffectiveTheme()
    document.documentElement.classList.toggle('dark', effective === 'dark')
    document.documentElement.classList.toggle('light', effective === 'light')
  }

  function setMode(newMode: ThemeMode) {
    mode.value = newMode
    applyTheme()
    localStorage.setItem('onedesk-theme', newMode)
  }

  function initTheme() {
    const saved = localStorage.getItem('onedesk-theme') as ThemeMode | null
    if (saved && ['light', 'dark', 'system'].includes(saved)) {
      mode.value = saved
    }
    applyTheme()

    // 监听系统主题变化
    window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', () => {
      if (mode.value === 'system') applyTheme()
    })
  }

  return { mode, getEffectiveTheme, setMode, initTheme }
})
