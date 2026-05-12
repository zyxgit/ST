<script setup lang="ts">
import { computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { NMenu } from 'naive-ui'

import { buildTopSections, getActiveSection, normalizePath } from '@/lib/admin-menu'
import { useAppStore } from '@/stores/app'
import { useAuthStore } from '@/stores/auth'

const route = useRoute()
const router = useRouter()
const appStore = useAppStore()
const authStore = useAuthStore()

const options = computed(() =>
  buildTopSections(authStore.menuTree).map((item) => ({
    key: item.key,
    label: item.title,
  })),
)

const currentSection = computed(() => getActiveSection(appStore.activeTopMenuKey, authStore.menuTree))

const childOptions = computed(() =>
  (currentSection.value?.children ?? []).map((item) => ({
    key: normalizePath(item.path),
    label: item.name,
  })),
)

async function handleUpdateValue(key: string) {
  const target = buildTopSections(authStore.menuTree).find((item) => item.key === key)
  if (!target) {
    return
  }

  appStore.setActiveTopMenu(key)
  await router.push(target.path)
}

async function handleChildUpdateValue(key: string) {
  await router.push(key)
}
</script>

<template>
  <div class="top-nav">
    <n-menu
      mode="horizontal"
      responsive
      :options="options"
      :value="appStore.activeTopMenuKey"
      @update:value="handleUpdateValue"
    />
    <n-menu
      v-if="appStore.navigationMode === 'top' && childOptions.length"
      mode="horizontal"
      responsive
      :options="childOptions"
      :value="route.path"
      @update:value="handleChildUpdateValue"
    />
  </div>
</template>

<style scoped>
.top-nav {
  display: flex;
  flex-direction: column;
  align-items: stretch;
  justify-content: center;
  min-width: 320px;
  gap: 4px;
}

:deep(.n-menu.n-menu--horizontal .n-menu-item-content) {
  color: var(--text-2);
}

:deep(.n-menu.n-menu--horizontal .n-menu-item-content.n-menu-item-content--selected) {
  color: var(--n-primary-color, #2563eb);
}

:deep(.n-menu + .n-menu) {
  border-top: 1px solid color-mix(in srgb, var(--panel-border) 68%, transparent 32%);
  padding-top: 4px;
}
</style>
