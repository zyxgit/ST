<script setup lang="ts">
import { CloseOutline } from '@vicons/ionicons5'
import { NButton, NIcon } from 'naive-ui'
import { useRoute, useRouter } from 'vue-router'

import { useAppStore } from '@/stores/app'

const route = useRoute()
const router = useRouter()
const appStore = useAppStore()

async function goTo(key: string) {
  await router.push(key)
}

async function closeTab(key: string) {
  if (key === '/dashboard') {
    return
  }

  const isCurrent = route.path === key
  appStore.removeVisitedTab(key)

  if (isCurrent) {
    const fallback = appStore.visitedTabs[appStore.visitedTabs.length - 1]?.key ?? '/system/users'
    await router.push(fallback)
  }
}
</script>

<template>
  <div class="tabs-bar">
    <button
      v-for="item in appStore.visitedTabs"
      :key="item.key"
      class="tabs-bar__item"
      :class="{ 'tabs-bar__item--active': route.path === item.key }"
      type="button"
      @click="goTo(item.key)"
    >
      <span>{{ item.title }}</span>
      <n-button
        v-if="item.key !== '/dashboard'"
        quaternary
        circle
        size="tiny"
        @click.stop="closeTab(item.key)"
      >
        <template #icon>
          <n-icon><close-outline /></n-icon>
        </template>
      </n-button>
    </button>
  </div>
</template>

<style scoped>
.tabs-bar {
  display: flex;
  gap: 8px;
  padding: 10px 20px;
  overflow-x: auto;
  border-top: 1px solid var(--panel-border);
  background: var(--panel-bg-soft);
}

.tabs-bar__item {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  padding: 7px 12px;
  border: 1px solid var(--panel-border);
  border-radius: 10px;
  background: var(--panel-bg-strong);
  color: var(--text-2);
  cursor: pointer;
}

.tabs-bar__item--active {
  background: color-mix(in srgb, var(--panel-bg-strong) 72%, var(--n-primary-color, #2563eb) 28%);
  color: #ffffff;
  border-color: transparent;
}
</style>
