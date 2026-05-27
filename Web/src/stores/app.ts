import { useStorage } from '@vueuse/core'
import { defineStore } from 'pinia'
import { computed, ref } from 'vue'

import { findTopMenuKeyByPath } from '@/lib/admin-menu'
import type { MenuTreeNode } from '@/types/menu'

interface VisitedTab {
  key: string
  title: string
}

export type ThemeMode = 'light' | 'dark' | 'system'
export type NavigationMode = 'side' | 'top' | 'mix'
export type ContentWidthMode = 'fluid' | 'fixed'
export type RouteAnimationMode = 'none' | 'fade' | 'slide-up' | 'slide-right' | 'zoom-fade' | 'blur'

export const useAppStore = defineStore('app', () => {
  const collapsed = useStorage('st-admin-collapsed', false)
  const themeMode = useStorage<ThemeMode>('st-admin-theme-mode', 'light')
  const primaryColor = useStorage('st-admin-primary-color', '#0f766e')
  const navigationMode = useStorage<NavigationMode>('st-admin-navigation-mode', 'side')
  const contentWidth = useStorage<ContentWidthMode>('st-admin-content-width', 'fluid')
  const fixedHeader = useStorage('st-admin-fixed-header', true)
  const fixedSidebar = useStorage('st-admin-fixed-sidebar', true)
  const multiTabs = useStorage('st-admin-multi-tabs', true)
  const fixedTabs = useStorage('st-admin-fixed-tabs', false)
  const colorWeakMode = useStorage('st-admin-color-weak-mode', false)
  const routeAnimation = useStorage<RouteAnimationMode>('st-admin-route-animation', 'fade')
  const activeTopMenuKey = ref('/system')
  const visitedTabs = ref<VisitedTab[]>([])
  const systemDark = ref(false)

  const siderWidth = computed(() => (collapsed.value ? 80 : 232))
  const isDark = computed(() => {
    if (themeMode.value === 'system') {
      return systemDark.value
    }

    return themeMode.value === 'dark'
  })

  function toggleCollapsed() {
    collapsed.value = !collapsed.value
  }

  function toggleTheme() {
    themeMode.value = isDark.value ? 'light' : 'dark'
  }

  function setPrimaryColor(color: string) {
    primaryColor.value = color
  }

  function setThemeMode(mode: ThemeMode) {
    themeMode.value = mode
  }

  function setNavigationMode(mode: NavigationMode) {
    navigationMode.value = mode

    if (mode === 'top') {
      collapsed.value = false
    }
  }

  function setContentWidth(mode: ContentWidthMode) {
    contentWidth.value = mode
  }

  function setFixedHeader(value: boolean) {
    fixedHeader.value = value
  }

  function setFixedSidebar(value: boolean) {
    fixedSidebar.value = value
  }

  function setMultiTabs(value: boolean) {
    multiTabs.value = value
    if (!value) {
      fixedTabs.value = false
    }
  }

  function setFixedTabs(value: boolean) {
    fixedTabs.value = multiTabs.value ? value : false
  }

  function setColorWeakMode(value: boolean) {
    colorWeakMode.value = value
  }

  function setRouteAnimation(value: RouteAnimationMode) {
    routeAnimation.value = value
  }

  function setSystemDark(value: boolean) {
    systemDark.value = value
  }

  function setActiveTopMenu(key: string) {
    activeTopMenuKey.value = key
  }

  function syncRoute(path: string, title: string, menuTree: MenuTreeNode[]) {
    activeTopMenuKey.value = findTopMenuKeyByPath(path, menuTree)

    if (path === '/login' || path.startsWith('/:pathMatch')) {
      return
    }

    const hasVisited = visitedTabs.value.some((item) => item.key === path)
    if (!hasVisited) {
      visitedTabs.value.push({ key: path, title })
    }
  }

  function removeVisitedTab(key: string) {
    visitedTabs.value = visitedTabs.value.filter((item) => item.key !== key)
  }

  return {
    collapsed,
    isDark,
    themeMode,
    primaryColor,
    navigationMode,
    contentWidth,
    fixedHeader,
    fixedSidebar,
    multiTabs,
    fixedTabs,
    colorWeakMode,
    routeAnimation,
    activeTopMenuKey,
    visitedTabs,
    siderWidth,
    toggleCollapsed,
    toggleTheme,
    setPrimaryColor,
    setThemeMode,
    setNavigationMode,
    setContentWidth,
    setFixedHeader,
    setFixedSidebar,
    setMultiTabs,
    setFixedTabs,
    setColorWeakMode,
    setRouteAnimation,
    setSystemDark,
    setActiveTopMenu,
    syncRoute,
    removeVisitedTab,
  }
})
