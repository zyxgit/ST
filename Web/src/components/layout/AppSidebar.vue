<script setup lang="ts">
import { BookOutline, HomeOutline, ListOutline, PeopleOutline, ReceiptOutline, SettingsOutline, ShieldOutline } from '@vicons/ionicons5'
import type { Component } from 'vue'
import { computed, h, ref, watch } from 'vue'
import { RouterLink, useRoute } from 'vue-router'
import { NIcon, NMenu } from 'naive-ui'
import type { MenuOption } from 'naive-ui'

import AppLogo from './AppLogo.vue'

import { buildSideMenuOptions, buildSidebarOptions, getActiveSection } from '@/lib/admin-menu'
import { resolveMenuIcon } from '@/lib/menu-icons'
import { useAppStore } from '@/stores/app'
import { useAuthStore } from '@/stores/auth'

const route = useRoute()
const appStore = useAppStore()
const authStore = useAuthStore()
const expandedKeys = ref<string[]>([])

const localIconMap: Record<string, Component> = {
  dashboard: HomeOutline,
  '/dashboard': HomeOutline,
  system: SettingsOutline,
  '/system': SettingsOutline,
  'system:user': PeopleOutline,
  'system:role': ShieldOutline,
  'system:menu': ListOutline,
  '/system/users': PeopleOutline,
  '/system/roles': ShieldOutline,
  '/system/menus': ListOutline,
  'operation-logs': ReceiptOutline,
  '/operation-logs': ReceiptOutline,
}

function renderIcon(icon: Component) {
  return () => h(NIcon, null, { default: () => h(icon) })
}

function resolveIcon(key: string, iconName?: string | null, fallbackIconKey?: string) {
  const normalizedKey = key.startsWith('/') ? key.slice(1) : key
  const normalizedFallbackKey = fallbackIconKey?.startsWith('/') ? fallbackIconKey.slice(1) : fallbackIconKey

  return (
    resolveMenuIcon(iconName) ??
    localIconMap[key] ??
    localIconMap[normalizedKey] ??
    (fallbackIconKey ? localIconMap[fallbackIconKey] : undefined) ??
    (normalizedFallbackKey ? localIconMap[normalizedFallbackKey] : undefined) ??
    ListOutline
  )
}

function mapMenuOptions(items: MenuOption[], fallbackIconKey?: string): MenuOption[] {
  return items.map((item) => {
    const key = String(item.key)
    const children = Array.isArray(item.children) ? mapMenuOptions(item.children, key) : undefined
    const iconName = 'iconName' in item ? String(item.iconName ?? '') : ''

    return {
      ...item,
      icon: renderIcon(resolveIcon(key, iconName, fallbackIconKey)),
      label: children?.length
        ? String(item.label)
        : () => h(RouterLink, { to: key }, { default: () => String(item.label) }),
      ...(children?.length ? { children } : {}),
    }
  })
}

function findExpandedKeysByPath(path: string) {
  const keys: string[] = []

  function visit(items: MenuOption[], parents: string[] = []) {
    for (const item of items) {
      const key = String(item.key)
      if (key === path) {
        keys.push(...parents)
        return true
      }

      if (Array.isArray(item.children) && item.children.length) {
        if (visit(item.children, [...parents, key])) {
          return true
        }
      }
    }

    return false
  }

  visit(buildSideMenuOptions(authStore.menuTree))
  return [...new Set(keys)]
}

const currentSection = computed(() => getActiveSection(appStore.activeTopMenuKey, authStore.menuTree))

const menuOptions = computed<MenuOption[]>(() =>
  mapMenuOptions(
    appStore.navigationMode === 'side'
    ? buildSideMenuOptions(authStore.menuTree)
    : buildSidebarOptions(appStore.activeTopMenuKey, authStore.menuTree),
  ),
)

watch(
  () => [route.path, appStore.navigationMode, authStore.menuTree],
  () => {
    if (appStore.navigationMode === 'side') {
      expandedKeys.value = findExpandedKeysByPath(route.path)
    }
  },
  { immediate: true, deep: true },
)

function handleExpandedKeysUpdate(keys: string[]) {
  expandedKeys.value = keys
}
</script>

<template>
  <div class="sidebar">
    <app-logo />
    <div v-if="!appStore.collapsed && appStore.navigationMode !== 'side'" class="sidebar__section">
      <span class="sidebar__section-label">当前模块</span>
      <strong>{{ currentSection?.title || '管理后台' }}</strong>
    </div>
    <Transition name="sidebar-menu" mode="out-in">
      <n-menu
        :key="appStore.activeTopMenuKey"
        :collapsed="appStore.collapsed"
        :collapsed-width="80"
        :collapsed-icon-size="20"
        :indent="18"
        :expanded-keys="expandedKeys"
        :options="menuOptions"
        :value="route.path"
        @update:expanded-keys="handleExpandedKeysUpdate"
      />
    </Transition>
  </div>
</template>

<style scoped>
.sidebar {
  display: flex;
  height: 100%;
  flex-direction: column;
  background: var(--panel-bg-strong);
}

.sidebar__section {
  padding: 16px 18px 10px;
}

.sidebar__section-label {
  display: block;
  margin-bottom: 6px;
  color: var(--text-3);
  font-size: 12px;
}

.sidebar__section strong {
  color: var(--text-1);
  font-size: 18px;
}

.sidebar-menu-enter-active,
.sidebar-menu-leave-active {
  transition:
    opacity 0.2s ease,
    transform 0.2s ease;
}

.sidebar-menu-enter-from,
.sidebar-menu-leave-to {
  opacity: 0;
  transform: translateX(10px);
}
</style>
