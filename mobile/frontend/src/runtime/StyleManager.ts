/**
 * 样式管理器
 * 管理插件 Scoped CSS 的动态注入和卸载
 * 通过 Vue Scoped CSS 机制实现样式隔离
 */

class StyleManager {
  // 已注入的样式：styleId → <style> 元素
  private injectedStyles = new Map<string, HTMLStyleElement>()

  /**
   * 注入插件样式
   */
  inject(styleId: string, css: string, pluginId: string): void {
    // 移除旧样式（如果存在）
    this.remove(styleId)

    const styleEl = document.createElement('style')
    styleEl.id = styleId
    styleEl.setAttribute('data-plugin', pluginId)

    // Vue Scoped CSS 使用 [data-v-xxxxx] 属性选择器
    // 编译阶段会自动添加 scoped hash，此处直接注入即可
    styleEl.textContent = css

    document.head.appendChild(styleEl)
    this.injectedStyles.set(styleId, styleEl)
  }

  /**
   * 移除插件样式
   */
  remove(styleId: string): void {
    const styleEl = this.injectedStyles.get(styleId)
    if (styleEl) {
      styleEl.remove()
      this.injectedStyles.delete(styleId)
    }
  }

  /**
   * 移除插件的所有样式
   */
  removeByPlugin(pluginId: string): void {
    for (const [styleId, styleEl] of this.injectedStyles.entries()) {
      if (styleEl.getAttribute('data-plugin') === pluginId) {
        styleEl.remove()
        this.injectedStyles.delete(styleId)
      }
    }
  }

  /**
   * 清除所有插件样式
   */
  clearAll(): void {
    for (const styleEl of this.injectedStyles.values()) {
      styleEl.remove()
    }
    this.injectedStyles.clear()
  }

  /**
   * 获取已注入的样式数量
   */
  get count(): number {
    return this.injectedStyles.size
  }
}

export const styleManager = new StyleManager()
