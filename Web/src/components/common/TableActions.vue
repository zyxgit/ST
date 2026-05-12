<script setup lang="ts">
import { ChevronDownOutline } from '@vicons/ionicons5'
import { NButton, NDropdown, NIcon } from 'naive-ui'

interface ActionItem {
  key: string
  label: string
  onClick: () => void | Promise<void>
  type?: 'default' | 'error'
  disabled?: boolean
}

const props = withDefaults(
  defineProps<{
    actions?: ActionItem[]
    moreActions?: ActionItem[]
  }>(),
  {
    actions: () => [],
    moreActions: () => [],
  },
)

function handleMoreSelect(key: string) {
  const action = props.moreActions.find((item) => item.key === key)
  action?.onClick()
}
</script>

<template>
  <div class="table-actions">
    <n-button
      v-for="item in actions"
      :key="item.key"
      text
      size="small"
      class="table-actions__button"
      :class="{ 'table-actions__button--danger': item.type === 'error' }"
      :disabled="item.disabled"
      @click="item.onClick"
    >
      {{ item.label }}
    </n-button>

    <n-dropdown
      v-if="moreActions.length"
      trigger="hover"
      :options="moreActions.map((item) => ({ key: item.key, label: item.label, disabled: item.disabled }))"
      @select="handleMoreSelect"
    >
      <button type="button" class="table-actions__more">
        <span>更多</span>
        <n-icon size="14"><chevron-down-outline /></n-icon>
      </button>
    </n-dropdown>
  </div>
</template>

<style scoped>
.table-actions {
  display: inline-flex;
  align-items: center;
  gap: 14px;
}

.table-actions__button {
  --n-text-color: var(--n-primary-color, #2563eb) !important;
  --n-text-color-hover: color-mix(in srgb, var(--n-primary-color, #2563eb) 88%, black 12%) !important;
  --n-text-color-pressed: color-mix(in srgb, var(--n-primary-color, #2563eb) 76%, black 24%) !important;
  padding: 0 !important;
}

.table-actions__button--danger {
  --n-text-color: #ef4444 !important;
  --n-text-color-hover: #dc2626 !important;
  --n-text-color-pressed: #b91c1c !important;
}

.table-actions__more {
  display: inline-flex;
  align-items: center;
  gap: 2px;
  padding: 0;
  border: none;
  background: transparent;
  color: var(--n-primary-color, #2563eb);
  cursor: pointer;
  font-size: 14px;
}

.table-actions__more:hover {
  color: color-mix(in srgb, var(--n-primary-color, #2563eb) 88%, black 12%);
}
</style>
