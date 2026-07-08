<script setup lang="ts">
import type { DataTableColumns } from 'naive-ui'
import { NButton, NCard, NDataTable, NInput, NPagination, NSelect, NSpace, NTag } from 'naive-ui'
import { computed, h, onMounted, reactive, ref } from 'vue'

import PageSection from '@/components/common/PageSection.vue'
import ServiceUnavailableState from '@/components/common/ServiceUnavailableState.vue'
import TableActions from '@/components/common/TableActions.vue'
import { deleteFile, getFiles } from '@/api/file'
import { PermissionCode } from '@/constants/permissions'
import { formatDateTime } from '@/lib/dayjs'
import { useAuthStore } from '@/stores/auth'
import { useDiscrete } from '@/lib/naive'
import type { FileListItem } from '@/types/file'

const authStore = useAuthStore()
const { message, dialog } = useDiscrete()

const canQuery = computed(() => authStore.hasPermission(PermissionCode.FileQuery))
const canDelete = computed(() => authStore.hasPermission(PermissionCode.FileDelete))

const loading = ref(false)
const loadError = ref('')
const items = ref<FileListItem[]>([])
const totalCount = ref(0)

const query = reactive({
  keyword: '',
  accessLevel: null as number | null,
  contentType: '',
  pageIndex: 1,
  pageSize: 10,
})

const accessLevelOptions = [
  { label: '公开', value: 0 },
  { label: '私有', value: 1 },
]

function formatFileSize(bytes: number): string {
  if (bytes === 0) return '0 B'
  const units = ['B', 'KB', 'MB', 'GB', 'TB']
  const i = Math.floor(Math.log(bytes) / Math.log(1024))
  return `${(bytes / 1024 ** i).toFixed(i === 0 ? 0 : 1)} ${units[i]}`
}

const columns: DataTableColumns<FileListItem> = [
  { title: '文件名', key: 'fileName', ellipsis: { tooltip: true }, minWidth: 200 },
  {
    title: '大小',
    key: 'fileSize',
    width: 100,
    render: (row: FileListItem) => formatFileSize(row.fileSize),
  },
  { title: '类型', key: 'contentType', ellipsis: { tooltip: true }, width: 150 },
  {
    title: '访问级别',
    key: 'accessLevel',
    width: 90,
    render: (row: FileListItem) =>
      h(NTag, { type: row.accessLevel === 0 ? 'success' : 'default', size: 'small' }, { default: () => (row.accessLevel === 0 ? '公开' : '私有') }),
  },
  { title: '上传者', key: 'uploaderName', width: 120, render: (row: FileListItem) => row.uploaderName || '-' },
  {
    title: '上传时间',
    key: 'createTime',
    width: 170,
    render: (row: FileListItem) => formatDateTime(row.createTime),
  },
  {
    title: '操作',
    key: 'actions',
    width: 140,
    align: 'center',
    render: (row: FileListItem) =>
      h(TableActions, {
        actions: [
          { key: 'download', label: '下载', onClick: () => handleDownload(row) },
          ...(canDelete.value ? [{ key: 'delete', label: '删除', onClick: () => handleDelete(row) }] : []),
        ],
      }),
  },
]

async function loadData() {
  loading.value = true
  try {
    const result = await getFiles({
      pageIndex: query.pageIndex,
      pageSize: query.pageSize,
      keyword: query.keyword || undefined,
      accessLevel: query.accessLevel ?? undefined,
      contentType: query.contentType || undefined,
    })
    items.value = result.items
    totalCount.value = result.totalCount
    loadError.value = ''
  } catch {
    loadError.value = '文件列表加载失败，请确认后台接口已启动后重试。'
    items.value = []
    totalCount.value = 0
  } finally {
    loading.value = false
  }
}

function handleDownload(row: FileListItem) {
  const baseUrl = import.meta.env.VITE_API_BASE_URL || ''
  window.open(`${baseUrl}${row.url}`, '_blank')
}

function handleDelete(row: FileListItem) {
  dialog.warning({
    title: '确认删除',
    content: `确定要删除文件「${row.fileName}」吗？此操作不可撤销。`,
    positiveText: '删除',
    negativeText: '取消',
    onPositiveClick: async () => {
      try {
        await deleteFile(row.id)
        message.success('删除成功')
        await loadData()
      } catch {
        // 错误提示已由 axios 拦截器统一处理
      }
    },
  })
}

function handleReset() {
  query.keyword = ''
  query.accessLevel = null
  query.contentType = ''
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
  <page-section title="文件管理" description="查看和管理已上传的文件，支持搜索、下载和删除操作。">
    <template v-if="canQuery">
      <n-card class="page-card" :bordered="false">
        <n-space>
          <n-input v-model:value="query.keyword" clearable placeholder="文件名搜索" @keyup.enter="loadData" />
          <n-select
            v-model:value="query.accessLevel"
            clearable
            placeholder="访问级别"
            :options="accessLevelOptions"
            style="width: 120px"
          />
          <n-input v-model:value="query.contentType" clearable placeholder="MIME 类型（如 image/）" style="width: 200px" />
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
          :row-key="(row: FileListItem) => row.id"
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
    </template>
  </page-section>
</template>
