import type { PluginContext } from './PluginSandbox'
import { createPluginContext } from './PluginSandbox'

/**
 * 插件模块加载器
 * 负责解析和实例化从桌面端下发的完整 JS 模块
 *
 * 模块格式约定：
 * 编译后的模块代码是一个自执行函数，接收 PluginContext 作为参数
 * 并返回一个包含渲染函数、生命周期钩子和状态定义的对象
 *
 * 示例编译后代码结构：
 * (function(ctx) {
 *   const { ref, computed, onMounted, onUnmounted } = VueRuntime;
 *   const count = ref(0);
 *   const doubled = computed(() => count.value * 2);
 *   function increment() { count.value++; ctx.setState({ count: count.value }); }
 *   onMounted(() => { ctx.log.info('mounted'); });
 *   return {
 *     state: { count, doubled },
 *     methods: { increment },
 *     render: () => h('div', { onClick: increment }, [count.value])
 *   };
 * })
 */

export interface PluginModule {
  state: Record<string, unknown>
  methods: Record<string, (...args: unknown[]) => unknown>
  render: () => unknown
  onMount?: () => void
  onUnmount?: () => void
  onStateUpdate?: (state: Record<string, unknown>) => void
  onCommand?: (command: string, data?: unknown) => void
}

interface LoadedPlugin {
  instanceId: string
  pluginId: string
  module: PluginModule
  context: PluginContext
  element?: HTMLElement
}

class ModuleLoader {
  private loadedPlugins = new Map<string, LoadedPlugin>()
  private vueRuntime: Record<string, unknown> = {}

  /**
   * 注入 Vue 运行时（ref, reactive, computed, watch, onMounted 等）
   */
  injectVueRuntime(runtime: Record<string, unknown>) {
    this.vueRuntime = runtime
  }

  /**
   * 加载并实例化插件模块
   */
  loadModule(
    instanceId: string,
    pluginId: string,
    moduleCode: string,
    config: Record<string, unknown>
  ): PluginModule | null {
    try {
      // 创建插件上下文
      const context = createPluginContext(pluginId, instanceId, config)

      // 在受控环境中执行模块代码
      const moduleFactory = this.createModuleFactory(moduleCode)

      // 注入 Vue 运行时和插件上下文
      const pluginModule = moduleFactory(context, this.vueRuntime)

      if (!pluginModule || typeof pluginModule.render !== 'function') {
        throw new Error('Module must export a render function')
      }

      // 保存已加载的插件
      this.loadedPlugins.set(instanceId, {
        instanceId,
        pluginId,
        module: pluginModule,
        context,
      })

      // 调用生命周期
      pluginModule.onMount?.()

      return pluginModule
    } catch (e) {
      console.error(`[ModuleLoader] Failed to load module ${pluginId}/${instanceId}:`, e)
      return null
    }
  }

  /**
   * 卸载插件模块
   */
  unloadModule(instanceId: string) {
    const loaded = this.loadedPlugins.get(instanceId)
    if (loaded) {
      loaded.module.onUnmount?.()
      loaded.element?.remove()
      this.loadedPlugins.delete(instanceId)
    }
  }

  /**
   * 获取已加载的插件
   */
  getPlugin(instanceId: string): LoadedPlugin | undefined {
    return this.loadedPlugins.get(instanceId)
  }

  /**
   * 获取所有已加载插件
   */
  getAllPlugins(): LoadedPlugin[] {
    return Array.from(this.loadedPlugins.values())
  }

  /**
   * 创建模块工厂函数
   * 使用 Function 构造器在受限作用域中执行模块代码
   */
  private createModuleFactory(moduleCode: string): (ctx: PluginContext, vueRuntime: Record<string, unknown>) => PluginModule {
    // 模块代码预期格式：(function(ctx, VueRuntime) { ... })
    // 使用 Function 构造器避免访问外部作用域
    const factory = new Function(
      'ctx',
      'VueRuntime',
      `"use strict"; return (${moduleCode})(ctx, VueRuntime);`
    ) as (ctx: PluginContext, vueRuntime: Record<string, unknown>) => PluginModule

    return factory
  }

  /**
   * 更新插件状态（由桌面端同步过来）
   */
  updatePluginState(instanceId: string, state: Record<string, unknown>) {
    const loaded = this.loadedPlugins.get(instanceId)
    if (loaded?.module.onStateUpdate) {
      loaded.module.onStateUpdate(state)
    }
  }

  /**
   * 向插件发送命令
   */
  sendCommand(instanceId: string, command: string, data?: unknown) {
    const loaded = this.loadedPlugins.get(instanceId)
    if (loaded?.module.onCommand) {
      loaded.module.onCommand(command, data)
    }
  }
}

// 单例导出
export const moduleLoader = new ModuleLoader()
