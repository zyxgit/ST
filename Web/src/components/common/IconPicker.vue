<script setup lang="ts">
import { CheckmarkOutline, CloseCircleOutline } from '@vicons/ionicons5'
import { NButton, NIcon, NInput, NModal } from 'naive-ui'
import { computed, ref } from 'vue'

import { menuIconOptions, resolveMenuIcon } from '@/lib/menu-icons'

const props = withDefaults(
  defineProps<{
    value?: string
    placeholder?: string
    title?: string
  }>(),
  {
    value: '', 
    placeholder: '请选择图标',
    title: '选择图标',
  },
)

const emit = defineEmits<{
  'update:value': [value: string]
}>()

const showModal = ref(false)
const keyword = ref('')

const currentIcon = computed(() => resolveMenuIcon(props.value))
const currentLabel = computed(() => menuIconOptions.find((item) => item.value === props.value)?.label ?? '')
const filteredOptions = computed(() => {
  const query = keyword.value.trim().toLowerCase()
  if (!query) {
    return menuIconOptions
  }

  return menuIconOptions.filter((item) =>
    item.label.toLowerCase().includes(query) || item.value.toLowerCase().includes(query),
  )
})

function openPicker() {
  showModal.value = true
}

function handleSelect(value: string) {
  emit('update:value', value)
  showModal.value = false
}

function clearValue() {
  emit('update:value', '')
}
</script>

<template>
  <div class="icon-picker">
    <button type="button" class="icon-picker__trigger" @click="openPicker">
      <div class="icon-picker__preview">
        <n-icon v-if="currentIcon" size="18">
          <component :is="currentIcon" />
        </n-icon>
        <span v-else class="icon-picker__empty">未选择</span>
      </div>
      <div class="icon-picker__meta">
        <strong>{{ currentLabel || placeholder }}</strong>
        <span>{{ value || '点击弹窗选择图标' }}</span>
      </div>
    </button>

    <n-button v-if="value" text type="error" class="icon-picker__clear" @click="clearValue">
      <template #icon>
        <n-icon><close-circle-outline /></n-icon>
      </template>
      清空
    </n-button>

    <n-modal v-model:show="showModal" preset="card" style="width: 760px" :title="title">
      <div class="icon-picker__panel">
        <n-input v-model:value="keyword" clearable placeholder="搜索图标名称或编码" />
        <div class="icon-picker__grid">
          <button v-for="item in filteredOptions" :key="item.value" type="button" class="icon-picker__option"
            :class="{ 'icon-picker__option--active': item.value === value }" @click="handleSelect(item.value)">
            <span class="icon-picker__option-icon">
              <n-icon size="22">
                <component :is="resolveMenuIcon(item.value)!" />
              </n-icon>
            </span>
            <span class="icon-picker__option-label">{{ item.label }}</span>
            <span class="icon-picker__option-value">{{ item.value }}</span>
            <n-icon v-if="item.value === value" class="icon-picker__option-check" size="16">
              <checkmark-outline />
            </n-icon>
          </button>
        </div>
      </div>
    </n-modal>
  </div>
</template>

<style scoped>
.icon-picker {
  display: flex;
  align-items: center;
  gap: 12px;
}

.icon-picker__trigger {
  display: flex;
  align-items: center;
  gap: 12px;
  width: 100%;
  padding: 12px 14px;
  border: 1px solid var(--panel-border);
  border-radius: 14px;
  background: var(--panel-bg-strong);
  cursor: pointer;
  text-align: left;
  transition:
    border-color 0.2s ease,
    box-shadow 0.2s ease,
    transform 0.2s ease;
}

.icon-picker__trigger:hover {
  border-color: color-mix(in srgb, var(--n-primary-color, #2563eb) 36%, var(--panel-border) 64%);
  box-shadow: var(--shadow-1);
  transform: translateY(-1px);
}

.icon-picker__preview {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 42px;
  height: 42px;
  border-radius: 12px;
  background: color-mix(in srgb, var(--n-primary-color, #2563eb) 10%, white 90%);
  color: var(--n-primary-color, #2563eb);
  flex-shrink: 0;
}

.icon-picker__empty {
  color: var(--text-3);
  font-size: 12px;
}

.icon-picker__meta {
  display: flex;
  min-width: 0;
  flex: 1;
  flex-direction: column;
  gap: 4px;
}

.icon-picker__meta strong {
  color: var(--text-1);
  font-size: 14px;
  font-weight: 600;
}

.icon-picker__meta span {
  color: var(--text-3);
  font-size: 12px;
}

.icon-picker__clear {
  flex-shrink: 0;
}

.icon-picker__panel {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.icon-picker__grid {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 14px;
  max-height: 460px;
  overflow: auto;
  padding-right: 4px;
}

.icon-picker__option {
  position: relative;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 8px;
  padding: 18px 12px 14px;
  border: 1px solid var(--panel-border);
  border-radius: 16px;
  background: var(--panel-bg-strong);
  cursor: pointer;
  transition:
    border-color 0.2s ease,
    box-shadow 0.2s ease,
    transform 0.2s ease,
    background-color 0.2s ease;
}

.icon-picker__option:hover {
  transform: translateY(-2px);
  box-shadow: var(--shadow-1);
}

.icon-picker__option--active {
  border-color: var(--n-primary-color, #2563eb);
  background: color-mix(in srgb, var(--n-primary-color, #2563eb) 8%, var(--panel-bg-strong) 92%);
}

.icon-picker__option-icon {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 48px;
  height: 48px;
  border-radius: 14px;
  background: color-mix(in srgb, var(--n-primary-color, #2563eb) 10%, white 90%);
  color: var(--n-primary-color, #2563eb);
}

.icon-picker__option-label {
  color: var(--text-1);
  font-size: 14px;
  font-weight: 600;
}

.icon-picker__option-value {
  color: var(--text-3);
  font-size: 12px;
  text-align: center;
  word-break: break-all;
}

.icon-picker__option-check {
  position: absolute;
  top: 10px;
  right: 10px;
  color: var(--n-primary-color, #2563eb);
}
</style>
