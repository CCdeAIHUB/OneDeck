import { createApp } from 'vue'
import { createPinia } from 'pinia'
import router from './router'
import App from './App.vue'
import './assets/main.css'
import { useConnectionStore } from './stores/connection'
import { useSchemeStore } from './stores/scheme'

const app = createApp(App)
const pinia = createPinia()
app.use(pinia)
app.use(router)
app.mount('#app')

// 初始化连接
const connectionStore = useConnectionStore()
connectionStore.connect()

// 全局错误边界 - 捕获渲染函数错误并上报
app.config.errorHandler = (err, instance, info) => {
  console.error('[OneDeck Runtime Error]', err, info)
  connectionStore.send({
    type: 'log_transfer',
    deviceId: connectionStore.deviceId,
    payload: {
      level: 'error',
      message: `Runtime error: ${err}`,
      source: 'VueRuntime',
      stackTrace: err instanceof Error ? err.stack : undefined,
    },
  })
}
