/**
 * OneDeck 插件开发 SDK
 * 插件通过此 SDK 与系统交互，所有能力均通过 JSAPI 获取
 */

// ==================== 插件元数据 ====================

export interface PluginManifest {
  id: string;
  name: string;
  version: string;
  description: string;
  author: string;
  icon: string;
  permissions: PluginPermission[];
  minPlatformVersion: string;
  entry: string;
  settings?: PluginSetting[];
}

export interface PluginPermission {
  name: string;
  reason: string;
  required: boolean;
}

export interface PluginSetting {
  key: string;
  label: string;
  type: 'string' | 'number' | 'boolean' | 'select' | 'color';
  defaultValue: unknown;
  options?: Array<{ label: string; value: unknown }>;
}

// ==================== 插件生命周期 ====================

export interface PluginLifecycle {
  onMount(ctx: PluginContext): Promise<void> | void;
  onUnmount(): Promise<void> | void;
  onStateUpdate(state: Record<string, unknown>): void;
  onCommand(command: string, data?: unknown): void;
}

// ==================== 插件上下文 ====================

export interface PluginContext {
  pluginId: string;
  instanceId: string;

  /** 调用 JSAPI */
  callApi(apiName: string, args?: Record<string, unknown>): Promise<unknown>;

  /** 获取插件存储数据 */
  storage: PluginStorage;

  /** 获取插件配置 */
  getConfig(): Record<string, unknown>;

  /** 发送事件到桌面端 */
  emitEvent(eventType: string, data?: Record<string, unknown>): void;

  /** 更新自身状态（会同步到桌面端） */
  setState(state: Record<string, unknown>): void;

  /** 记录日志 */
  log: PluginLogger;
}

// ==================== 插件存储 ====================

export interface PluginStorage {
  get(key: string): Promise<unknown>;
  set(key: string, value: unknown): Promise<void>;
  remove(key: string): Promise<void>;
  keys(): Promise<string[]>;
  clear(): Promise<void>;

  /** 临时存储（应用生命周期内有效，重启清空） */
  temp: {
    get(key: string): unknown;
    set(key: string, value: unknown): void;
    remove(key: string): void;
    clear(): void;
  };
}

// ==================== 插件日志 ====================

export interface PluginLogger {
  debug(message: string, ...args: unknown[]): void;
  info(message: string, ...args: unknown[]): void;
  warn(message: string, ...args: unknown[]): void;
  error(message: string, ...args: unknown[]): void;
}

// ==================== JSAPI 接口定义 ====================

export interface JsApiRegistry {
  /** 文件操作 */
  file: {
    read(path: string): Promise<string>;
    write(path: string, content: string): Promise<void>;
    delete(path: string): Promise<void>;
    exists(path: string): Promise<boolean>;
    list(dir: string): Promise<string[]>;
  };

  /** 摄像头 */
  camera: {
    takePhoto(): Promise<string>; // base64
    startStream(): Promise<string>; // streamId
    stopStream(streamId: string): Promise<void>;
  };

  /** 定位 */
  location: {
    getCurrent(): Promise<{ lat: number; lng: number; accuracy: number }>;
    startWatch(): Promise<string>; // watchId
    stopWatch(watchId: string): Promise<void>;
  };

  /** 网络 */
  network: {
    getStatus(): Promise<{ connected: boolean; type: string; ssid?: string }>;
  };

  /** 设备信息 */
  device: {
    getInfo(): Promise<DeviceInfo>;
    getBattery(): Promise<{ level: number; charging: boolean }>;
    vibrate(duration: number): Promise<void>;
  };

  /** 剪贴板 */
  clipboard: {
    read(): Promise<string>;
    write(text: string): Promise<void>;
  };

  /** 通知 */
  notification: {
    show(title: string, body: string, data?: Record<string, unknown>): Promise<void>;
  };

  /** 内存读取（仅桌面端可用） */
  memory: {
    readProcessMemory(pid: number, offset: number, size: number): Promise<number[]>;
    getProcessList(): Promise<Array<{ pid: number; name: string }>>;
    readWindowInfo(pid: number): Promise<WindowInfo>;
  };

  /** 权限 */
  permission: {
    request(permission: string): Promise<boolean>;
    check(permission: string): Promise<boolean>;
  };
}

export interface DeviceInfo {
  platform: string;
  osVersion: string;
  model: string;
  screenResolution: { width: number; height: number };
  batteryLevel: number;
  networkType: string;
}

export interface WindowInfo {
  title: string;
  bounds: { x: number; y: number; width: number; height: number };
  isForeground: boolean;
}

// ==================== 插件定义辅助 ====================

export function definePlugin(manifest: PluginManifest, lifecycle: PluginLifecycle): {
  manifest: PluginManifest;
  lifecycle: PluginLifecycle;
} {
  return { manifest, lifecycle };
}
