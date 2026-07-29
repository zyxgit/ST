<script setup lang="ts">
import type { DataTableColumns } from 'naive-ui'
import {
  NButton,
  NCard,
  NDataTable,
  NForm,
  NFormItem,
  NInput,
  NInputNumber,
  NModal,
  NSpace,
  NTag,
} from 'naive-ui'
import { computed, h, onMounted, ref } from 'vue'

import PageSection from '@/components/common/PageSection.vue'
import ServiceUnavailableState from '@/components/common/ServiceUnavailableState.vue'
import TableActions from '@/components/common/TableActions.vue'
import { createSku, deductStock, getSkus, increaseStock } from '@/api/inventory'
import { PermissionCode } from '@/constants/permissions'
import { useAuthStore } from '@/stores/auth'
import { useDiscrete } from '@/lib/naive'
import type { SkuDto } from '@/types/inventory'

const authStore = useAuthStore()
const { message } = useDiscrete()

const canQuery = computed(() => authStore.hasPermission(PermissionCode.InventorySkuQuery))
const canCreate = computed(() => authStore.hasPermission(PermissionCode.InventorySkuCreate))
const canStock = computed(() => authStore.hasPermission(PermissionCode.InventorySkuStock))

const loading = ref(false)
const loadError = ref('')
const items = ref<SkuDto[]>([])

const columns: DataTableColumns<SkuDto> = [
  { title: 'SKU ID', key: 'skuId', minWidth: 280, ellipsis: { tooltip: true } },
  { title: '商品名称', key: 'productName', minWidth: 160, ellipsis: { tooltip: true } },
  {
    title: '可用库存',
    key: 'available',
    width: 100,
    align: 'right',
    render: (row: SkuDto) =>
      h(NTag, { type: row.available > 0 ? 'success' : 'error', size: 'small' }, { default: () => row.available }),
  },
  { title: '冻结', key: 'frozen', width: 80, align: 'right' },
  { title: '已售', key: 'sold', width: 80, align: 'right' },
  { title: '总库存', key: 'totalStock', width: 100, align: 'right' },
  {
    title: '操作',
    key: 'actions',
    width: 120,
    align: 'center',
    render: (row: SkuDto) => {
      const actions = []
      if (canStock.value) {
        actions.push(
          { key: 'increase', label: '补货', onClick: () => openIncreaseModal(row) },
          { key: 'deduct', label: '扣减', onClick: () => openDeductModal(row) },
        )
      }
      return h(TableActions, { actions })
    },
  },
]

// 创建 SKU 弹窗
const showCreateModal = ref(false)
const createForm = ref({ skuId: '', productName: '', initialStock: 0 })
const createLoading = ref(false)

function openCreateModal() {
  createForm.value = { skuId: '', productName: '', initialStock: 0 }
  showCreateModal.value = true
}

async function handleCreate() {
  if (!createForm.value.productName) {
    message.warning('请输入商品名称')
    return
  }
  if (createForm.value.initialStock <= 0) {
    message.warning('初始库存必须大于 0')
    return
  }

  createLoading.value = true
  try {
    await createSku({
      skuId: createForm.value.skuId || crypto.randomUUID(),
      productName: createForm.value.productName,
      initialStock: createForm.value.initialStock,
    })
    message.success('SKU 创建成功')
    showCreateModal.value = false
    await loadData()
  } catch {
    message.error('创建失败')
  } finally {
    createLoading.value = false
  }
}

// 增加库存弹窗
const showIncreaseModal = ref(false)
const increaseTarget = ref<SkuDto | null>(null)
const increaseQuantity = ref(1)
const increaseLoading = ref(false)

function openIncreaseModal(sku: SkuDto) {
  increaseTarget.value = sku
  increaseQuantity.value = 1
  showIncreaseModal.value = true
}

async function handleIncrease() {
  if (!increaseTarget.value || increaseQuantity.value <= 0) {
    message.warning('数量必须大于 0')
    return
  }

  increaseLoading.value = true
  try {
    await increaseStock(increaseTarget.value.skuId, increaseQuantity.value)
    message.success('库存已增加')
    showIncreaseModal.value = false
    await loadData()
  } catch {
    message.error('操作失败')
  } finally {
    increaseLoading.value = false
  }
}

// 扣减库存弹窗
const showDeductModal = ref(false)
const deductTarget = ref<SkuDto | null>(null)
const deductQuantity = ref(1)
const deductLoading = ref(false)

function openDeductModal(sku: SkuDto) {
  deductTarget.value = sku
  deductQuantity.value = 1
  showDeductModal.value = true
}

async function handleDeduct() {
  if (!deductTarget.value || deductQuantity.value <= 0) {
    message.warning('数量必须大于 0')
    return
  }

  deductLoading.value = true
  try {
    await deductStock(deductTarget.value.skuId, deductQuantity.value)
    message.success('库存已扣减')
    showDeductModal.value = false
    await loadData()
  } catch {
    message.error('操作失败')
  } finally {
    deductLoading.value = false
  }
}

async function loadData() {
  loading.value = true
  try {
    items.value = await getSkus()
    loadError.value = ''
  } catch {
    loadError.value = 'SKU 列表加载失败，请确认后台接口已启动后重试。'
    items.value = []
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  if (canQuery.value) {
    loadData()
  }
})
</script>

<template>
  <page-section title="SKU 管理" description="管理商品 SKU 和库存，支持创建 SKU、补货和查看库存状态。">
    <template v-if="canQuery">
      <n-card class="page-card" :bordered="false">
        <n-space>
          <n-button v-if="canCreate" type="primary" @click="openCreateModal">创建 SKU</n-button>
          <n-button @click="loadData">刷新</n-button>
        </n-space>
      </n-card>

      <n-card class="page-card" :bordered="false">
        <n-data-table
          v-if="!loadError"
          :columns="columns"
          :data="items"
          :loading="loading"
          :row-key="(row: SkuDto) => row.skuId"
        />
        <service-unavailable-state v-else :description="loadError" @retry="loadData" />
      </n-card>

      <!-- 创建 SKU 弹窗 -->
      <n-modal v-model:show="showCreateModal" preset="card" title="创建 SKU" style="max-width: 480px">
        <n-form label-placement="left" label-width="80">
          <n-form-item label="SKU ID">
            <n-input v-model:value="createForm.skuId" placeholder="留空自动生成 UUID" />
          </n-form-item>
          <n-form-item label="商品名称">
            <n-input v-model:value="createForm.productName" placeholder="请输入商品名称" />
          </n-form-item>
          <n-form-item label="初始库存">
            <n-input-number v-model:value="createForm.initialStock" :min="1" style="width: 100%" />
          </n-form-item>
        </n-form>
        <template #footer>
          <n-space justify="end">
            <n-button @click="showCreateModal = false">取消</n-button>
            <n-button type="primary" :loading="createLoading" @click="handleCreate">确认创建</n-button>
          </n-space>
        </template>
      </n-modal>

      <!-- 增加库存弹窗 -->
      <n-modal v-model:show="showIncreaseModal" preset="card" title="补货" style="max-width: 400px">
        <template v-if="increaseTarget">
          <div style="margin-bottom: 12px">
            商品：<strong>{{ increaseTarget.productName }}</strong>
          </div>
          <div style="margin-bottom: 16px">
            当前可用：<n-tag type="success" size="small">{{ increaseTarget.available }}</n-tag>
          </div>
          <n-form label-placement="left" label-width="60">
            <n-form-item label="数量">
              <n-input-number v-model:value="increaseQuantity" :min="1" style="width: 100%" />
            </n-form-item>
          </n-form>
        </template>
        <template #footer>
          <n-space justify="end">
            <n-button @click="showIncreaseModal = false">取消</n-button>
            <n-button type="primary" :loading="increaseLoading" @click="handleIncrease">确认补货</n-button>
          </n-space>
        </template>
      </n-modal>

      <!-- 扣减库存弹窗 -->
      <n-modal v-model:show="showDeductModal" preset="card" title="扣减库存" style="max-width: 400px">
        <template v-if="deductTarget">
          <div style="margin-bottom: 12px">
            商品：<strong>{{ deductTarget.productName }}</strong>
          </div>
          <div style="margin-bottom: 16px">
            当前可用：<n-tag type="success" size="small">{{ deductTarget.available }}</n-tag>
          </div>
          <n-form label-placement="left" label-width="60">
            <n-form-item label="数量">
              <n-input-number v-model:value="deductQuantity" :min="1" :max="deductTarget.available" style="width: 100%" />
            </n-form-item>
          </n-form>
        </template>
        <template #footer>
          <n-space justify="end">
            <n-button @click="showDeductModal = false">取消</n-button>
            <n-button type="error" :loading="deductLoading" @click="handleDeduct">确认扣减</n-button>
          </n-space>
        </template>
      </n-modal>
    </template>
  </page-section>
</template>
