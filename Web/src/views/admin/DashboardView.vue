<script setup lang="ts">
import type { EChartsOption } from 'echarts'
import * as echarts from 'echarts'
import { NCard, NGrid, NGridItem, NStatistic } from 'naive-ui'
import { h, nextTick, onMounted, ref } from 'vue'

import PageSection from '@/components/common/PageSection.vue'
import { useAuthStore } from '@/stores/auth'

const authStore = useAuthStore()
const chartRef = ref<HTMLDivElement | null>(null)

const chartOptions: EChartsOption = {
  tooltip: { trigger: 'axis' },
  grid: { left: 24, right: 24, top: 32, bottom: 24 },
  xAxis: { type: 'category', data: ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'] },
  yAxis: { type: 'value' },
  series: [
    {
      name: '访问请求',
      type: 'line',
      smooth: true,
      areaStyle: {},
      data: [12, 18, 15, 26, 33, 29, 42],
      color: '#18a058',
    },
  ],
}

// 1. 定义一个返回 VNode 的渲染函数
const renderCardTitle = () => {
  return h('div', [
    h('span', '近 7 日请求趋势 '),
    h('span', { style: 'color: #999; font-size: 12px;' },'（示例模板非真实数据）'),
  ])
}

onMounted(async () => {
  await nextTick()

  if (!chartRef.value) {
    return
  }

  const instance = echarts.init(chartRef.value)
  instance.setOption(chartOptions)
  window.addEventListener('resize', () => instance.resize())
})
</script>

<template>
  <page-section title="工作台" description="当前用户、权限情况和后台概览。">
    <n-grid :cols="4" :x-gap="16" :y-gap="16">
      <n-grid-item>
        <n-card class="page-card" :bordered="false">
          <n-statistic label="当前用户" :value="authStore.currentUser.nickName || '未加载'" />
        </n-card>
      </n-grid-item>
      <n-grid-item>
        <n-card class="page-card" :bordered="false">
          <n-statistic label="角色数" :value="authStore.currentUser.roles.length" />
        </n-card>
      </n-grid-item>
      <n-grid-item>
        <n-card class="page-card" :bordered="false">
          <n-statistic label="权限数" :value="authStore.currentUser.permissions.length" />
        </n-card>
      </n-grid-item>
      <n-grid-item>
        <n-card class="page-card" :bordered="false">
          <n-statistic label="登录 IP" :value="authStore.currentUser.clientIp || '-'" />
        </n-card>
      </n-grid-item>
    </n-grid>

    <n-card class="page-card" :bordered="false" :title="renderCardTitle" content-style="padding: 0;" >
      <div ref="chartRef" style="height: 360px"></div>
    </n-card>
  </page-section>
</template>
