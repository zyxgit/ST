<script setup lang="ts">
import { NButton, NCard, NSpace } from 'naive-ui'

withDefaults(
  defineProps<{
    title?: string
    description?: string
    buttonText?: string
  }>(),
  {
    title: '服务暂时不可用',
    description: '后端接口当前无法连接，请确认服务已启动后再重试。',
    buttonText: '刷新重试',
  },
)

defineEmits<{
  retry: []
}>()
</script>

<template>
  <n-card class="page-card" :bordered="false">
    <n-space vertical :size="12" align="center" style="padding: 20px 0; text-align: center">
      <div style="font-size: 18px; font-weight: 600">{{ title }}</div>
      <div style="max-width: 520px; color: var(--text-3)">
        {{ description }}
      </div>
      <n-button type="primary" @click="$emit('retry')">{{ buttonText }}</n-button>
    </n-space>
  </n-card>
</template>
