/**
 * OneDeck WebSocket 通讯协议 - 消息定义
 * 双向通讯链路：
 *   移动端→桌面端：JSAPI调用、事件上报、日志传输
 *   桌面端→移动端：代码下发、状态同步、指令下发
 */

// ==================== 消息基础结构 ====================

export enum MessageType {
  // 移动端 → 桌面端
  JSAPI_CALL = 'jsapi_call',
  JSAPI_CALL_RESULT = 'jsapi_call_result',
  EVENT_REPORT = 'event_report',
  LOG_TRANSFER = 'log_transfer',
  DEVICE_HEARTBEAT = 'device_heartbeat',
  DEVICE_REGISTER = 'device_register',
  PERMISSION_REQUEST = 'permission_request',
  LOG_BATCH_TRANSFER = 'log_batch_transfer', // 断连补传日志

  // 桌面端 → 移动端
  SCHEME_PUSH = 'scheme_push',
  STATE_SYNC = 'state_sync',
  JSAPI_RESPONSE = 'jsapi_response',
  COMMAND = 'command',
  PERMISSION_RESULT = 'permission_result',
  MODULE_PUSH = 'module_push',       // 插件完整JS模块下发
  STYLE_PUSH = 'style_push',         // CSS样式下发
}

export interface BaseMessage {
  id: string;
  type: MessageType;
  timestamp: number;
  deviceId: string;
  pluginId?: string;
}

// ==================== 设备管理消息 ====================

export interface DeviceRegisterMessage extends BaseMessage {
  type: MessageType.DEVICE_REGISTER;
  payload: {
    deviceName: string;
    platform: 'android' | 'ios' | 'harmony';
    screenResolution: { width: number; height: number };
    osVersion: string;
    appId: string;
  };
}

export interface DeviceHeartbeatMessage extends BaseMessage {
  type: MessageType.DEVICE_HEARTBEAT;
  payload: {
    batteryLevel: number;
    networkType: 'wifi' | 'cellular' | 'none';
  };
}

// ==================== JSAPI 消息 ====================

export interface JsApiCallMessage extends BaseMessage {
  type: MessageType.JSAPI_CALL;
  payload: {
    apiName: string;
    args: Record<string, unknown>;
    callId: string;
  };
}

export interface JsApiResponseMessage extends BaseMessage {
  type: MessageType.JSAPI_RESPONSE;
  payload: {
    callId: string;
    success: boolean;
    result?: unknown;
    error?: string;
  };
}

// ==================== 方案与模块下发 ====================

export interface SchemePushMessage extends BaseMessage {
  type: MessageType.SCHEME_PUSH;
  payload: {
    schemeId: string;
    schemeName: string;
    layout: SchemeLayout;
    plugins: SchemePlugin[];
    version: number;
  };
}

export interface SchemeLayout {
  type: 'grid' | 'free' | 'tabs';
  columns: number;
  rows: number;
  pages: SchemePage[];
}

export interface SchemePage {
  id: string;
  name: string;
  slots: SchemeSlot[];
}

export interface SchemeSlot {
  id: string;
  pluginId: string;
  row: number;
  column: number;
  rowSpan: number;
  columnSpan: number;
}

export interface SchemePlugin {
  pluginId: string;
  instanceId: string;
  config: Record<string, unknown>;
}

export interface ModulePushMessage extends BaseMessage {
  type: MessageType.MODULE_PUSH;
  payload: {
    pluginId: string;
    instanceId: string;
    moduleCode: string;   // 编译后的完整JS模块（含逻辑+渲染函数）
    moduleId: string;
    version: string;
  };
}

export interface StylePushMessage extends BaseMessage {
  type: MessageType.STYLE_PUSH;
  payload: {
    pluginId: string;
    css: string;         // Scoped CSS
    styleId: string;
  };
}

// ==================== 状态同步 ====================

export interface StateSyncMessage extends BaseMessage {
  type: MessageType.STATE_SYNC;
  payload: {
    pluginId: string;
    instanceId: string;
    state: Record<string, unknown>;
    source: 'desktop' | 'mobile';
  };
}

// ==================== 事件上报 ====================

export interface EventReportMessage extends BaseMessage {
  type: MessageType.EVENT_REPORT;
  payload: {
    pluginId: string;
    instanceId: string;
    eventType: string;
    eventData: Record<string, unknown>;
  };
}

// ==================== 日志消息 ====================

export enum LogLevel {
  DEBUG = 'debug',
  INFO = 'info',
  WARN = 'warn',
  ERROR = 'error',
}

export interface LogTransferMessage extends BaseMessage {
  type: MessageType.LOG_TRANSFER;
  payload: {
    level: LogLevel;
    message: string;
    source: string;
    stackTrace?: string;
  };
}

export interface LogBatchTransferMessage extends BaseMessage {
  type: MessageType.LOG_BATCH_TRANSFER;
  payload: {
    logs: Array<{
      level: LogLevel;
      message: string;
      source: string;
      timestamp: number;
      stackTrace?: string;
    }>;
  };
}

// ==================== 权限消息 ====================

export interface PermissionRequestMessage extends BaseMessage {
  type: MessageType.PERMISSION_REQUEST;
  payload: {
    permission: string;
    reason: string;
  };
}

export interface PermissionResultMessage extends BaseMessage {
  type: MessageType.PERMISSION_RESULT;
  payload: {
    permission: string;
    granted: boolean;
  };
}

// ==================== 指令消息 ====================

export enum CommandType {
  FREEZE = 'freeze',
  UNFREEZE = 'unfreeze',
  RELOAD_SCHEME = 'reload_scheme',
  UPDATE_PLUGIN = 'update_plugin',
}

export interface CommandMessage extends BaseMessage {
  type: MessageType.COMMAND;
  payload: {
    command: CommandType;
    data?: Record<string, unknown>;
  };
}

// ==================== 消息类型联合 ====================

export type OneDeckMessage =
  | DeviceRegisterMessage
  | DeviceHeartbeatMessage
  | JsApiCallMessage
  | JsApiResponseMessage
  | SchemePushMessage
  | ModulePushMessage
  | StylePushMessage
  | StateSyncMessage
  | EventReportMessage
  | LogTransferMessage
  | LogBatchTransferMessage
  | PermissionRequestMessage
  | PermissionResultMessage
  | CommandMessage;
