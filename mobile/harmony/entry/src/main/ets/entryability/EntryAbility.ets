/**
 * OneDeck Mobile - HarmonyOS (ArkTS)
 * 主入口 Ability
 */
import UIAbility from '@ohos.app.ability.UIAbility'
import window from '@ohos.window'

export default class EntryAbility extends UIAbility {
  onCreate(want, launchParam) {
    console.info('[OneDeck] EntryAbility onCreate')
  }

  onWindowStageCreate(windowStage: window.WindowStage) {
    console.info('[OneDeck] EntryAbility onWindowStageCreate')

    // 设置窗口全屏
    windowStage.getMainWindow().then((win) => {
      win.setWindowLayoutFullScreen(true)
    })

    // 加载前端页面
    windowStage.loadContent('pages/Index', (err, data) => {
      if (err.code) {
        console.error('[OneDeck] Failed to load content:', JSON.stringify(err))
        return
      }
      console.info('[OneDeck] Content loaded successfully')
    })
  }

  onDestroy() {
    console.info('[OneDeck] EntryAbility onDestroy')
  }
}
