/**
 * OneDeck Mobile - HarmonyOS 主页面
 * 使用 Web 组件加载前端 Vue 应用
 */
@Entry
@Component
struct Index {
  @State isConnected: boolean = false
  @State desktopUrl: string = 'ws://192.168.1.100:9720/mobile'
  private webController: WebController = new WebController()

  build() {
    Column() {
      // WebView 加载前端
      Web({ src: $rawfile('frontend/index.html'), controller: this.webController })
        .width('100%')
        .height('100%')
        .javaScriptAccess(true)
        .domStorageAccess(true)
        .fileAccess(true)
        .onPageEnd(() => {
          // 注入 JSAPI Bridge
          this.injectJsBridge()
        })
    }
    .width('100%')
    .height('100%')
  }

  /**
   * 注入 JSAPI Bridge
   * 将原生能力暴露给前端
   */
  private injectJsBridge() {
    // TODO: 使用 webController.runJavaScript() 注入 JSAPI
    this.webController.runJavaScript(
      `window.OneDeckNative = {
        callApi: function(apiName, args) {
          // 通过 Web 组件的 javaScriptProxy 调用原生方法
        },
        getDeviceId: function() { return 'harmony-device'; },
        getPlatform: function() { return 'harmony'; },
        requestPermission: function(permission) { return true; },
        log: function(level, message) { console.log('[' + level + '] ' + message); }
      };`
    )
  }
}
