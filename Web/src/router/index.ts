import { createRouter, createWebHistory } from 'vue-router'
import NProgress from 'nprogress'

import { useAuthStore } from '@/stores/auth'

import { adminRoutes, publicRoutes } from './routes'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [...adminRoutes, ...publicRoutes],
})

let bootstrapping = false

router.beforeEach(async (to) => {
  NProgress.start()
  document.title = `ST Admin | ${String(to.meta.title ?? '管理后台')}`

  const authStore = useAuthStore()

  if (!authStore.initialized && !bootstrapping) {
    bootstrapping = true

    try {
      await authStore.bootstrap()
      authStore.setBootstrapFailed(false)
    } catch {
      authStore.setBootstrapFailed(true)
      authStore.initialized = true
    } finally {
      bootstrapping = false
    }
  }

  if (to.meta.public) {
    return true
  }

  if (!authStore.isAuthenticated) {
    return {
      path: '/login',
      query: {
        redirect: to.fullPath,
      },
    }
  }

  const requiredPermission = typeof to.meta.permission === 'string' ? to.meta.permission : undefined
  if (!authStore.bootstrapFailed && requiredPermission && !authStore.hasPermission(requiredPermission)) {
    return '/dashboard'
  }

  return true
})

router.afterEach(() => {
  NProgress.done()
})

export default router
