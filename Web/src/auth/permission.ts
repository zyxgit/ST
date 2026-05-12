import type { App, DirectiveBinding } from 'vue'

import { useAuthStore } from '@/stores/auth'

function togglePermission(el: HTMLElement, binding: DirectiveBinding<string | string[]>) {
  const authStore = useAuthStore()
  const values = Array.isArray(binding.value) ? binding.value : [binding.value]
  const hasPermission = values.some((item) => authStore.hasPermission(item))

  el.style.display = hasPermission ? '' : 'none'
}

export function setupPermissionDirective(app: App) {
  app.directive('permission', {
    mounted(el, binding) {
      togglePermission(el as HTMLElement, binding)
    },
    updated(el, binding) {
      togglePermission(el as HTMLElement, binding)
    },
  })
}
