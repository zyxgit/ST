<script setup lang="ts">
import type { DataTableColumns } from 'naive-ui'
import { NButton, NCard, NCode, NDataTable, NDatePicker, NDescriptions, NDescriptionsItem, NDrawer, NDrawerContent, NInput, NPagination, NSelect, NSpace, NTag } from 'naive-ui'
import { computed, h, onMounted, reactive, ref } from 'vue'

import PageSection from '@/components/common/PageSection.vue'
import ServiceUnavailableState from '@/components/common/ServiceUnavailableState.vue'
import TableActions from '@/components/common/TableActions.vue'
import { batchReplayDeadLetters, getDeadLetterDetail, getDeadLetters, replayDeadLetter } from '@/api/dead-letter'
import { PermissionCode } from '@/constants/permissions'
import { formatDateTime } from '@/lib/dayjs'
import { useAuthStore } from '@/stores/auth'
import { useDiscrete } from '@/lib/naive'
import type { DeadLetterDetail, DeadLetterListItem } from '@/types/dead-letter'

const authStore = useAuthStore()
const { message } = useDiscrete()

const canQuery = computed(() => authStore.hasPermission(PermissionCode.DeadLetterQuery))
const canReplay = computed(() => authStore.hasPermission(PermissionCode.DeadLetterReplay))

const loading = ref(false)
const loadError = ref('')
const items = ref<DeadLetterListItem[]>([])
const totalCount = ref(0)
const detail = ref<DeadLetterDetail | null>(null)
const showDrawer = ref(false)
const selectedRowKeys = ref<(string | number)[]>([])
const replaying = ref(false)

const query = reactive({
  queueName: '',
  isReplayed: null as 'true' | 'false' | null,
  pageIndex: 1,
  pageSize: 10,
  range: null as [number, number] | null,
})

const columns: DataTableColumns<DeadLetterListItem> = [
  { title: '队列', key: 'queueName', ellipsis: { tooltip: true }, width: 180 },
  { title: '交换机', key: 'exchangeName', ellipsis: { tooltip: true }, width: 150 },
  { title: '路由键', key: 'routingKey', width: 130 },
  {
    title: '错误信息',
    key: 'errorMessage',
    ellipsis: { tooltip: true },
    width: 220,
    render: (row: DeadLetterListItem) => row.errorMessage || '-',
  },
  {
    title: '重试',
    key: 'retryCount',
    width: 80,
    render: (row: DeadLetterListItem) => `${row.retryCount}/${row.maxRetryCount}`,
  },
  {
    title: '进入死信时间',
    key: 'createdAtUtc',
    width: 170,
    render: (row: DeadLetterListItem) => formatDateTime(row.createdAtUtc),
  },
  {
    title: '状态',
    key: 'isReplayed',
    width: 90,
    render: (row: DeadLetterListItem) =>
      h(NTag, { type: row.isReplayed ? 'success' : 'warning', size: 'small' }, { default: () => (row.isReplayed ? '已重放' : '待处理') }),
  },
  {
    title: '操作',
    key: 'actions',
    width: 140,
    align: 'center',
    render: (row: DeadLetterListItem) =>
      h(TableActions, {
        actions: [
          { key: 'detail', label: '详情', onClick: () => openDetail(row.id) },
          ...(canReplay.value && !row.isReplayed
            ? [{ key: 'replay', label: '重放', onClick: () => handleReplay(row) }]
            : []),
        ],
      }),
  },
]

async function loadData() {
  loading.value = true
  selectedRowKeys.value = []

  try {
    const result = await getDeadLetters({
      pageIndex: query.pageIndex,
      pageSize: query.pageSize,
      queueName: query.queueName || undefined,
      isReplayed: query.isReplayed === null ? undefined : query.isReplayed === 'true',
      startTime: query.range ? new Date(query.range[0]).toISOString() : undefined,
      endTime: query.range ? new Date(query.range[1]).toISOString() : undefined,
    })
    items.value = result.items
    totalCount.value = result.totalCount
    loadError.value = ''
  } catch {
    loadError.value = '死信队列加载失败，请确认后台接口已启动后重试。'
    items.value = []
    totalCount.value = 0
  } finally {
    loading.value = false
  }
}

async function openDetail(id: string) {
  detail.value = await getDeadLetterDetail(id)
  showDrawer.value = true
}

async function handleReplay(row: DeadLetterListItem) {
  replaying.value = true
  try {
    const result = await replayDeadLetter(row.id)
    if (result.success) {
      message.success('重放成功')
      await loadData()
    } else {
      message.error('重放失败')
    }
  } catch {
    message.error('重放请求失败')
  } finally {
    replaying.value = false
  }
}

async function handleBatchReplay() {
  if (selectedRowKeys.value.length === 0) {
    message.warning('请先勾选要重放的死信消息')
    return
  }

  replaying.value = true
  try {
    const result = await batchReplayDeadLetters(selectedRowKeys.value.map(String))
    message.success(`批量重放完成：成功 ${result.replayed} 条，失败 ${result.failed} 条`)
    await loadData()
  } catch {
    message.error('批量重放请求失败')
  } finally {
    replaying.value = false
  }
}

function handleSelectionChange(keys: (string | number)[]) {
  selectedRowKeys.value = keys
}

onMounted(() => {
  if (canQuery.value) {
    loadData()
  }
})
</script>

<template>
  <page-section title="死信队列" description="管理消费失败的消息，支持查询、查看详情和重放操作。">
    <template v-if="canQuery">
      <n-card class="page-card" :bordered="false">
        <n-space>
          <n-input v-model:value="query.queueName" clearable placeholder="队列名称" />
          <n-select
            v-model:value="query.isReplayed"
            clearable
            placeholder="重放状态"
            :options="[
              { label: '待处理', value: 'false' },
              { label: '已重放', value: 'true' },
            ]"
            style="width: 140px"
          />
          <n-date-picker v-model:value="query.range" clearable type="datetimerange" />
          <n-button type="primary" @click="loadData">查询</n-button>
          <n-button
            v-if="canReplay"
            :disabled="selectedRowKeys.length === 0 || replaying"
            :loading="replaying"
            @click="handleBatchReplay"
          >
            批量重放 ({{ selectedRowKeys.length }})
          </n-button>
        </n-space>
      </n-card>

      <n-card class="page-card" :bordered="false">
        <n-data-table
          v-if="!loadError"
          :columns="columns"
          :data="items"
          :loading="loading"
          :row-key="(row: DeadLetterListItem) => row.id"
          :checked-row-keys="selectedRowKeys"
          @update:checked-row-keys="handleSelectionChange"
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

      <n-drawer v-model:show="showDrawer" :width="720">
        <n-drawer-content title="死信消息详情">
          <n-descriptions v-if="detail" label-placement="top" :column="2">
            <n-descriptions-item label="队列">{{ detail.queueName }}</n-descriptions-item>
            <n-descriptions-item label="交换机">{{ detail.exchangeName }}</n-descriptions-item>
            <n-descriptions-item label="路由键">{{ detail.routingKey }}</n-descriptions-item>
            <n-descriptions-item label="重试次数">{{ detail.retryCount }} / {{ detail.maxRetryCount }}</n-descriptions-item>
            <n-descriptions-item label="进入死信时间">{{ formatDateTime(detail.createdAtUtc) }}</n-descriptions-item>
            <n-descriptions-item label="消息时间">{{ formatDateTime(detail.messageCreatedAtUtc) }}</n-descriptions-item>
            <n-descriptions-item label="状态">
              <n-tag :type="detail.isReplayed ? 'success' : 'warning'" size="small">
                {{ detail.isReplayed ? '已重放' : '待处理' }}
              </n-tag>
            </n-descriptions-item>
            <n-descriptions-item v-if="detail.isReplayed" label="重放时间">{{ formatDateTime(detail.replayedAtUtc) }}</n-descriptions-item>
            <n-descriptions-item label="错误信息" :span="2">{{ detail.errorMessage || '-' }}</n-descriptions-item>
            <n-descriptions-item v-if="detail.replayResult" label="重放结果" :span="2">{{ detail.replayResult }}</n-descriptions-item>
          </n-descriptions>

          <n-space vertical style="margin-top: 20px">
            <div style="font-weight: 600; margin-bottom: 4px">原始消息</div>
            <n-code :code="detail?.originalMessage || '{}'" language="json" show-line-numbers />

            <template v-if="detail?.errorStackTrace">
              <div style="font-weight: 600; margin-bottom: 4px; margin-top: 16px">错误堆栈</div>
              <n-code :code="detail.errorStackTrace" language="text" show-line-numbers />
            </template>
          </n-space>

          <template #footer>
            <n-space justify="end">
              <n-button @click="showDrawer = false">关闭</n-button>
              <n-button
                v-if="canReplay && detail && !detail.isReplayed"
                type="primary"
                :loading="replaying"
                @click="detail && handleReplay(detail)"
              >
                重放
              </n-button>
            </n-space>
          </template>
        </n-drawer-content>
      </n-drawer>
    </template>
  </page-section>
</template>
