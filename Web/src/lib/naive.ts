import type { DialogApi, LoadingBarApi, MessageApi, NotificationApi } from 'naive-ui'
import { createDiscreteApi } from 'naive-ui'

let message: MessageApi | null = null
let dialog: DialogApi | null = null
let notification: NotificationApi | null = null
let loadingBar: LoadingBarApi | null = null

export function setupNaiveDiscreteApi() {
  if (message && dialog && notification && loadingBar) {
    return
  }

  const discrete = createDiscreteApi(['message', 'dialog', 'notification', 'loadingBar'])
  message = discrete.message
  dialog = discrete.dialog
  notification = discrete.notification
  loadingBar = discrete.loadingBar
}

export function useDiscrete() {
  if (!message || !dialog || !notification || !loadingBar) {
    setupNaiveDiscreteApi()
  }

  return {
    message: message!,
    dialog: dialog!,
    notification: notification!,
    loadingBar: loadingBar!,
  }
}
