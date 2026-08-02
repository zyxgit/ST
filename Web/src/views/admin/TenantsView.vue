<script setup lang="ts">
import type { DataTableColumns, FormInst, FormRules } from 'naive-ui'
import {
  NButton,
  NCard,
  NDataTable,
  NDatePicker,
  NDrawer,
  NDrawerContent,
  NForm,
  NFormItem,
  NInput,
  NInputNumber,
  NPagination,
  NSelect,
  NSpace,
  NTag,
} from 'naive-ui'
import { computed, h, nextTick, onMounted, reactive, ref } from 'vue'

import PageSection from '@/components/common/PageSection.vue'
import ServiceUnavailableState from '@/components/common/ServiceUnavailableState.vue'
import TableActions from '@/components/common/TableActions.vue'
import {
  activateTenant,
  addTenantUser,
  createTenant,
  deleteTenant,
  getTenantDetail,
  getTenantQuota,
  getTenantUsers,
  getTenants,
  removeTenantUser,
  suspendTenant,
  updateTenant,
  updateTenantQuota,
} from '@/api/tenant'
import { getUsers } from '@/api/user'
import { PermissionCode } from '@/constants/permissions'
import { formatDateTime } from '@/lib/dayjs'
import { codeRule, requiredRule } from '@/lib/form-rules'
import { useDiscrete } from '@/lib/naive'
import { useAuthStore } from '@/stores/auth'
import type { TenantListItem, TenantUser } from '@/types/tenant'

const { message, dialog } = useDiscrete()
const authStore = useAuthStore()

// ======================== 常量 ========================
const statusOptions = [
  { label: '正常', value: 'Active' },
  { label: '已暂停', value: 'Suspended' },
  { label: '已注销', value: 'Deleted' },
]

const statusTagMap: Record<string, { label: string; type: 'success' | 'warning' | 'error' }> = {
  Active: { label: '正常', type: 'success' },
  Suspended: { label: '已暂停', type: 'warning' },
  Deleted: { label: '已注销', type: 'error' },
}

const packageOptions = [
  { label: '基础版', value: 'basic' },
  { label: '专业版', value: 'pro' },
  { label: '企业版', value: 'enterprise' },
]

const roleInTenantOptions = [
  { label: '所有者 (owner)', value: 'owner' },
  { label: '管理员 (admin)', value: 'admin' },
  { label: '成员 (member)', value: 'member' },
]

// ======================== 权限 ========================
const canQuery = computed(() => authStore.hasPermission(PermissionCode.TenantQuery))
const canCreate = computed(() => authStore.hasPermission(PermissionCode.TenantCreate))
const canUpdate = computed(() => authStore.hasPermission(PermissionCode.TenantUpdate))
const canDelete = computed(() => authStore.hasPermission(PermissionCode.TenantDelete))
const canManageUser = computed(() => authStore.hasPermission(PermissionCode.TenantUser))
const canManageQuota = computed(() => authStore.hasPermission(PermissionCode.TenantQuota))

// ======================== 列表 ========================
const loading = ref(false)
const loadError = ref('')
const items = ref<TenantListItem[]>([])
const totalCount = ref(0)

const query = reactive({
  keyword: '',
  status: null as string | null,
  pageIndex: 1,
  pageSize: 10,
})

const columns = computed<DataTableColumns<TenantListItem>>(() => [
  { title: '编码', key: 'code', width: 120 },
  { title: '名称', key: 'name', width: 160 },
  {
    title: '状态',
    key: 'status',
    width: 80,
    render: (row) => {
      const tag = statusTagMap[row.status] || { label: row.status, type: 'default' as const }
      return h(NTag, { type: tag.type, size: 'small' }, { default: () => tag.label })
    },
  },
  {
    title: '套餐',
    key: 'packageId',
    width: 100,
    render: (row) => {
      const pkg = packageOptions.find((p) => p.value === row.packageId)
      return pkg ? pkg.label : (row.packageId || '-')
    },
  },
  {
    title: '过期时间',
    key: 'expireAtUtc',
    width: 160,
    render: (row) => formatDateTime(row.expireAtUtc),
  },
  { title: '用户数', key: 'userCount', width: 80, align: 'center' },
  {
    title: '创建时间',
    key: 'createTime',
    width: 160,
    render: (row) => formatDateTime(row.createTime),
  },
  {
    title: '操作',
    key: 'actions',
    width: 200,
    align: 'center',
    render: (row) =>
      h(TableActions, {
        actions: [
          ...(canUpdate.value
            ? [{ key: 'edit', label: '编辑', onClick: () => openEditDrawer(row.id) }]
            : []),
          ...(canDelete.value && row.status !== 'Deleted'
            ? [{ key: 'delete', label: '删除', type: 'error' as const, onClick: () => handleDelete(row.id) }]
            : []),
        ],
        moreActions: [
          ...(canUpdate.value && row.status !== 'Active'
            ? [{ key: 'activate', label: '激活', onClick: () => handleActivate(row.id) }]
            : []),
          ...(canUpdate.value && row.status === 'Active'
            ? [{ key: 'suspend', label: '暂停', onClick: () => handleSuspend(row.id) }]
            : []),
          ...(canManageUser.value
            ? [{ key: 'users', label: '用户管理', onClick: () => openUsersDrawer(row.id) }]
            : []),
          ...(canManageQuota.value
            ? [{ key: 'quota', label: '配额管理', onClick: () => openQuotaDrawer(row.id) }]
            : []),
        ],
      }),
  },
])

async function loadData() {
  loading.value = true
  try {
    const result = await getTenants(query)
    items.value = result.items
    totalCount.value = result.totalCount
    loadError.value = ''
  } catch {
    loadError.value = '租户列表加载失败，请确认后台接口已启动后重试。'
    items.value = []
    totalCount.value = 0
  } finally {
    loading.value = false
  }
}

// ======================== 新增/编辑 Drawer ========================
const showFormDrawer = ref(false)
const editingId = ref<string | null>(null)
const formRef = ref<FormInst | null>(null)

const formValue = reactive({
  code: '',
  name: '',
  packageId: null as string | null,
  expireAtUtc: null as number | null,
})

const rules = computed<FormRules>(() => ({
  code: [codeRule('租户编码')],
  name: [requiredRule('租户名称')],
}))

function resetForm() {
  editingId.value = null
  formValue.code = ''
  formValue.name = ''
  formValue.packageId = null
  formValue.expireAtUtc = null
}

function openCreateDrawer() {
  resetForm()
  showFormDrawer.value = true
  void nextTick(() => formRef.value?.restoreValidation())
}

async function openEditDrawer(id: string) {
  const detail = await getTenantDetail(id)
  editingId.value = id
  formValue.code = detail.code
  formValue.name = detail.name
  formValue.packageId = detail.packageId || null
  formValue.expireAtUtc = detail.expireAtUtc ? new Date(detail.expireAtUtc).getTime() : null
  showFormDrawer.value = true
  void nextTick(() => formRef.value?.restoreValidation())
}

async function handleFormSubmit() {
  try {
    await formRef.value?.validate()
  } catch {
    return
  }

  if (editingId.value) {
    await updateTenant(editingId.value, {
      name: formValue.name.trim(),
      packageId: formValue.packageId || null,
      expireAtUtc: formValue.expireAtUtc ? new Date(formValue.expireAtUtc).toISOString() : null,
    })
  } else {
    await createTenant({
      code: formValue.code.trim(),
      name: formValue.name.trim(),
    })
  }

  message.success(editingId.value ? '租户已更新' : '租户已创建')
  showFormDrawer.value = false
  await loadData()
}

// ======================== 状态操作 ========================
function handleActivate(id: string) {
  dialog.info({
    title: '激活租户',
    content: '确认激活该租户吗？',
    positiveText: '确认',
    negativeText: '取消',
    async onPositiveClick() {
      await activateTenant(id)
      message.success('租户已激活')
      await loadData()
    },
  })
}

function handleSuspend(id: string) {
  dialog.warning({
    title: '暂停租户',
    content: '暂停后该租户将无法访问系统，确认继续吗？',
    positiveText: '确认',
    negativeText: '取消',
    async onPositiveClick() {
      await suspendTenant(id)
      message.success('租户已暂停')
      await loadData()
    },
  })
}

function handleDelete(id: string) {
  dialog.error({
    title: '删除租户',
    content: '删除后不可恢复，确认继续吗？',
    positiveText: '删除',
    negativeText: '取消',
    async onPositiveClick() {
      await deleteTenant(id)
      message.success('租户已删除')
      await loadData()
    },
  })
}

// ======================== 用户管理 Drawer ========================
const showUsersDrawer = ref(false)
const usersTenantId = ref('')
const tenantUsers = ref<TenantUser[]>([])
const usersLoading = ref(false)
const showAddUserForm = ref(false)
const addUserForm = reactive({
  userId: '',
  roleInTenant: 'member',
})
const userOptions = ref<{ label: string; value: string }[]>([])
const userSearchLoading = ref(false)

async function openUsersDrawer(tenantId: string) {
  usersTenantId.value = tenantId
  showUsersDrawer.value = true
  showAddUserForm.value = false
  await loadTenantUsers()
}

async function loadTenantUsers() {
  usersLoading.value = true
  try {
    tenantUsers.value = await getTenantUsers(usersTenantId.value)
  } catch {
    tenantUsers.value = []
  } finally {
    usersLoading.value = false
  }
}

async function handleUserSearch(searchQuery: string) {
  if (!searchQuery.trim()) {
    userOptions.value = []
    return
  }
  userSearchLoading.value = true
  try {
    const result = await getUsers({ keyword: searchQuery, pageIndex: 1, pageSize: 20 })
    userOptions.value = result.items.map((u) => ({
      label: `${u.nickName} (${u.email})`,
      value: u.id,
    }))
  } catch {
    userOptions.value = []
  } finally {
    userSearchLoading.value = false
  }
}

async function handleAddUser() {
  if (!addUserForm.userId) {
    message.warning('请选择用户')
    return
  }
  await addTenantUser(usersTenantId.value, {
    userId: addUserForm.userId,
    roleInTenant: addUserForm.roleInTenant,
  })
  message.success('用户已添加')
  showAddUserForm.value = false
  addUserForm.userId = ''
  addUserForm.roleInTenant = 'member'
  await loadTenantUsers()
}

function handleRemoveUser(userId: string) {
  dialog.warning({
    title: '移除用户',
    content: '确认将该用户从租户中移除吗？',
    positiveText: '移除',
    negativeText: '取消',
    async onPositiveClick() {
      await removeTenantUser(usersTenantId.value, userId)
      message.success('用户已移除')
      await loadTenantUsers()
    },
  })
}

// ======================== 配额管理 Drawer ========================
const showQuotaDrawer = ref(false)
const quotaTenantId = ref('')
const quotaLoading = ref(false)

const quotaForm = reactive({
  maxUsers: 100,
  maxStorageBytes: 10,
  maxApiCallsPerDay: 100000,
  maxFileSize: 100,
  maxOrdersPerDay: 10000,
})

async function openQuotaDrawer(tenantId: string) {
  quotaTenantId.value = tenantId
  showQuotaDrawer.value = true
  quotaLoading.value = true
  try {
    const quota = await getTenantQuota(tenantId)
    quotaForm.maxUsers = quota.maxUsers
    quotaForm.maxStorageBytes = Math.round(quota.maxStorageBytes / (1024 * 1024 * 1024))
    quotaForm.maxApiCallsPerDay = quota.maxApiCallsPerDay
    quotaForm.maxFileSize = Math.round(quota.maxFileSize / (1024 * 1024))
    quotaForm.maxOrdersPerDay = quota.maxOrdersPerDay
  } catch {
    // 使用默认值
  } finally {
    quotaLoading.value = false
  }
}

async function handleQuotaSubmit() {
  await updateTenantQuota(quotaTenantId.value, {
    maxUsers: quotaForm.maxUsers,
    maxStorageBytes: quotaForm.maxStorageBytes * 1024 * 1024 * 1024,
    maxApiCallsPerDay: quotaForm.maxApiCallsPerDay,
    maxFileSize: quotaForm.maxFileSize * 1024 * 1024,
    maxOrdersPerDay: quotaForm.maxOrdersPerDay,
  })
  message.success('配额已更新')
  showQuotaDrawer.value = false
}

// ======================== 初始化 ========================
onMounted(async () => {
  if (canQuery.value) {
    await loadData()
  }
})
</script>

<template>
  <page-section title="租户管理" description="管理租户信息、状态、用户关联和资源配额。">
    <!-- 查询区域 -->
    <n-card class="page-card" :bordered="false">
      <n-space justify="space-between">
        <n-space>
          <n-input v-model:value="query.keyword" clearable placeholder="编码 / 名称" />
          <n-select
            v-model:value="query.status"
            clearable
            placeholder="状态"
            :options="statusOptions"
            style="width: 120px"
          />
          <n-button v-if="canQuery" type="primary" @click="loadData">查询</n-button>
        </n-space>
        <n-button v-if="canCreate" type="primary" @click="openCreateDrawer">新增租户</n-button>
      </n-space>
    </n-card>

    <!-- 列表 -->
    <n-card class="page-card" :bordered="false">
      <n-data-table
        v-if="canQuery && !loadError"
        :columns="columns"
        :data="items"
        :loading="loading"
        :row-key="(row: TenantListItem) => row.id"
      />
      <service-unavailable-state v-else-if="loadError" :description="loadError" @retry="loadData" />
      <div v-else style="color: var(--text-3)">当前账号没有查看租户列表的权限。</div>
      <div style="display: flex; justify-content: flex-end; margin-top: 16px">
        <n-pagination
          v-if="canQuery"
          v-model:page="query.pageIndex"
          v-model:page-size="query.pageSize"
          :item-count="totalCount"
          show-size-picker
          @update:page="loadData"
          @update:page-size="loadData"
        />
      </div>
    </n-card>

    <!-- 新增/编辑 Drawer -->
    <n-drawer v-model:show="showFormDrawer" :width="500" placement="right">
      <n-drawer-content :title="editingId ? '编辑租户' : '新增租户'" body-content-style="padding-bottom: 12px">
        <n-form ref="formRef" :model="formValue" :rules="rules" label-placement="top">
          <n-form-item label="租户编码" path="code" required>
            <n-input v-model:value="formValue.code" :disabled="!!editingId" placeholder="小写字母+数字，如 acme" />
          </n-form-item>
          <n-form-item label="租户名称" path="name" required>
            <n-input v-model:value="formValue.name" />
          </n-form-item>
          <n-form-item label="套餐">
            <n-select v-model:value="formValue.packageId" :options="packageOptions" clearable placeholder="请选择套餐" />
          </n-form-item>
          <n-form-item label="过期时间">
            <n-date-picker v-model:value="formValue.expireAtUtc" type="datetime" clearable style="width: 100%" />
          </n-form-item>
        </n-form>
        <template #footer>
          <n-space justify="end">
            <n-button @click="showFormDrawer = false">取消</n-button>
            <n-button type="primary" @click="handleFormSubmit">保存</n-button>
          </n-space>
        </template>
      </n-drawer-content>
    </n-drawer>

    <!-- 用户管理 Drawer -->
    <n-drawer v-model:show="showUsersDrawer" :width="640" placement="right">
      <n-drawer-content title="租户用户管理" body-content-style="padding-bottom: 12px">
        <n-space vertical>
          <n-button v-if="!showAddUserForm" type="primary" size="small" @click="showAddUserForm = true">
            添加用户
          </n-button>

          <n-card v-if="showAddUserForm" size="small" title="添加用户">
            <n-form label-placement="top">
              <n-form-item label="搜索用户">
                <n-select
                  v-model:value="addUserForm.userId"
                  filterable
                  remote
                  :options="userOptions"
                  :loading="userSearchLoading"
                  placeholder="输入昵称或邮箱搜索"
                  @search="handleUserSearch"
                />
              </n-form-item>
              <n-form-item label="租户内角色">
                <n-select v-model:value="addUserForm.roleInTenant" :options="roleInTenantOptions" />
              </n-form-item>
            </n-form>
            <n-space justify="end">
              <n-button size="small" @click="showAddUserForm = false">取消</n-button>
              <n-button type="primary" size="small" @click="handleAddUser">确认添加</n-button>
            </n-space>
          </n-card>

          <n-data-table
            :columns="[
              { title: '昵称', key: 'nickName' },
              { title: '邮箱', key: 'email' },
              { title: '角色', key: 'roleInTenant', width: 100 },
              { title: '加入时间', key: 'joinedAtUtc', width: 160, render: (row: TenantUser) => formatDateTime(row.joinedAtUtc) },
              {
                title: '操作',
                key: 'actions',
                width: 80,
                align: 'center',
                render: (row: TenantUser) =>
                  h(NButton, { text: true, type: 'error', size: 'small', onClick: () => handleRemoveUser(row.userId) }, { default: () => '移除' }),
              },
            ]"
            :data="tenantUsers"
            :loading="usersLoading"
            :row-key="(row: TenantUser) => row.userId"
          />
        </n-space>
      </n-drawer-content>
    </n-drawer>

    <!-- 配额管理 Drawer -->
    <n-drawer v-model:show="showQuotaDrawer" :width="500" placement="right">
      <n-drawer-content title="租户配额管理" body-content-style="padding-bottom: 12px">
        <n-spin :show="quotaLoading">
          <n-form :model="quotaForm" label-placement="top">
            <n-form-item label="用户数上限">
              <n-input-number v-model:value="quotaForm.maxUsers" :min="1" style="width: 100%" />
            </n-form-item>
            <n-form-item label="存储容量上限 (GB)">
              <n-input-number v-model:value="quotaForm.maxStorageBytes" :min="0" style="width: 100%" />
            </n-form-item>
            <n-form-item label="每日 API 调用上限">
              <n-input-number v-model:value="quotaForm.maxApiCallsPerDay" :min="0" style="width: 100%" />
            </n-form-item>
            <n-form-item label="单文件大小上限 (MB)">
              <n-input-number v-model:value="quotaForm.maxFileSize" :min="0" style="width: 100%" />
            </n-form-item>
            <n-form-item label="每日订单上限">
              <n-input-number v-model:value="quotaForm.maxOrdersPerDay" :min="0" style="width: 100%" />
            </n-form-item>
          </n-form>
        </n-spin>
        <template #footer>
          <n-space justify="end">
            <n-button @click="showQuotaDrawer = false">取消</n-button>
            <n-button type="primary" @click="handleQuotaSubmit">保存</n-button>
          </n-space>
        </template>
      </n-drawer-content>
    </n-drawer>
  </page-section>
</template>
