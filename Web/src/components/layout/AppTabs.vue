<script setup lang="ts">
import { CloseOutline, ChevronBackOutline, ChevronForwardOutline } from '@vicons/ionicons5'
import { NButton, NIcon } from 'naive-ui'
import { useRoute, useRouter } from 'vue-router'
import { ref, onMounted, onUnmounted, nextTick } from 'vue'

import { useAppStore } from '@/stores/app'

const route = useRoute()
const router = useRouter()
const appStore = useAppStore()

const tabsContainerRef = ref<HTMLElement | null>(null)
const showLeftArrow = ref(false)
const showRightArrow = ref(false)

async function goTo(key: string) {
  await router.push(key)
  nextTick(() => {
    scrollToActiveTab()
    updateArrows()
  })
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

  nextTick(() => {
    updateArrows()
  })
}

function updateArrows() {
  const container = tabsContainerRef.value
  if (!container) return

  showLeftArrow.value = container.scrollLeft > 0
  showRightArrow.value = container.scrollLeft < container.scrollWidth - container.clientWidth
}

function scrollToActiveTab() {
  const container = tabsContainerRef.value
  if (!container) return

  const activeTab = container.querySelector('.tabs-bar__item--active') as HTMLElement
  if (activeTab) {
    const containerRect = container.getBoundingClientRect()
    const tabRect = activeTab.getBoundingClientRect()

    if (tabRect.left < containerRect.left) {
      container.scrollBy({ left: tabRect.left - containerRect.left - 20, behavior: 'smooth' })
    } else if (tabRect.right > containerRect.right) {
      container.scrollBy({ left: tabRect.right - containerRect.right + 20, behavior: 'smooth' })
    }
  }
}

function scrollLeft() {
  const container = tabsContainerRef.value
  if (!container) return
  container.scrollBy({ left: -200, behavior: 'smooth' })
  setTimeout(updateArrows, 300)
}

function scrollRight() {
  const container = tabsContainerRef.value
  if (!container) return
  container.scrollBy({ left: 200, behavior: 'smooth' })
  setTimeout(updateArrows, 300)
}

function handleWheel(e: WheelEvent) {
  const container = tabsContainerRef.value
  if (!container) return
  e.preventDefault()
  container.scrollBy({ left: e.deltaY > 0 ? 100 : -100, behavior: 'smooth' })
  setTimeout(updateArrows, 100)
}

onMounted(() => {
  const container = tabsContainerRef.value
  if (container) {
    container.addEventListener('wheel', handleWheel, { passive: false })
    updateArrows()
    scrollToActiveTab()
  }
})

onUnmounted(() => {
  const container = tabsContainerRef.value
  if (container) {
    container.removeEventListener('wheel', handleWheel)
  }
})
</script>

<template>
  <div class="tabs-bar-wrapper">
    <button
      v-show="showLeftArrow"
      class="tabs-bar__arrow tabs-bar__arrow--left"
      type="button"
      @click="scrollLeft"
    >
      <n-icon size="16"><chevron-back-outline /></n-icon>
    </button>

    <div ref="tabsContainerRef" class="tabs-bar">
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

    <button
      v-show="showRightArrow"
      class="tabs-bar__arrow tabs-bar__arrow--right"
      type="button"
      @click="scrollRight"
    >
      <n-icon size="16"><chevron-forward-outline /></n-icon>
    </button>
  </div>
</template>

<style scoped>
.tabs-bar-wrapper {
  position: relative;
  display: flex;
  align-items: center;
  border-top: 1px solid var(--panel-border);
  background: var(--panel-bg-soft);
}

.tabs-bar {
  display: flex;
  gap: 8px;
  padding: 10px 8px;
  overflow-x: auto;
  scrollbar-width: none;
  -ms-overflow-style: none;
  flex: 1;
  scroll-behavior: smooth;
}

.tabs-bar::-webkit-scrollbar {
  display: none;
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
  white-space: nowrap;
  flex-shrink: 0;
}

.tabs-bar__item--active {
  background: color-mix(in srgb, var(--panel-bg-strong) 72%, var(--n-primary-color, #2563eb) 28%);
  color: #ffffff;
  border-color: transparent;
}

.tabs-bar__arrow {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 28px;
  height: 28px;
  border: 1px solid var(--panel-border);
  border-radius: 6px;
  background: var(--panel-bg-strong);
  color: var(--text-2);
  cursor: pointer;
  flex-shrink: 0;
  transition: all 0.2s;
}

.tabs-bar__arrow:hover {
  background: var(--panel-bg-soft);
  color: var(--text-1);
}

.tabs-bar__arrow--left {
  margin-left: 4px;
}

.tabs-bar__arrow--right {
  margin-right: 4px;
}
</style>
