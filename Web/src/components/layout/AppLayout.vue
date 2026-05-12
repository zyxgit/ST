<script setup lang="ts">
import { NButton, NCard, NSpace } from 'naive-ui'
import { computed, watch } from 'vue'
import { useRoute } from 'vue-router'

import AppHeader from './AppHeader.vue'
import AppSidebar from './AppSidebar.vue'
import AppTabs from './AppTabs.vue'

import { useAppStore } from '@/stores/app'
import { useAuthStore } from '@/stores/auth'

const appStore = useAppStore()
const authStore = useAuthStore()
const route = useRoute()
const showSidebar = computed(() => appStore.navigationMode !== 'top')

function reloadPage() {
  window.location.reload()
}

watch(
  () => [route.path, route.meta.title, authStore.menuTree],
  () => {
    appStore.syncRoute(route.path, String(route.meta.title ?? '未命名页面'), authStore.menuTree)
  },
  { immediate: true, deep: true },
)
</script>

<template>
  <div class="layout-shell" :class="`layout-shell--${appStore.navigationMode}`">
    <aside
      v-if="showSidebar"
      class="layout-shell__sider"
      :class="{ 'layout-shell__sider--fixed': appStore.fixedSidebar }"
      :style="{ width: `${appStore.siderWidth}px` }"
    >
      <app-sidebar />
    </aside>
    <section class="layout-shell__main">
      <div class="layout-shell__top" :class="{ 'layout-shell__top--fixed': appStore.fixedHeader || appStore.fixedTabs }">
        <app-header class="layout-shell__header" :class="{ 'layout-shell__header--fixed': appStore.fixedHeader }" />
        <app-tabs
          v-if="appStore.multiTabs"
          class="layout-shell__tabs"
          :class="{ 'layout-shell__tabs--fixed': appStore.fixedTabs }"
        />
      </div>
      <main class="layout-shell__content">
        <div
          class="layout-shell__content-inner"
          :class="{ 'layout-shell__content-inner--fixed': appStore.contentWidth === 'fixed' }"
        >
          <n-card v-if="authStore.bootstrapFailed" class="page-card" :bordered="false" style="margin-bottom: 20px">
            <n-space justify="space-between" align="center">
              <div>
                <div style="font-size: 16px; font-weight: 600">后台服务暂时不可用</div>
                <div style="margin-top: 4px; color: var(--text-3)">当前接口未连接成功，页面数据可能无法加载。</div>
              </div>
              <n-button type="primary" @click="reloadPage">刷新重试</n-button>
            </n-space>
          </n-card>
          <router-view v-slot="{ Component }">
            <transition :name="appStore.routeAnimation === 'none' ? '' : appStore.routeAnimation" mode="out-in">
              <component :is="Component" />
            </transition>
          </router-view>
        </div>
      </main>
    </section>
  </div>
</template>

<style scoped>
.layout-shell {
  display: flex;
  width: 100%;
  height: 100vh;
  background: var(--app-bg);
}

.layout-shell__sider {
  flex-shrink: 0;
  height: 100vh;
  border-right: 1px solid var(--panel-border);
  background: var(--panel-bg-strong);
  transition: width 0.2s ease;
}

.layout-shell__sider--fixed {
  position: sticky;
  top: 0;
}

.layout-shell__main {
  display: flex;
  flex: 1;
  min-width: 0;
  flex-direction: column;
  height: 100vh;
}

.layout-shell__top {
  z-index: 5;
}

.layout-shell__top--fixed {
  position: sticky;
  top: 0;
  backdrop-filter: blur(14px);
}

.layout-shell__content {
  flex: 1;
  overflow: auto;
  padding: 20px;
  background:
    radial-gradient(circle at top left, rgba(37, 99, 235, 0.08), transparent 20%),
    var(--app-bg-soft);
}

.layout-shell__content-inner {
  width: 100%;
}

.layout-shell__content-inner--fixed {
  max-width: 1440px;
  margin: 0 auto;
}

.fade-enter-active,
.fade-leave-active {
  transition:
    opacity 0.18s ease,
    transform 0.18s ease;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
  transform: translateY(10px);
}

.slide-up-enter-active,
.slide-up-leave-active,
.slide-right-enter-active,
.slide-right-leave-active,
.zoom-fade-enter-active,
.zoom-fade-leave-active,
.blur-enter-active,
.blur-leave-active {
  transition:
    opacity 0.22s ease,
    transform 0.22s ease,
    filter 0.22s ease;
}

.slide-up-enter-from,
.slide-up-leave-to {
  opacity: 0;
  transform: translateY(18px);
}

.slide-right-enter-from,
.slide-right-leave-to {
  opacity: 0;
  transform: translateX(22px);
}

.zoom-fade-enter-from,
.zoom-fade-leave-to {
  opacity: 0;
  transform: scale(0.97);
}

.blur-enter-from,
.blur-leave-to {
  opacity: 0;
  filter: blur(10px);
  transform: scale(0.985);
}
</style>
