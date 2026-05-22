<script setup lang="ts">
import {
  darkTheme,
  dateZhCN,
  NConfigProvider,
  NDialogProvider,
  NLoadingBarProvider,
  NMessageProvider,
  NNotificationProvider,
  type GlobalThemeOverrides,
  zhCN,
} from 'naive-ui'
import { usePreferredDark } from '@vueuse/core'
import { computed, watchEffect } from 'vue'

import { useAppStore } from '@/stores/app'
import { useAuthStore } from '@/stores/auth'

const appStore = useAppStore()
const authStore = useAuthStore()
const preferredDark = usePreferredDark()
const isBootstrapping = computed(() => !authStore.initialized)
const theme = computed(() => (appStore.isDark ? darkTheme : null))
const themeOverrides = computed<GlobalThemeOverrides>(() => ({
  common: {
    primaryColor: appStore.primaryColor,
    primaryColorHover: appStore.primaryColor,
    primaryColorPressed: appStore.primaryColor,
    primaryColorSuppl: appStore.primaryColor,
    borderRadius: '14px',
  },
  Card: {
    color: appStore.isDark ? '#14181f' : '#ffffff',
    colorEmbedded: appStore.isDark ? '#14181f' : '#ffffff',
    borderColor: appStore.isDark ? '#252b36' : '#e5e7eb',
  },
  DataTable: {
    thColor: appStore.isDark ? '#1b212b' : '#f8fafc',
    tdColor: appStore.isDark ? '#14181f' : '#ffffff',
    borderColor: appStore.isDark ? '#252b36' : '#e5e7eb',
  },
  Input: {
    color: appStore.isDark ? '#11161d' : '#ffffff',
  },
  Select: {
    peers: {
      InternalSelection: {
        color: appStore.isDark ? '#11161d' : '#ffffff',
      },
    },
  },
}))

watchEffect(() => {
  appStore.setSystemDark(preferredDark.value)
  document.documentElement.dataset.theme = appStore.isDark ? 'dark' : 'light'
  document.body.dataset.theme = appStore.isDark ? 'dark' : 'light'
  document.documentElement.dataset.themeMode = appStore.themeMode
  document.documentElement.dataset.colorWeak = appStore.colorWeakMode ? 'true' : 'false'
  document.body.dataset.colorWeak = appStore.colorWeakMode ? 'true' : 'false'
})
</script>

<template>
  <template v-if="isBootstrapping">
    <div class="app-loading">
      <div class="app-loading__brand">ST Admin</div>
      <div class="app-loading__spinner">
        <div class="app-loading__ring app-loading__ring--outer"></div>
        <div class="app-loading__ring app-loading__ring--inner"></div>
        <div class="app-loading__core"></div>
      </div>
      <div class="app-loading__text">正在加载...</div>
    </div>
  </template>
  <template v-else>
    <n-config-provider :theme="theme" :theme-overrides="themeOverrides" :locale="zhCN" :date-locale="dateZhCN">
      <n-loading-bar-provider>
        <n-dialog-provider>
          <n-notification-provider>
            <n-message-provider>
              <router-view />
            </n-message-provider>
          </n-notification-provider>
        </n-dialog-provider>
      </n-loading-bar-provider>
    </n-config-provider>
  </template>
</template>
