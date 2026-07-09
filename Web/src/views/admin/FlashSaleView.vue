<script setup lang="ts">
import type { DataTableColumns } from 'naive-ui'
import {
  NButton,
  NCard,
  NDataTable,
  NForm,
  NFormItem,
  NInputNumber,
  NProgress,
  NSelect,
  NSpace,
  NStatistic,
  NTag,
} from 'naive-ui'
import { computed, h, onMounted, reactive, ref } from 'vue'

import PageSection from '@/components/common/PageSection.vue'
import { createOrder } from '@/api/order'
import { getSkus } from '@/api/inventory'
import { useDiscrete } from '@/lib/naive'
import type { SkuDto } from '@/types/inventory'

const { message } = useDiscrete()

// ── SKU 数据 ──────────────────────────────────────────────────────────────────
const skuOptions = ref<{ label: string; value: string; sku: SkuDto }[]>([])
const selectedSkuId = ref<string | null>(null)

const selectedSku = computed(() => skuOptions.value.find((o) => o.value === selectedSkuId.value)?.sku ?? null)

async function loadSkus() {
  try {
    const skus = await getSkus()
    skuOptions.value = skus.map((s) => ({
      label: `${s.productName} (库存: ${s.available})`,
      value: s.skuId,
      sku: s,
    }))
  } catch {
    message.error('加载 SKU 列表失败')
  }
}

// ── 模拟配置 ──────────────────────────────────────────────────────────────────
const config = reactive({
  quantity: 1,
  concurrency: 10,
  unitPrice: 99.9,
})

// ── 模拟结果 ──────────────────────────────────────────────────────────────────
interface SimResult {
  index: number
  userId: string
  status: 'success' | 'error'
  orderNo: string
  error: string
  duration: number
}

const running = ref(false)
const results = ref<SimResult[]>([])
const completedCount = ref(0)
const totalCount = ref(0)

const successCount = computed(() => results.value.filter((r) => r.status === 'success').length)
const failCount = computed(() => results.value.filter((r) => r.status === 'error').length)
const progressPercent = computed(() => (totalCount.value === 0 ? 0 : Math.round((completedCount.value / totalCount.value) * 100)))

const resultColumns: DataTableColumns<SimResult> = [
  { title: '#', key: 'index', width: 60, align: 'center' },
  { title: '用户 ID', key: 'userId', width: 300, ellipsis: { tooltip: true } },
  {
    title: '状态',
    key: 'status',
    width: 80,
    align: 'center',
    render: (row) =>
      row.status === 'success'
        ? h(NTag, { type: 'success', size: 'small' }, { default: () => '成功' })
        : h(NTag, { type: 'error', size: 'small' }, { default: () => '失败' }),
  },
  { title: '订单号', key: 'orderNo', width: 220, ellipsis: { tooltip: true } },
  { title: '错误信息', key: 'error', minWidth: 200, ellipsis: { tooltip: true } },
  {
    title: '耗时(ms)',
    key: 'duration',
    width: 90,
    align: 'right',
    render: (row) => `${row.duration}`,
  },
]

// ── 开始抢购 ──────────────────────────────────────────────────────────────────
async function handleStart() {
  if (!selectedSkuId.value) {
    message.warning('请选择 SKU')
    return
  }

  running.value = true
  results.value = []
  completedCount.value = 0
  totalCount.value = config.concurrency

  const skuId = selectedSkuId.value
  const productName = selectedSku.value?.productName ?? '未知商品'

  const tasks = Array.from({ length: config.concurrency }, (_, i) => {
    const userId = crypto.randomUUID()
    const startTime = performance.now()

    return createOrder({
      userId,
      items: [{ skuId, productName, quantity: config.quantity, unitPrice: config.unitPrice }],
    })
      .then((order): SimResult => ({
        index: i + 1,
        userId,
        status: 'success',
        orderNo: order.orderNo,
        error: '',
        duration: Math.round(performance.now() - startTime),
      }))
      .catch((err): SimResult => ({
        index: i + 1,
        userId,
        status: 'error',
        orderNo: '',
        error: err?.response?.data?.detail ?? err?.response?.data?.message ?? err?.message ?? '未知错误',
        duration: Math.round(performance.now() - startTime),
      }))
      .then((result) => {
        completedCount.value++
        // 实时追加结果
        results.value = [...results.value, result]
        return result
      })
  })

  await Promise.allSettled(tasks)
  running.value = false
  message.info(`模拟完成：成功 ${successCount.value}，失败 ${failCount.value}`)

  // 刷新 SKU 库存
  await loadSkus()
}

function handleReset() {
  results.value = []
  completedCount.value = 0
  totalCount.value = 0
}

onMounted(() => {
  loadSkus()
})
</script>

<template>
  <page-section title="抢购模拟" description="模拟高并发下单场景，验证 Saga 分布式事务在竞态条件下的表现。">
    <n-card class="page-card" title="模拟配置" :bordered="false">
      <n-form label-placement="left" label-width="100">
        <n-form-item label="选择 SKU">
          <n-select
            v-model:value="selectedSkuId"
            :options="skuOptions"
            placeholder="请选择要抢购的商品"
            filterable
            style="max-width: 400px"
          />
        </n-form-item>
        <n-form-item v-if="selectedSku" label="当前库存">
          <n-space>
            <n-statistic label="可用" :value="selectedSku.available" />
            <n-statistic label="已冻结" :value="selectedSku.frozen" />
            <n-statistic label="已售" :value="selectedSku.sold" />
          </n-space>
        </n-form-item>
        <n-form-item label="单价(元)">
          <n-input-number v-model:value="config.unitPrice" :min="0.01" :step="1" :precision="2" style="width: 160px" />
        </n-form-item>
        <n-form-item label="每人数量">
          <n-input-number v-model:value="config.quantity" :min="1" :max="99" style="width: 160px" />
        </n-form-item>
        <n-form-item label="并发数">
          <n-input-number v-model:value="config.concurrency" :min="1" :max="100" style="width: 160px" />
        </n-form-item>
        <n-form-item>
          <n-space>
            <n-button type="primary" :loading="running" :disabled="running || !selectedSkuId" @click="handleStart">
              {{ running ? '抢购中...' : '开始抢购' }}
            </n-button>
            <n-button :disabled="running" @click="handleReset">重置</n-button>
          </n-space>
        </n-form-item>
      </n-form>
    </n-card>

    <n-card v-if="totalCount > 0" class="page-card" title="实时统计" :bordered="false">
      <n-space vertical>
        <n-space justify="space-between" style="width: 100%">
          <n-statistic label="总请求数" :value="totalCount" />
          <n-statistic label="已完成" :value="completedCount" />
          <n-statistic label="成功" :value="successCount" />
          <n-statistic label="失败" :value="failCount" />
        </n-space>
        <n-progress
          type="line"
          :percentage="progressPercent"
          :status="running ? 'info' : failCount > 0 ? 'warning' : 'success'"
          :show-indicator="true"
        />
      </n-space>
    </n-card>

    <n-card v-if="results.length > 0" class="page-card" title="请求结果" :bordered="false">
      <n-data-table
        :columns="resultColumns"
        :data="results"
        :row-key="(row: SimResult) => row.index"
        :max-height="400"
        :scrollbar-props="{ trigger: 'none' }"
      />
    </n-card>
  </page-section>
</template>
