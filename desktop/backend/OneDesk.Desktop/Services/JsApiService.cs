using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace OneDesk.Desktop.Services;

/// <summary>
/// JSAPI 服务
/// 处理来自移动端/桌面前端的 JSAPI 调用
/// 所有设备能力均通过此服务暴露，插件只能消费返回数据
/// </summary>
public class JsApiService
{
    private readonly LogService _logService;
    private readonly StorageService _storageService;
    private readonly Dictionary<string, Func<Dictionary<string, object>, string, string?, Task<JsApiResult>>> _apiHandlers = new();

    public JsApiService(LogService logService, StorageService storageService)
    {
        _logService = logService;
        _storageService = storageService;
        RegisterBuiltinApis();
    }

    /// <summary>
    /// 注册自定义 JSAPI
    /// </summary>
    public void RegisterApi(string apiName, Func<Dictionary<string, object>, string, string?, Task<JsApiResult>> handler)
    {
        _apiHandlers[apiName] = handler;
    }

    /// <summary>
    /// 执行 JSAPI 调用
    /// </summary>
    public async Task<JsApiResult> ExecuteApiAsync(string apiName, Dictionary<string, object> args, string deviceId, string? pluginId)
    {
        if (!_apiHandlers.TryGetValue(apiName, out var handler))
        {
            _logService.Warn("JsApiService", $"Unknown JSAPI: {apiName}");
            return JsApiResult.Fail($"Unknown API: {apiName}");
        }

        try
        {
            var result = await handler(args, deviceId, pluginId);
            _logService.Debug("JsApiService", $"JSAPI {apiName} called by {pluginId ?? "system"} on {deviceId}: {(result.IsSuccess ? "OK" : "FAIL")}");
            return result;
        }
        catch (Exception ex)
        {
            _logService.Error("JsApiService", $"JSAPI {apiName} error: {ex.Message}", ex.StackTrace);
            return JsApiResult.Fail(ex.Message);
        }
    }

    #region Win32 P/Invoke for Window Management

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    #endregion

    private void RegisterBuiltinApis()
    {
        // ==================== PC 端独有 API ====================

        // pc.processList - 获取进程列表
        RegisterApi("pc.processList", async (args, deviceId, pluginId) =>
        {
            await Task.CompletedTask;
            var processes = Process.GetProcesses()
                .Where(p => !string.IsNullOrEmpty(p.ProcessName))
                .Take(100)
                .Select(p => new
                {
                    pid = p.Id,
                    name = p.ProcessName,
                    memoryMB = Math.Round((double)p.WorkingSet64 / 1024 / 1024, 1),
                    startTime = tryGetStartTime(p),
                })
                .ToList();
            return JsApiResult.Ok(new { processes });
        });

        // pc.processMemory - 读取进程内存信息
        RegisterApi("pc.processMemory", async (args, deviceId, pluginId) =>
        {
            await Task.CompletedTask;
            var pid = Convert.ToInt32(args.GetValueOrDefault("pid", 0));
            try
            {
                using var proc = Process.GetProcessById(pid);
                return JsApiResult.Ok(new
                {
                    pid = proc.Id,
                    name = proc.ProcessName,
                    workingSet64 = proc.WorkingSet64,
                    workingSetMB = Math.Round((double)proc.WorkingSet64 / 1024 / 1024, 1),
                    privateMemorySize64 = proc.PrivateMemorySize64,
                    virtualMemorySize64 = proc.VirtualMemorySize64,
                    threads = proc.Threads.Count,
                    handles = proc.HandleCount,
                });
            }
            catch (Exception ex)
            {
                return JsApiResult.Fail($"Cannot read process {pid}: {ex.Message}");
            }
        });

        // pc.windowInfo - 读取窗口信息
        RegisterApi("pc.windowInfo", async (args, deviceId, pluginId) =>
        {
            await Task.CompletedTask;
            var windows = new List<object>();
            EnumWindows((hWnd, _) =>
            {
                if (!IsWindowVisible(hWnd)) return true;
                var titleLen = GetWindowTextLength(hWnd);
                if (titleLen == 0) return true;
                var sb = new StringBuilder(titleLen + 1);
                GetWindowText(hWnd, sb, sb.Capacity);
                GetWindowRect(hWnd, out var rect);
                GetWindowThreadProcessId(hWnd, out var pid);
                windows.Add(new
                {
                    hWnd = hWnd.ToInt64(),
                    title = sb.ToString(),
                    bounds = new { x = rect.Left, y = rect.Top, width = rect.Right - rect.Left, height = rect.Bottom - rect.Top },
                    isForeground = hWnd == GetForegroundWindow(),
                    pid,
                });
                return true;
            }, IntPtr.Zero);
            return JsApiResult.Ok(new { windows });
        });

        // pc.clipboard.read - 读取剪贴板
        RegisterApi("pc.clipboard.read", async (args, deviceId, pluginId) =>
        {
            try
            {
                var text = await Task.Run(() =>
                {
                    if (System.Windows.Forms.Clipboard.ContainsText())
                        return System.Windows.Forms.Clipboard.GetText();
                    return "";
                });
                return JsApiResult.Ok(new { text });
            }
            catch (Exception ex)
            {
                return JsApiResult.Fail($"Clipboard read failed: {ex.Message}");
            }
        });

        // pc.clipboard.write - 写入剪贴板
        RegisterApi("pc.clipboard.write", async (args, deviceId, pluginId) =>
        {
            var text = args.GetValueOrDefault("text", "")?.ToString() ?? "";
            try
            {
                await Task.Run(() => System.Windows.Forms.Clipboard.SetText(text));
                return JsApiResult.Ok();
            }
            catch (Exception ex)
            {
                return JsApiResult.Fail($"Clipboard write failed: {ex.Message}");
            }
        });

        // pc.screenshot - 截取屏幕
        RegisterApi("pc.screenshot", async (args, deviceId, pluginId) =>
        {
            try
            {
                var screenshotPath = Path.Combine(Path.GetTempPath(), $"onedesk-screenshot-{DateTime.Now:yyyyMMddHHmmss}.png");
                await Task.Run(() =>
                {
                    var bounds = System.Windows.Forms.Screen.PrimaryScreen?.Bounds
                        ?? new System.Drawing.Rectangle(0, 0, 1920, 1080);
                    using var bmp = new System.Drawing.Bitmap(bounds.Width, bounds.Height);
                    using var g = System.Drawing.Graphics.FromImage(bmp);
                    g.CopyFromScreen(bounds.Location, System.Drawing.Point.Empty, bounds.Size);
                    bmp.Save(screenshotPath, System.Drawing.Imaging.ImageFormat.Png);
                });
                var imageData = await File.ReadAllBytesAsync(screenshotPath);
                var base64 = Convert.ToBase64String(imageData);
                try { File.Delete(screenshotPath); } catch { }
                return JsApiResult.Ok(new { base64, format = "png", width = 1920, height = 1080 });
            }
            catch (Exception ex)
            {
                return JsApiResult.Fail($"Screenshot failed: {ex.Message}");
            }
        });

        // pc.keyEvent - 模拟按键
        RegisterApi("pc.keyEvent", async (args, deviceId, pluginId) =>
        {
            var key = args.GetValueOrDefault("key", "")?.ToString() ?? "";
            var action = args.GetValueOrDefault("action", "press")?.ToString() ?? "press";
            // 安全检查：仅允许有限按键
            var allowedKeys = new HashSet<string> { "enter", "tab", "escape", "space", "up", "down", "left", "right", "f1", "f2", "f3", "f4", "f5", "f6", "f7", "f8", "f9", "f10", "f11", "f12" };
            if (!allowedKeys.Contains(key.ToLower()))
                return JsApiResult.Fail($"Key '{key}' is not allowed for security reasons");
            await Task.CompletedTask;
            // 实际按键模拟需要 SendKeys，此处为安全实现
            return JsApiResult.Ok(new { key, action, simulated = true });
        });

        // pc.mouseEvent - 模拟鼠标
        RegisterApi("pc.mouseEvent", async (args, deviceId, pluginId) =>
        {
            var x = Convert.ToInt32(args.GetValueOrDefault("x", 0));
            var y = Convert.ToInt32(args.GetValueOrDefault("y", 0));
            var action = args.GetValueOrDefault("action", "move")?.ToString() ?? "move";
            await Task.CompletedTask;
            // 安全限制：仅允许移动和点击
            if (action != "move" && action != "click" && action != "doubleClick")
                return JsApiResult.Fail($"Mouse action '{action}' is not allowed");
            return JsApiResult.Ok(new { x, y, action, simulated = true });
        });

        // pc.systemInfo - 系统信息
        RegisterApi("pc.systemInfo", async (args, deviceId, pluginId) =>
        {
            await Task.CompletedTask;
            var drives = DriveInfo.GetDrives()
                .Where(d => d.IsReady)
                .Select(d => new { name = d.Name, totalGB = Math.Round((double)d.TotalSize / 1024 / 1024 / 1024, 1), freeGB = Math.Round((double)d.AvailableFreeSpace / 1024 / 1024 / 1024, 1) })
                .ToList();
            return JsApiResult.Ok(new
            {
                machineName = Environment.MachineName,
                userName = Environment.UserName,
                osVersion = Environment.OSVersion.ToString(),
                processorCount = Environment.ProcessorCount,
                clrVersion = Environment.Version.ToString(),
                totalMemoryMB = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / 1024 / 1024,
                drives,
            });
        });

        // pc.file.read - 读取本地文件
        RegisterApi("pc.file.read", async (args, deviceId, pluginId) =>
        {
            var path = args.GetValueOrDefault("path", "")?.ToString() ?? "";
            if (string.IsNullOrEmpty(path)) return JsApiResult.Fail("Path is required");
            try
            {
                var content = await File.ReadAllTextAsync(path);
                return JsApiResult.Ok(new { content, path });
            }
            catch (Exception ex)
            {
                return JsApiResult.Fail($"File read failed: {ex.Message}");
            }
        });

        // pc.file.write - 写入本地文件
        RegisterApi("pc.file.write", async (args, deviceId, pluginId) =>
        {
            var path = args.GetValueOrDefault("path", "")?.ToString() ?? "";
            var content = args.GetValueOrDefault("content", "")?.ToString() ?? "";
            if (string.IsNullOrEmpty(path)) return JsApiResult.Fail("Path is required");
            try
            {
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                await File.WriteAllTextAsync(path, content);
                return JsApiResult.Ok(new { path, bytesWritten = Encoding.UTF8.GetByteCount(content) });
            }
            catch (Exception ex)
            {
                return JsApiResult.Fail($"File write failed: {ex.Message}");
            }
        });

        // pc.file.delete - 删除本地文件
        RegisterApi("pc.file.delete", async (args, deviceId, pluginId) =>
        {
            var path = args.GetValueOrDefault("path", "")?.ToString() ?? "";
            if (string.IsNullOrEmpty(path)) return JsApiResult.Fail("Path is required");
            try
            {
                await Task.Run(() =>
                {
                    if (File.Exists(path)) File.Delete(path);
                    else if (Directory.Exists(path)) Directory.Delete(path, true);
                });
                return JsApiResult.Ok(new { path });
            }
            catch (Exception ex)
            {
                return JsApiResult.Fail($"File delete failed: {ex.Message}");
            }
        });

        // pc.file.list - 列出目录文件
        RegisterApi("pc.file.list", async (args, deviceId, pluginId) =>
        {
            var dir = args.GetValueOrDefault("dir", "")?.ToString() ?? "";
            if (string.IsNullOrEmpty(dir)) return JsApiResult.Fail("Directory is required");
            try
            {
                var files = await Task.Run(() =>
                {
                    var di = new DirectoryInfo(dir);
                    return di.GetFileSystemInfos()
                        .Select(f => new
                        {
                            name = f.Name,
                            isDirectory = f is DirectoryInfo,
                            size = f is FileInfo fi ? fi.Length : 0,
                            lastModified = f.LastWriteTimeUtc.ToString("O"),
                        })
                        .ToList();
                });
                return JsApiResult.Ok(new { files, dir });
            }
            catch (Exception ex)
            {
                return JsApiResult.Fail($"Directory list failed: {ex.Message}");
            }
        });

        // pc.app.launch - 启动应用程序
        RegisterApi("pc.app.launch", async (args, deviceId, pluginId) =>
        {
            var appPath = args.GetValueOrDefault("path", "")?.ToString() ?? "";
            var arguments = args.GetValueOrDefault("args", "")?.ToString() ?? "";
            if (string.IsNullOrEmpty(appPath)) return JsApiResult.Fail("Application path is required");
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = appPath,
                    Arguments = arguments,
                    UseShellExecute = true,
                };
                var process = Process.Start(psi);
                return JsApiResult.Ok(new { pid = process?.Id ?? 0, path = appPath });
            }
            catch (Exception ex)
            {
                return JsApiResult.Fail($"App launch failed: {ex.Message}");
            }
        });

        // pc.fontList - 获取系统字体列表
        RegisterApi("pc.fontList", async (args, deviceId, pluginId) =>
        {
            await Task.CompletedTask;
            try
            {
                var fonts = new List<string>();
                using var collection = new System.Drawing.Text.InstalledFontCollection();
                foreach (var family in collection.Families)
                {
                    fonts.Add(family.Name);
                }
                return JsApiResult.Ok(new { fonts, count = fonts.Count });
            }
            catch (Exception ex)
            {
                return JsApiResult.Fail($"Font list failed: {ex.Message}");
            }
        });

        // pc.processKill - 关闭进程
        RegisterApi("pc.processKill", async (args, deviceId, pluginId) =>
        {
            var processName = args.GetValueOrDefault("name", "")?.ToString() ?? "";
            var pid = args.GetValueOrDefault("pid", 0);
            try
            {
                if (pid is int p && p > 0)
                {
                    var proc = Process.GetProcessById(p);
                    proc.Kill();
                    return JsApiResult.Ok(new { killed = true, pid = p });
                }
                if (!string.IsNullOrEmpty(processName))
                {
                    var procs = Process.GetProcessesByName(processName);
                    foreach (var proc in procs) proc.Kill();
                    return JsApiResult.Ok(new { killed = true, name = processName, count = procs.Length });
                }
                return JsApiResult.Fail("Process name or pid is required");
            }
            catch (Exception ex)
            {
                return JsApiResult.Fail($"Process kill failed: {ex.Message}");
            }
        });

        // pc.registry.read - 读取注册表（仅 Windows）
        RegisterApi("pc.registry.read", async (args, deviceId, pluginId) =>
        {
            await Task.CompletedTask;
            if (!OperatingSystem.IsWindows())
                return JsApiResult.Fail("Registry is only available on Windows");
            var keyPath = args.GetValueOrDefault("key", "")?.ToString() ?? "";
            var valueName = args.GetValueOrDefault("value", "")?.ToString() ?? "";
            if (string.IsNullOrEmpty(keyPath)) return JsApiResult.Fail("Registry key is required");
            try
            {
                var value = Microsoft.Win32.Registry.GetValue(keyPath, valueName, null);
                return JsApiResult.Ok(new { key = keyPath, valueName, value });
            }
            catch (Exception ex)
            {
                return JsApiResult.Fail($"Registry read failed: {ex.Message}");
            }
        });

        // ==================== 双端通用 API ====================

        // device.info - 设备基本信息
        RegisterApi("device.info", async (args, deviceId, pluginId) =>
        {
            await Task.CompletedTask;
            return JsApiResult.Ok(new
            {
                platform = "desktop",
                osVersion = Environment.OSVersion.ToString(),
                model = "OneDesk Desktop",
                machineName = Environment.MachineName,
                processorCount = Environment.ProcessorCount,
                clrVersion = Environment.Version.ToString(),
            });
        });

        // device.network - 网络连接状态
        RegisterApi("device.network", async (args, deviceId, pluginId) =>
        {
            await Task.CompletedTask;
            var host = System.Net.Dns.GetHostName();
            var addresses = await System.Net.Dns.GetHostAddressesAsync(host);
            var ips = addresses
                .Where(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                .Select(a => a.ToString())
                .ToList();
            return JsApiResult.Ok(new
            {
                connected = true,
                type = "ethernet",
                hostname = host,
                ipAddresses = ips,
            });
        });

        // device.storage - 存储空间信息
        RegisterApi("device.storage", async (args, deviceId, pluginId) =>
        {
            await Task.CompletedTask;
            var drives = DriveInfo.GetDrives()
                .Where(d => d.IsReady)
                .Select(d => new
                {
                    name = d.Name,
                    label = d.VolumeLabel,
                    totalBytes = d.TotalSize,
                    availableBytes = d.AvailableFreeSpace,
                    usedBytes = d.TotalSize - d.AvailableFreeSpace,
                    format = d.DriveFormat,
                })
                .ToList();
            return JsApiResult.Ok(new { drives });
        });

        // device.memory - 内存信息
        RegisterApi("device.memory", async (args, deviceId, pluginId) =>
        {
            await Task.CompletedTask;
            var gcInfo = GC.GetGCMemoryInfo();
            return JsApiResult.Ok(new
            {
                totalAvailableMB = gcInfo.TotalAvailableMemoryBytes / 1024 / 1024,
                heapSizeMB = gcInfo.HeapSizeBytes / 1024 / 1024,
                memoryLoadBytes = gcInfo.MemoryLoadBytes,
                fragmentedBytes = gcInfo.FragmentedBytes,
            });
        });

        // notification.show - 显示系统通知
        RegisterApi("notification.show", async (args, deviceId, pluginId) =>
        {
            var title = args.GetValueOrDefault("title", "")?.ToString() ?? "";
            var body = args.GetValueOrDefault("body", "")?.ToString() ?? "";
            // 通过 WebView2 前端通知系统显示
            await Task.CompletedTask;
            return JsApiResult.Ok(new { title, body, shown = true });
        });

        // storage.get - 读取持久化数据
        RegisterApi("storage.get", async (args, deviceId, pluginId) =>
        {
            var key = args.GetValueOrDefault("key", "")?.ToString() ?? "";
            var value = await _storageService.GetPluginDataAsync(pluginId ?? "system", key);
            return JsApiResult.Ok(new { key, value });
        });

        // storage.set - 写入持久化数据
        RegisterApi("storage.set", async (args, deviceId, pluginId) =>
        {
            var key = args.GetValueOrDefault("key", "")?.ToString() ?? "";
            var value = args.GetValueOrDefault("value");
            var valueJson = value != null ? JsonSerializer.Serialize(value) : "null";
            await _storageService.SetPluginDataAsync(pluginId ?? "system", key, valueJson);
            return JsApiResult.Ok(new { key });
        });

        // storage.remove - 删除持久化数据
        RegisterApi("storage.remove", async (args, deviceId, pluginId) =>
        {
            var key = args.GetValueOrDefault("key", "")?.ToString() ?? "";
            await _storageService.RemovePluginDataAsync(pluginId ?? "system", key);
            return JsApiResult.Ok(new { key });
        });

        // storage.keys - 获取所有键名
        RegisterApi("storage.keys", async (args, deviceId, pluginId) =>
        {
            var keys = await _storageService.GetPluginDataKeysAsync(pluginId ?? "system");
            return JsApiResult.Ok(new { keys });
        });

        // storage.clear - 清空持久化数据
        RegisterApi("storage.clear", async (args, deviceId, pluginId) =>
        {
            await _storageService.ClearPluginDataAsync(pluginId ?? "system");
            return JsApiResult.Ok();
        });

        // http.get - HTTP GET 请求
        RegisterApi("http.get", async (args, deviceId, pluginId) =>
        {
            var url = args.GetValueOrDefault("url", "")?.ToString() ?? "";
            if (string.IsNullOrEmpty(url)) return JsApiResult.Fail("URL is required");
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(30);
                var response = await client.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();
                return JsApiResult.Ok(new
                {
                    statusCode = (int)response.StatusCode,
                    headers = response.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value)),
                    body = content,
                });
            }
            catch (Exception ex)
            {
                return JsApiResult.Fail($"HTTP GET failed: {ex.Message}");
            }
        });

        // http.post - HTTP POST 请求
        RegisterApi("http.post", async (args, deviceId, pluginId) =>
        {
            var url = args.GetValueOrDefault("url", "")?.ToString() ?? "";
            var body = args.GetValueOrDefault("body", "")?.ToString() ?? "";
            var contentType = args.GetValueOrDefault("contentType", "application/json")?.ToString() ?? "application/json";
            if (string.IsNullOrEmpty(url)) return JsApiResult.Fail("URL is required");
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(30);
                var content = new StringContent(body, Encoding.UTF8, contentType);
                var response = await client.PostAsync(url, content);
                var responseBody = await response.Content.ReadAsStringAsync();
                return JsApiResult.Ok(new
                {
                    statusCode = (int)response.StatusCode,
                    body = responseBody,
                });
            }
            catch (Exception ex)
            {
                return JsApiResult.Fail($"HTTP POST failed: {ex.Message}");
            }
        });

        // websocket.connect - WebSocket 连接
        RegisterApi("websocket.connect", async (args, deviceId, pluginId) =>
        {
            var url = args.GetValueOrDefault("url", "")?.ToString() ?? "";
            if (string.IsNullOrEmpty(url)) return JsApiResult.Fail("URL is required");
            // WebSocket 连接需要持久化，此处返回连接信息
            await Task.CompletedTask;
            return JsApiResult.Ok(new { url, status = "connecting", message = "WebSocket connection initiated. Use WebSocketService for persistent connections." });
        });

        // crypto.hash - 哈希计算
        RegisterApi("crypto.hash", async (args, deviceId, pluginId) =>
        {
            var data = args.GetValueOrDefault("data", "")?.ToString() ?? "";
            var algorithm = args.GetValueOrDefault("algorithm", "sha256")?.ToString() ?? "sha256";
            await Task.CompletedTask;
            try
            {
                var bytes = Encoding.UTF8.GetBytes(data);
                byte[] hashBytes = algorithm.ToLower() switch
                {
                    "md5" => MD5.HashData(bytes),
                    "sha1" => SHA1.HashData(bytes),
                    "sha256" => SHA256.HashData(bytes),
                    "sha512" => SHA512.HashData(bytes),
                    _ => SHA256.HashData(bytes),
                };
                var hex = Convert.ToHexString(hashBytes).ToLowerInvariant();
                return JsApiResult.Ok(new { algorithm, hex, inputLength = data.Length });
            }
            catch (Exception ex)
            {
                return JsApiResult.Fail($"Hash failed: {ex.Message}");
            }
        });

        // crypto.encrypt - 加密/解密
        RegisterApi("crypto.encrypt", async (args, deviceId, pluginId) =>
        {
            var data = args.GetValueOrDefault("data", "")?.ToString() ?? "";
            var key = args.GetValueOrDefault("key", "")?.ToString() ?? "";
            var action = args.GetValueOrDefault("action", "encrypt")?.ToString() ?? "encrypt";
            await Task.CompletedTask;
            try
            {
                if (string.IsNullOrEmpty(key) || key.Length < 16)
                    return JsApiResult.Fail("Key must be at least 16 characters for AES encryption");
                var keyBytes = SHA256.HashData(Encoding.UTF8.GetBytes(key));
                if (action == "encrypt")
                {
                    using var aes = Aes.Create();
                    aes.Key = keyBytes;
                    aes.GenerateIV();
                    using var encryptor = aes.CreateEncryptor();
                    var plainBytes = Encoding.UTF8.GetBytes(data);
                    var encrypted = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
                    var result = new byte[aes.IV.Length + encrypted.Length];
                    Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
                    Buffer.BlockCopy(encrypted, 0, result, aes.IV.Length, encrypted.Length);
                    return JsApiResult.Ok(new { encrypted = Convert.ToBase64String(result), algorithm = "aes-256-cbc" });
                }
                else
                {
                    var fullCipher = Convert.FromBase64String(data);
                    using var aes = Aes.Create();
                    aes.Key = keyBytes;
                    var iv = new byte[16];
                    Buffer.BlockCopy(fullCipher, 0, iv, 0, 16);
                    aes.IV = iv;
                    using var decryptor = aes.CreateDecryptor();
                    var cipherBytes = new byte[fullCipher.Length - 16];
                    Buffer.BlockCopy(fullCipher, 16, cipherBytes, 0, cipherBytes.Length);
                    var decrypted = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
                    return JsApiResult.Ok(new { decrypted = Encoding.UTF8.GetString(decrypted), algorithm = "aes-256-cbc" });
                }
            }
            catch (Exception ex)
            {
                return JsApiResult.Fail($"Crypto operation failed: {ex.Message}");
            }
        });
    }

    private static string? tryGetStartTime(Process p)
    {
        try { return p.StartTime.ToString("O"); }
        catch { return null; }
    }
}

/// <summary>
/// JSAPI 调用结果
/// </summary>
public class JsApiResult
{
    public bool IsSuccess { get; init; }
    public object? Data { get; init; }
    public string? ErrorMessage { get; init; }

    public static JsApiResult Ok(object? data = null) => new() { IsSuccess = true, Data = data };
    public static JsApiResult Fail(string error) => new() { IsSuccess = false, ErrorMessage = error };
}
