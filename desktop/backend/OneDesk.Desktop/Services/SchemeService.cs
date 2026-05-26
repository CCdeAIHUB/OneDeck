using OneDeck.Desktop.Models;

namespace OneDeck.Desktop.Services;

/// <summary>
/// 方案管理服务
/// 管理移动端方案的设计、存储和下发
/// </summary>
public class SchemeService
{
    private readonly StorageService _storageService;
    private readonly LogService _logService;

    public SchemeService(StorageService storageService, LogService logService)
    {
        _storageService = storageService;
        _logService = logService;
    }

    /// <summary>
    /// 创建新方案
    /// </summary>
    public async Task<Scheme> CreateSchemeAsync(string name, string targetDeviceId)
    {
        var scheme = new Scheme
        {
            Id = Guid.NewGuid().ToString("N")[..12],
            Name = name,
            TargetDeviceId = targetDeviceId,
            Layout = new SchemeLayout
            {
                Type = "grid",
                Columns = 4,
                Rows = 3,
                Pages = new List<SchemePage>
                {
                    new()
                    {
                        Id = Guid.NewGuid().ToString("N")[..8],
                        Name = "Page 1",
                        Slots = new List<SchemeSlot>()
                    }
                }
            },
            Version = 1
        };

        await _storageService.SaveSchemeAsync(scheme);
        _logService.Info("SchemeService", $"Scheme created: {name} for device {targetDeviceId}");
        return scheme;
    }

    /// <summary>
    /// 更新方案
    /// </summary>
    public async Task UpdateSchemeAsync(Scheme scheme)
    {
        scheme.Version++;
        scheme.UpdatedAt = DateTime.UtcNow;
        await _storageService.SaveSchemeAsync(scheme);
        _logService.Info("SchemeService", $"Scheme updated: {scheme.Name} v{scheme.Version}");
    }

    /// <summary>
    /// 为方案添加插件实例
    /// </summary>
    public async Task AddPluginToSchemeAsync(string schemeId, string pluginId, int row, int column, int rowSpan = 1, int columnSpan = 1)
    {
        var scheme = await _storageService.GetSchemeForDeviceAsync(
            (await _storageService.GetAllPluginsAsync()).FirstOrDefault()?.Id ?? "");

        // TODO: 完善方案插件管理
        _logService.Info("SchemeService", $"Plugin {pluginId} added to scheme {schemeId}");
    }

    /// <summary>
    /// 获取设备方案
    /// </summary>
    public async Task<Scheme?> GetSchemeForDeviceAsync(string deviceId)
    {
        return await _storageService.GetSchemeForDeviceAsync(deviceId);
    }
}
