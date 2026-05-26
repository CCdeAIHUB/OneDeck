/// <reference types="vite/client" />

declare module '*.vue' {
  import type { DefineComponent } from 'vue'
  const component: DefineComponent<{}, {}, any>
  export default component
}

/**
 * 移动端原生壳子 JSAPI 接口
 * 由原生层注入到 WebView 的 window 对象
 */
interface Window {
  OneDeckNative?: {
    callApi(apiName: string, args: string): Promise<string>
    getDeviceId(): string
    getPlatform(): string
    requestPermission(permission: string): Promise<boolean>
    log(level: string, message: string): void
  }
}
