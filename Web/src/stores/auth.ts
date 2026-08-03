import { defineStore } from 'pinia'
import { computed, ref } from 'vue'

import { getCurrentUser, login as loginApi, logout as logoutApi } from '@/api/auth'
import { getCurrentUserMenuTree } from '@/api/menu'
import { clearTokens, getAccessToken, getRefreshToken, setTokens } from '@/auth/token'
import router from '@/router'
import type { CurrentUser, LoginCommand } from '@/types/auth'
import type { MenuTreeNode } from '@/types/menu'

export const useAuthStore = defineStore('auth', () => {
  const accessToken = ref(getAccessToken())
  const currentUser = ref<CurrentUser>({
    isAuthenticated: false,
    roles: [],
    permissions: [],
  })
  const menuTree = ref<MenuTreeNode[]>([])
  const initialized = ref(false)
  const bootstrapFailed = ref(false)

  const isAuthenticated = computed(() => Boolean(accessToken.value))

  function syncTokenState() {
    accessToken.value = getAccessToken()
  }

  async function login(payload: LoginCommand) {
    const result = await loginApi(payload)
    setTokens(result)
    syncTokenState()
    await bootstrap()
  }

  async function bootstrap() {
    if (!getAccessToken()) {
      bootstrapFailed.value = false
      initialized.value = true
      return
    }

    const [user, menus] = await Promise.all([getCurrentUser(), getCurrentUserMenuTree()])
    currentUser.value = user
    menuTree.value = Array.isArray(menus) ? menus : []
    bootstrapFailed.value = false
    initialized.value = true
  }

  async function refreshMenuTree() {
    const menus = await getCurrentUserMenuTree()
    menuTree.value = menus
    console.log('Menu tree refreshed:', menus)
    return menus
  }

  async function logout() {
    const refreshToken = getRefreshToken()

    if (refreshToken) {
      await logoutApi({ refreshToken }).catch(() => undefined)
    }

    clearTokens()
    accessToken.value = ''
    currentUser.value = {
      isAuthenticated: false,
      roles: [],
      permissions: [],
    }
    menuTree.value = []
    bootstrapFailed.value = false
    initialized.value = true
    await router.replace('/login')
  }

  function setBootstrapFailed(value: boolean) {
    bootstrapFailed.value = value
  }

  function hasPermission(permission?: string) {
    if (!permission) {
      return true
    }

    return currentUser.value.permissions.includes(permission)
  }

  function patchCurrentUser(payload: Partial<CurrentUser>) {
    currentUser.value = {
      ...currentUser.value,
      ...payload,
    }
  }

  return {
    accessToken,
    currentUser,
    menuTree,
    initialized,
    bootstrapFailed,
    isAuthenticated,
    login,
    logout,
    bootstrap,
    refreshMenuTree,
    setBootstrapFailed,
    patchCurrentUser,
    hasPermission,
  }
})
