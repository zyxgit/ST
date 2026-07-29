<script setup lang="ts">
import type { DataTableColumns } from 'naive-ui'
import { NButton, NCard, NDataTable, NInput, NSpace, NTag } from 'naive-ui'
import { computed, h, ref } from 'vue'

import PageSection from '@/components/common/PageSection.vue'
import { getPayment, getPaymentByOrderNo } from '@/api/payment'
import { PermissionCode } from '@/constants/permissions'
import { formatDateTime } from '@/lib/dayjs'
import { useAuthStore } from '@/stores/auth'
import { useDiscrete } from '@/lib/naive'
import type { PaymentDto } from '@/types/payment'

const authStore = useAuthStore()
const { message } = useDiscrete()

const canQuery = computed(() => authStore.hasPermission(PermissionCode.PaymentRecordQuery))

const loading = ref(false)
const orderId = ref('')
const payment = ref<PaymentDto | null>(null)
const notFound = ref(false)

const guidRegex = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i

const columns: DataTableColumns<PaymentDto> = [
  { title: '支付 ID', key: 'id', minWidth: 280, ellipsis: { tooltip: true } },
  { title: '订单 ID', key: 'orderId', minWidth: 280, ellipsis: { tooltip: true } },
  { title: '订单号', key: 'orderNo', minWidth: 200, ellipsis: { tooltip: true } },
  {
    title: '金额',
    key: 'amount',
    width: 120,
    align: 'right',
    render: (row: PaymentDto) => `¥${row.amount.toFixed(2)}`,
  },
  {
    title: '状态',
    key: 'status',
    width: 100,
    render: (row: PaymentDto) => {
      const typeMap: Record<string, 'success' | 'error' | 'warning' | 'default'> = {
        Succeeded: 'success',
        Failed: 'error',
        Pending: 'warning',
      }
      return h(NTag, { type: typeMap[row.status] ?? 'default', size: 'small' }, { default: () => row.status })
    },
  },
  {
    title: '失败原因',
    key: 'failureReason',
    minWidth: 160,
    ellipsis: { tooltip: true },
    render: (row: PaymentDto) => row.failureReason || '-',
  },
  {
    title: '创建时间',
    key: 'createTime',
    width: 170,
    render: (row: PaymentDto) => formatDateTime(row.createTime),
  },
]

async function handleSearch() {
  const input = orderId.value.trim()
  if (!input) {
    message.warning('请输入订单号或订单 ID')
    return
  }

  loading.value = true
  notFound.value = false
  payment.value = null

  try {
    payment.value = guidRegex.test(input)
      ? await getPayment(input)
      : await getPaymentByOrderNo(input)
  } catch {
    notFound.value = true
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <page-section title="支付记录" description="按订单号或订单 ID 查询支付记录，查看支付状态和详情。">
    <template v-if="canQuery">
      <n-card class="page-card" :bordered="false">
        <n-space>
          <n-input
            v-model:value="orderId"
            clearable
            placeholder="输入订单号或订单 ID"
            style="width: 360px"
            @keyup.enter="handleSearch"
          />
          <n-button type="primary" :loading="loading" @click="handleSearch">查询</n-button>
        </n-space>
      </n-card>

      <n-card v-if="payment" class="page-card" :bordered="false">
        <n-data-table
          :columns="columns"
          :data="[payment]"
          :row-key="(row: PaymentDto) => row.id"
        />
      </n-card>

      <n-card v-if="notFound" class="page-card" :bordered="false">
        <div style="text-align: center; padding: 24px; color: var(--text-3)">
          未找到该订单的支付记录
        </div>
      </n-card>
    </template>
  </page-section>
</template>
