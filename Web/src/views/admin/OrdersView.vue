<script setup lang="ts">
import type { DataTableColumns } from 'naive-ui'
import {
  NButton,
  NCard,
  NDataTable,
  NDescriptions,
  NDescriptionsItem,
  NDrawer,
  NDrawerContent,
  NInput,
  NPagination,
  NSelect,
  NSpace,
  NTag,
} from 'naive-ui'
import { computed, h, onMounted, reactive, ref } from 'vue'

import PageSection from '@/components/common/PageSection.vue'
import ServiceUnavailableState from '@/components/common/ServiceUnavailableState.vue'
import TableActions from '@/components/common/TableActions.vue'
import { cancelOrder, getOrders } from '@/api/order'
import { mockFail, mockPay } from '@/api/payment'
import { PermissionCode } from '@/constants/permissions'
import { formatDateTime } from '@/lib/dayjs'
import { useAuthStore } from '@/stores/auth'
import { useDiscrete } from '@/lib/naive'
import type { OrderDto, OrderItemDto } from '@/types/order'
import { OrderStatusMap } from '@/types/order'

const authStore = useAuthStore()
const { message, dialog } = useDiscrete()

const canQuery = computed(() => authStore.hasPermission(PermissionCode.OrderQuery))
const canCancel = computed(() => authStore.hasPermission(PermissionCode.OrderCancel))

const loading = ref(false)
const loadError = ref('')
const items = ref<OrderDto[]>([])
const totalCount = ref(0)

const query = reactive({
  orderNo: '',
  status: null as number | null,
  pageIndex: 1,
  pageSize: 10,
})

const statusOptions = Object.entries(OrderStatusMap).map(([value, { label }]) => ({
  label,
  value: Number(value),
}))

const columns: DataTableColumns<OrderDto> = [
  { title: '订单号', key: 'orderNo', minWidth: 200, ellipsis: { tooltip: true } },
  { title: '用户 ID', key: 'userId', width: 280, ellipsis: { tooltip: true } },
  {
    title: '总金额',
    key: 'totalAmount',
    width: 100,
    align: 'right',
    render: (row: OrderDto) => `¥${row.totalAmount.toFixed(2)}`,
  },
  {
    title: '状态',
    key: 'status',
    width: 110,
    render: (row: OrderDto) => {
      const info = OrderStatusMap[row.status] ?? { label: '未知', type: 'default' as const }
      return h(NTag, { type: info.type, size: 'small' }, { default: () => info.label })
    },
  },
  {
    title: '创建时间',
    key: 'createTime',
    width: 170,
    render: (row: OrderDto) => formatDateTime(row.createTime),
  },
  {
    title: '操作',
    key: 'actions',
    width: 200,
    align: 'center',
    render: (row: OrderDto) => {
      const actions: { key: string; label: string; onClick: () => void | Promise<void>; type?: 'error' }[] = [
        { key: 'detail', label: '详情', onClick: () => showDetail(row) },
      ]

      if (row.status <= 1 && canCancel.value) {
        actions.push({ key: 'cancel', label: '取消', type: 'error', onClick: () => handleCancel(row) })
      }
      if (row.status === 1) {
        actions.push({ key: 'pay', label: '支付', onClick: () => handleMockPay(row) })
        actions.push({ key: 'fail', label: '支付失败', onClick: () => handleMockFail(row) })
      }

      return h(TableActions, { actions })
    },
  },
]

const detailDrawer = ref(false)
const detailOrder = ref<OrderDto | null>(null)

function showDetail(order: OrderDto) {
  detailOrder.value = order
  detailDrawer.value = true
}

async function loadData() {
  loading.value = true
  try {
    const result = await getOrders({
      pageIndex: query.pageIndex,
      pageSize: query.pageSize,
      orderNo: query.orderNo || undefined,
      status: query.status ?? undefined,
    })
    items.value = result.items
    totalCount.value = result.totalCount
    loadError.value = ''
  } catch {
    loadError.value = '订单列表加载失败，请确认后台接口已启动后重试。'
    items.value = []
    totalCount.value = 0
  } finally {
    loading.value = false
  }
}

function handleCancel(row: OrderDto) {
  dialog.warning({
    title: '确认取消',
    content: `确定要取消订单「${row.orderNo}」吗？`,
    positiveText: '取消订单',
    negativeText: '返回',
    onPositiveClick: async () => {
      try {
        await cancelOrder(row.id)
        message.success('订单已取消')
        await loadData()
      } catch {
        message.error('取消失败')
      }
    },
  })
}

async function handleMockPay(row: OrderDto) {
  try {
    await mockPay(row.id)
    message.success('模拟支付成功')
    await loadData()
  } catch {
    message.error('操作失败')
  }
}

async function handleMockFail(row: OrderDto) {
  dialog.warning({
    title: '确认模拟支付失败',
    content: `确定要模拟订单「${row.orderNo}」支付失败吗？`,
    positiveText: '确认',
    negativeText: '返回',
    onPositiveClick: async () => {
      try {
        await mockFail(row.id)
        message.success('模拟支付失败成功')
        await loadData()
      } catch {
        message.error('操作失败')
      }
    },
  })
}

function handleReset() {
  query.orderNo = ''
  query.status = null
  query.pageIndex = 1
  loadData()
}

onMounted(() => {
  if (canQuery.value) {
    loadData()
  }
})
</script>

<template>
  <page-section title="订单管理" description="查看订单列表，支持筛选状态、取消订单和模拟支付操作。">
    <template v-if="canQuery">
      <n-card class="page-card" :bordered="false">
        <n-space>
          <n-input v-model:value="query.orderNo" clearable placeholder="订单号搜索" @keyup.enter="loadData" />
          <n-select
            v-model:value="query.status"
            clearable
            placeholder="订单状态"
            :options="statusOptions"
            style="width: 140px"
          />
          <n-button type="primary" @click="loadData">查询</n-button>
          <n-button @click="handleReset">重置</n-button>
        </n-space>
      </n-card>

      <n-card class="page-card" :bordered="false">
        <n-data-table
          v-if="!loadError"
          :columns="columns"
          :data="items"
          :loading="loading"
          :row-key="(row: OrderDto) => row.id"
        />
        <service-unavailable-state v-else :description="loadError" @retry="loadData" />
        <div v-if="!loadError" style="display: flex; justify-content: flex-end; margin-top: 16px">
          <n-pagination
            v-model:page="query.pageIndex"
            v-model:page-size="query.pageSize"
            :item-count="totalCount"
            show-size-picker
            @update:page="loadData"
            @update:page-size="loadData"
          />
        </div>
      </n-card>

      <n-drawer v-model:show="detailDrawer" :width="560">
        <n-drawer-content title="订单详情" closable>
          <template v-if="detailOrder">
            <n-descriptions label-placement="left" bordered :column="1">
              <n-descriptions-item label="订单号">{{ detailOrder.orderNo }}</n-descriptions-item>
              <n-descriptions-item label="用户 ID">{{ detailOrder.userId }}</n-descriptions-item>
              <n-descriptions-item label="总金额">¥{{ detailOrder.totalAmount.toFixed(2) }}</n-descriptions-item>
              <n-descriptions-item label="状态">
                <n-tag :type="OrderStatusMap[detailOrder.status]?.type ?? 'default'" size="small">
                  {{ OrderStatusMap[detailOrder.status]?.label ?? '未知' }}
                </n-tag>
              </n-descriptions-item>
              <n-descriptions-item label="创建时间">{{ formatDateTime(detailOrder.createTime) }}</n-descriptions-item>
              <n-descriptions-item v-if="detailOrder.cancelReason" label="取消原因">
                {{ detailOrder.cancelReason }}
              </n-descriptions-item>
            </n-descriptions>

            <div style="margin-top: 16px; font-weight: 600">订单项</div>
            <n-data-table
              :columns="[
                { title: '商品名称', key: 'productName' },
                { title: 'SKU ID', key: 'skuId', ellipsis: { tooltip: true } },
                { title: '数量', key: 'quantity', width: 80, align: 'right' },
                { title: '单价', key: 'unitPrice', width: 100, align: 'right', render: (row: OrderItemDto) => `¥${row.unitPrice.toFixed(2)}` },
                { title: '小计', key: 'subtotal', width: 100, align: 'right', render: (row: OrderItemDto) => `¥${row.subtotal.toFixed(2)}` },
              ]"
              :data="detailOrder.items"
              :row-key="(row: OrderItemDto) => row.skuId"
              size="small"
              style="margin-top: 8px"
            />
          </template>
        </n-drawer-content>
      </n-drawer>
    </template>
  </page-section>
</template>
