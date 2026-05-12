<script setup lang="ts">
import type { DataTableColumns } from 'naive-ui'
import { NButton, NCard, NCode, NDataTable, NDatePicker, NDescriptions, NDescriptionsItem, NDrawer, NDrawerContent, NInput, NPagination, NSelect, NSpace, NTag } from 'naive-ui'
import { computed, h, onMounted, reactive, ref } from 'vue'

import PageSection from '@/components/common/PageSection.vue'
import ServiceUnavailableState from '@/components/common/ServiceUnavailableState.vue'
import TableActions from '@/components/common/TableActions.vue'
import { getOperationLogDetail, getOperationLogs } from '@/api/operation-log'
import { PermissionCode } from '@/constants/permissions'
import { formatDateTime } from '@/lib/dayjs'
import { useAuthStore } from '@/stores/auth'
import type { OperationLogDetail, OperationLogListItem } from '@/types/operation-log'

const authStore = useAuthStore()
const canQuery = computed(() => authStore.hasPermission(PermissionCode.OperationLogQuery))

const loading = ref(false)
const loadError = ref('')
const items = ref<OperationLogListItem[]>([])
const totalCount = ref(0)
const detail = ref<OperationLogDetail | null>(null)
const showDrawer = ref(false)

const query = reactive({
  keyword: '',
  serviceName: '',
  success: null as 'true' | 'false' | null,
  pageIndex: 1,
  pageSize: 10,
  range: null as [number, number] | null,
})

const columns: DataTableColumns<OperationLogListItem> = [
  { title: '服务', key: 'serviceName' },
  { title: '操作', key: 'operationName' },
  {
    title: '请求',
    key: 'path',
    render: (row: OperationLogListItem) => `${row.method} ${row.path}`,
  },
  {
    title: '状态',
    key: 'success',
    render: (row: OperationLogListItem) => h(NTag, { type: row.success ? 'success' : 'error' }, { default: () => (row.success ? '成功' : '失败') }),
  },
  {
    title: '耗时',
    key: 'durationMs',
    render: (row: OperationLogListItem) => `${row.durationMs} ms`,
  },
  {
    title: '时间',
    key: 'createdAtUtc',
    render: (row: OperationLogListItem) => formatDateTime(row.createdAtUtc),
  },
  {
    title: '操作',
    key: 'actions',
    width: 120,
    align: 'center',
    render: (row: OperationLogListItem) =>
      h(TableActions, {
        actions: [{ key: 'detail', label: '详情', onClick: () => openDetail(row.id) }],
      }),
  },
]

async function loadData() {
  loading.value = true

  try {
    const result = await getOperationLogs({
      pageIndex: query.pageIndex,
      pageSize: query.pageSize,
      keyword: query.keyword,
      serviceName: query.serviceName,
      success: query.success === null ? null : query.success === 'true',
      startTimeUtc: query.range ? new Date(query.range[0]).toISOString() : null,
      endTimeUtc: query.range ? new Date(query.range[1]).toISOString() : null,
    })
    items.value = result.items
    totalCount.value = result.totalCount
    loadError.value = ''
  } catch {
    loadError.value = '操作日志加载失败，请确认后台接口已启动后重试。'
    items.value = []
    totalCount.value = 0
  } finally {
    loading.value = false
  }
}

async function openDetail(id: number) {
  detail.value = await getOperationLogDetail(id)
  showDrawer.value = true
}

onMounted(() => {
  if (canQuery.value) {
    loadData()
  }
})
</script>

<template>
  <page-section title="操作日志" description="支持分页查询、按时间筛选和日志详情查看。">
    <template v-if="canQuery">
      <n-card class="page-card" :bordered="false">
        <n-space>
          <n-input v-model:value="query.keyword" clearable placeholder="关键字 / TraceId / 路径" />
          <n-input v-model:value="query.serviceName" clearable placeholder="服务名" />
          <n-date-picker v-model:value="query.range" clearable type="datetimerange" />
          <n-select
            v-model:value="query.success"
            clearable
              placeholder="执行结果"
              :options="[
              { label: '成功', value: 'true' },
              { label: '失败', value: 'false' },
            ]"
            style="width: 140px"
          />
          <n-button type="primary" @click="loadData">查询</n-button>
        </n-space>
      </n-card>

      <n-card class="page-card" :bordered="false">
        <n-data-table v-if="!loadError" :columns="columns" :data="items" :loading="loading" :row-key="(row: OperationLogListItem) => row.id" />
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

      <n-drawer v-model:show="showDrawer" :width="720">
        <n-drawer-content title="日志详情">
          <n-descriptions v-if="detail" label-placement="top" :column="2">
            <n-descriptions-item label="服务">{{ detail.serviceName }}</n-descriptions-item>
            <n-descriptions-item label="时间">{{ formatDateTime(detail.createdAtUtc) }}</n-descriptions-item>
            <n-descriptions-item label="操作">{{ detail.operationName }}</n-descriptions-item>
            <n-descriptions-item label="TraceId">{{ detail.traceId }}</n-descriptions-item>
            <n-descriptions-item label="请求">{{ detail.method }} {{ detail.path }}</n-descriptions-item>
            <n-descriptions-item label="耗时">{{ detail.durationMs }} ms</n-descriptions-item>
            <n-descriptions-item label="异常">{{ detail.exceptionMessage || '-' }}</n-descriptions-item>
            <n-descriptions-item label="状态">{{ detail.statusCode }}</n-descriptions-item>
          </n-descriptions>

          <n-space vertical style="margin-top: 20px">
            <n-code :code="detail?.requestJson || '{}'" language="json" show-line-numbers />
            <n-code :code="detail?.responseJson || '{}'" language="json" show-line-numbers />
          </n-space>
        </n-drawer-content>
      </n-drawer>
    </template>
  </page-section>
</template>
