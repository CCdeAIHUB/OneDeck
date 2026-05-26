# OneDesk

跨平台流控制软件系统，类似 Stream Deck。系统由"桌面端"和"移动端"组成，桌面端作为控制中心与能力底座，移动端作为展示与触发终端。

## 架构概览

```
┌─────────────────────────────────────────────────┐
│                   桌面端 (Desktop)                │
│  ┌──────────────┐  ┌──────────────────────────┐  │
│  │  C# 后端      │  │  Vue 3 前端              │  │
│  │  - WebSocket  │◄─┤  - 仪表盘               │  │
│  │  - JSAPI      │  │  - 设备管理             │  │
│  │  - 插件管理    │  │  - 方案编辑器(可视化/代码)│  │
│  │  - 日志系统    │  │  - 插件管理             │  │
│  │  - 存储服务    │  │  - 日志查看             │  │
│  │  - 内存读取    │  │  - 设置                 │  │
│  └──────┬────────┘  └──────────────────────────┘  │
│         │ WebSocket Server (port 9720)            │
└─────────┼─────────────────────────────────────────┘
          │
    ══════╪══════ WebSocket (双向通讯)
          │
┌─────────┼─────────────────────────────────────────┐
│         │              移动端 (Mobile)              │
│  ┌──────▼────────┐  ┌──────────────────────────┐  │
│  │  原生壳子      │  │  Vue 3 前端              │  │
│  │  Android(Kotlin)│  │  - 连接页               │  │
│  │  iOS(Swift)    │◄─┤  - 方案渲染             │  │
│  │  Harmony(ArkTS)│  │  - 运行时/沙箱          │  │
│  │  - JSAPI Bridge│  │  - 模块加载器           │  │
│  │  - 权限管理     │  │  - 样式管理器           │  │
│  │  - 日志缓冲     │  │  - 断连遮罩             │  │
│  └───────────────┘  └──────────────────────────┘  │
└───────────────────────────────────────────────────┘
```

## 通讯机制

**前端零网络原则**：所有前端（桌面端和移动端 Vue 应用及插件）严禁直接处理任何网络通讯，所有网络请求由后端（原生壳子）处理。

**双向通讯链路**：
- **移动端→桌面端**：`JSAPI调用 → 原生壳子 → WebSocket → 桌面端C#`
- **桌面端→移动端**：`桌面端前端 → 桌面端C# → WebSocket → 移动端原生壳子 → 移动端前端`

## 核心特性

| 特性 | 说明 |
|------|------|
| 插件系统 | 完整JS模块双端运行，插件代码随方案下发到移动端 |
| 方案编辑 | 混合模式：可视化拖拽编辑器 + 代码模式 |
| 状态同步 | 桌面端驱动：移动端交互事件转发桌面端处理 |
| CSS隔离 | Vue Scoped CSS，自动添加 scoped 属性 |
| 断连处理 | 界面冻结 + 断连提示，等待重连 |
| 日志补传 | 移动端原生层环形缓冲区(1000条)，重连后批量补传 |
| 重连策略 | 指数退避：1s→2s→4s→8s→16s→最大30s |
| 图标规范 | Solar图标库：bold=选中，bold-duotone=Hover |

## 项目结构

```
onedeck/
├── shared/                        # 共享层
│   ├── protocol/                  # WebSocket 通讯协议
│   │   ├── src/messages.ts        # 消息定义
│   │   └── package.json
│   └── plugin-sdk/                # 插件开发 SDK
│       ├── src/index.ts           # SDK 接口定义
│       └── package.json
│
├── desktop/                       # 桌面端
│   ├── backend/                   # C# .NET 后端
│   │   └── OneDesk.Desktop/
│   │       ├── Program.cs         # 入口
│   │       ├── AppHost.cs         # 主机
│   │       ├── Models/            # 数据模型
│   │       └── Services/          # 核心服务
│   │           ├── WebSocketService.cs   # WebSocket 管理
│   │           ├── JsApiService.cs       # JSAPI 注册与分发
│   │           ├── PluginService.cs      # 插件管理
│   │           ├── LogService.cs         # 分级日志
│   │           ├── StorageService.cs     # SQLite 存储
│   │           └── SchemeService.cs      # 方案管理
│   └── frontend/                  # Vue 3 前端
│       ├── src/
│       │   ├── router/            # 路由
│       │   ├── stores/            # Pinia 状态管理
│       │   │   ├── connection.ts  # WebSocket 连接
│       │   │   ├── devices.ts     # 设备管理
│       │   │   ├── schemes.ts     # 方案管理
│       │   │   ├── plugins.ts     # 插件管理
│       │   │   └── logs.ts        # 日志管理
│       │   ├── views/             # 页面
│       │   │   ├── DashboardView.vue     # 仪表盘
│       │   │   ├── DevicesView.vue       # 设备列表
│       │   │   ├── DeviceDetailView.vue  # 设备详情
│       │   │   ├── SchemesView.vue       # 方案列表
│       │   │   ├── SchemeEditorView.vue  # 方案编辑器
│       │   │   ├── PluginsView.vue       # 插件管理
│       │   │   ├── LogsView.vue         # 日志查看
│       │   │   └── SettingsView.vue      # 设置
│       │   └── components/        # 通用组件
│       │       ├── TitleBar.vue   # 自绘标题栏
│       │       ├── Sidebar.vue    # 侧边导航
│       │       ├── PageHeader.vue # 页面头部
│       │       └── DeviceCard.vue # 设备卡片
│       └── package.json
│
└── mobile/                        # 移动端
    ├── frontend/                  # Vue 3 前端（独立项目）
    │   ├── src/
    │   │   ├── router/            # 路由
    │   │   ├── stores/            # Pinia 状态管理
    │   │   │   ├── connection.ts  # 连接管理 + 日志缓冲
    │   │   │   └── scheme.ts      # 方案 + 模块管理
    │   │   ├── runtime/           # 运行时核心
    │   │   │   ├── PluginSandbox.ts   # 插件沙箱上下文
    │   │   │   ├── ModuleLoader.ts     # 模块加载器
    │   │   │   └── StyleManager.ts     # 样式管理器
    │   │   ├── views/             # 页面
    │   │   │   ├── ConnectView.vue     # 连接页
    │   │   │   ├── SchemeView.vue      # 方案渲染
    │   │   │   └── MobileSettingsView.vue
    │   │   └── components/
    │   │       └── DisconnectOverlay.vue  # 断连遮罩
    │   └── package.json
    │
    ├── android/                   # Android (Kotlin)
    │   └── app/src/main/java/ai/onedesk/mobile/
    │       ├── MainActivity.kt         # 主 Activity
    │       ├── OneDeskApplication.kt    # Application
    │       ├── OneDeskJsBridge.kt       # JSAPI Bridge
    │       ├── WebSocketClient.kt       # WebSocket 客户端
    │       └── LogBuffer.kt            # 日志环形缓冲区
    │
    ├── ios/                       # iOS (Swift)
    │   └── OneDeskMobile/
    │       ├── OneDeskMobileApp.swift   # App 入口
    │       ├── ContentView.swift        # WebView 容器
    │       └── OneDeskJsBridge.swift    # JSAPI Bridge
    │
    └── harmony/                   # HarmonyOS (ArkTS)
        └── entry/src/main/ets/
            ├── entryability/EntryAbility.ets   # Ability
            └── pages/Index.ets                  # 主页面
```

## 技术栈

| 组件 | 技术 |
|------|------|
| 桌面端后端 | C# .NET 9 (跨平台: Win/Mac/Linux, arm64/amd64) |
| 桌面端前端 | Vue 3 + Vite + Pinia + Vue Router + TailWindCSS v4 |
| 移动端前端 | Vue 3 + Vite + Pinia + Vue Router + TailWindCSS v4 |
| Android | Kotlin |
| iOS | Swift |
| HarmonyOS | ArkTS |
| 图标 | YesIcon / Solar 图标库 |
| 代码编辑器 | CodeMirror 6 (MIT) |
| 数据库 | SQLite (双端) |
| 通讯 | WebSocket |

## 开源组件声明

本项目使用以下 MIT 协议开源组件：

| 组件 | 版本 | 协议 | 用途 |
|------|------|------|------|
| [CodeMirror](https://codemirror.net/) | 6.x | MIT | 代码编辑器（语法高亮、代码折叠） |

## 开发指南

### 桌面端

```bash
# 后端
cd desktop/backend/OneDesk.Desktop
dotnet run -- --ws-port 9720

# 前端
cd desktop/frontend
npm install
npm run dev
```

### 移动端前端

```bash
cd mobile/frontend
npm install
npm run dev
```

### 插件开发

插件通过 `@onedesk/plugin-sdk` 开发：

```typescript
import { definePlugin } from '@onedesk/plugin-sdk'

export default definePlugin(
  {
    id: 'my-plugin',
    name: 'My Plugin',
    version: '1.0.0',
    description: 'A sample plugin',
    author: 'Developer',
    icon: 'solar:star-bold',
    permissions: [],
    minPlatformVersion: '0.1.0',
    entry: './index.vue',
  },
  {
    async onMount(ctx) {
      ctx.log.info('Plugin mounted!')

      // 调用 JSAPI
      const deviceInfo = await ctx.callApi('device.getInfo')

      // 使用存储
      await ctx.storage.set('key', 'value')

      // 读取配置
      const config = ctx.getConfig()
    },

    onUnmount() {
      console.log('Plugin unmounted')
    },

    onStateUpdate(state) {
      // 接收桌面端同步的状态
    },

    onCommand(command, data) {
      // 接收桌面端下发的命令
    },
  }
)
```

## 安全机制

1. **前端零网络**：前端严禁直接处理网络通讯
2. **插件沙箱**：插件运行在独立沙箱中，互不干扰
3. **数据隔离**：插件通过 `plugin_id` 进行 SQLite 数据隔离
4. **JSAPI 只读**：内存读取功能仅返回数据，插件不能控制底层逻辑
5. **无存储能力**：插件无法自主存储内容，必须通过 JSAPI
6. **macOS 限制**：内存读取在 macOS 上不可用（SIP保护），返回平台不支持错误

## License

Private - All Rights Reserved
