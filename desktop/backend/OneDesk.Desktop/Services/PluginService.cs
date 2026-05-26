using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OneDeck.Desktop.Models;

namespace OneDeck.Desktop.Services;

/// <summary>
/// 插件管理服务
/// 负责插件的安装、卸载、加载、编译和 JS 模块生成
/// </summary>
public class PluginService
{
    private readonly StorageService _storageService;
    private readonly LogService _logService;
    private readonly Dictionary<string, PluginInfo> _loadedPlugins = new();

    public PluginService(StorageService storageService, LogService logService)
    {
        _storageService = storageService;
        _logService = logService;
    }

    /// <summary>
    /// 加载所有已安装的插件
    /// </summary>
    public async Task LoadInstalledPluginsAsync()
    {
        var plugins = await _storageService.GetAllPluginsAsync();
        foreach (var plugin in plugins)
        {
            if (plugin.IsEnabled)
            {
                _loadedPlugins[plugin.Id] = plugin;
                _logService.Info("PluginService", $"Plugin loaded: {plugin.Name} v{plugin.Version}");
            }
        }
    }

    /// <summary>
    /// 安装插件（从 .zip 或目录）
    /// </summary>
    public async Task<PluginInfo> InstallPluginAsync(string sourcePath)
    {
        // TODO: 1. 解析 plugin manifest
        //       2. 编译 Vue 组件为完整 JS 模块（包含逻辑 + 渲染函数 + Scoped CSS）
        //       3. 存储到 SQLite

        var plugin = new PluginInfo
        {
            Id = Guid.NewGuid().ToString("N")[..12],
            Name = "New Plugin",
            Version = "1.0.0",
            EntryPath = sourcePath,
            InstalledAt = DateTime.UtcNow
        };

        await _storageService.SavePluginAsync(plugin);
        _loadedPlugins[plugin.Id] = plugin;

        _logService.Info("PluginService", $"Plugin installed: {plugin.Name}");
        return plugin;
    }

    /// <summary>
    /// 卸载插件
    /// </summary>
    public async Task UninstallPluginAsync(string pluginId)
    {
        _loadedPlugins.Remove(pluginId);
        await _storageService.DeletePluginAsync(pluginId);
        _logService.Info("PluginService", $"Plugin uninstalled: {pluginId}");
    }

    /// <summary>
    /// 启用/禁用插件
    /// </summary>
    public async Task SetPluginEnabledAsync(string pluginId, bool enabled)
    {
        if (_loadedPlugins.TryGetValue(pluginId, out var plugin))
        {
            plugin.IsEnabled = enabled;
            await _storageService.SavePluginAsync(plugin);

            if (enabled)
                _logService.Info("PluginService", $"Plugin enabled: {plugin.Name}");
            else
            {
                _loadedPlugins.Remove(pluginId);
                _logService.Info("PluginService", $"Plugin disabled: {plugin.Name}");
            }
        }
    }

    /// <summary>
    /// 获取插件信息
    /// </summary>
    public Task<PluginInfo?> GetPluginAsync(string pluginId)
    {
        _loadedPlugins.TryGetValue(pluginId, out var plugin);
        return Task.FromResult(plugin);
    }

    /// <summary>
    /// 获取所有已安装插件
    /// </summary>
    public Task<List<PluginInfo>> GetAllPluginsAsync()
    {
        return Task.FromResult(_loadedPlugins.Values.ToList());
    }

    /// <summary>
    /// 编译插件 Vue 组件为完整 JS 模块
    /// 输出包含：渲染函数 + 响应式状态 + 方法 + 生命周期
    /// </summary>
    public async Task<string> CompilePluginAsync(string pluginId)
    {
        if (!_loadedPlugins.TryGetValue(pluginId, out var plugin))
            throw new InvalidOperationException($"Plugin not found: {pluginId}");

        // TODO: 调用 Vite/esbuild 编译流程：
        // 1. 读取插件 Vue SFC 文件
        // 2. 使用 @vue/compiler-sfc 编译为 ES Module
        //    - <template> → render function
        //    - <script setup> → 逻辑代码
        //    - <style> → scoped CSS（自动添加 scoped 属性）
        // 3. 打包为自执行函数，接收 PluginContext 作为参数
        // 4. 返回编译后的 JS 字符串

        _logService.Info("PluginService", $"Plugin compiled: {plugin.Name}");
        await Task.CompletedTask;
        return plugin.ModuleCode;
    }
}
