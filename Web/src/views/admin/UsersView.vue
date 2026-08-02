<script setup lang="ts">
import type { DataTableColumns, FormInst, FormRules } from 'naive-ui'
import {
  NButton,
  NCard,
  NDataTable,
  NDrawer,
  NDrawerContent,
  NForm,
  NFormItem,
  NInput,
  NPagination,
  NSelect,
  NSpace,
  NSwitch,
  NTag,
} from 'naive-ui'
import { computed, h, nextTick, onMounted, reactive, ref } from 'vue'

import PageSection from '@/components/common/PageSection.vue'
import ServiceUnavailableState from '@/components/common/ServiceUnavailableState.vue'
import TableActions from '@/components/common/TableActions.vue'
import { changeUserStatus, createUser, deleteUser, getRoleOptions, getUserDetail, getUsers, resetUserPassword, updateUser } from '@/api/user'
import { PermissionCode } from '@/constants/permissions'
import { formatDateTime } from '@/lib/dayjs'
import { arrayRequiredRule, emailRule, requiredPhoneRule, passwordRule, requiredRule } from '@/lib/form-rules'
import { useDiscrete } from '@/lib/naive'
import { useAuthStore } from '@/stores/auth'
import type { UserListItem } from '@/types/user'

const { message, dialog } = useDiscrete()
const authStore = useAuthStore()

const loading = ref(false)
const loadError = ref('')
const roleOptions = ref<{ label: string; value: string }[]>([])
const items = ref<UserListItem[]>([])
const totalCount = ref(0)
const showModal = ref(false)
const editingId = ref<string | null>(null)
const formRef = ref<FormInst | null>(null)

const query = reactive({
  keyword: '',
  isEnable: null as 'true' | 'false' | null,
  roleId: null as string | null,
  pageIndex: 1,
  pageSize: 10,
})

const formValue = reactive({
  nickName: '',
  email: '',
  phone: '',
  password: '',
  isEnable: true,
  roleIds: [] as string[],
})
const rules = computed<FormRules>(() => ({
  nickName: [requiredRule('昵称')],
  email: [emailRule()],
  phone: [requiredPhoneRule()],
  roleIds: [arrayRequiredRule('角色')],
  ...(editingId.value ? {} : { password: [passwordRule('初始密码')] }),
}))
const canQuery = computed(() => authStore.hasPermission(PermissionCode.UserQuery))
const canCreate = computed(() => authStore.hasPermission(PermissionCode.UserCreate))
const canUpdate = computed(() => authStore.hasPermission(PermissionCode.UserUpdate))
const canDelete = computed(() => authStore.hasPermission(PermissionCode.UserDelete))
const canResetPassword = computed(() => authStore.hasPermission(PermissionCode.UserResetPassword))
const canChangeStatus = computed(() => authStore.hasPermission(PermissionCode.UserChangeStatus))

const columns = computed<DataTableColumns<UserListItem>>(() => [
  { title: '昵称', key: 'nickName' },
  { title: '邮箱', key: 'email' },
  {
    title: '角色',
    key: 'roles',
    render: (row: UserListItem) => row.roles.map((item) => h(NTag, { style: 'margin-right: 8px;' }, { default: () => item })),
  },
  {
    title: '状态',
    key: 'isEnable',
    render: (row: UserListItem) =>
      h(NSwitch, {
        value: row.isEnable,
        disabled: !canChangeStatus.value,
        async 'onUpdate:value'(value: boolean) {
          await changeUserStatus(row.id, { isEnable: value })
          message.success('状态已更新')
          await loadData()
        },
      }),
  },
  {
    title: '最后登录',
    key: 'lastLoginTime',
    render: (row: UserListItem) => formatDateTime(row.lastLoginTime),
  },
  {
    title: '创建时间',
    key: 'createTime',
    render: (row: UserListItem) => formatDateTime(row.createTime),
  },
  {
    title: '操作',
    key: 'actions',
    width: 200,
    align: 'center',
    render: (row: UserListItem) =>
      h(TableActions, {
        actions: [
          ...(canUpdate.value ? [{ key: 'edit', label: '编辑', onClick: () => openEditModal(row.id) }] : []),
          ...(canDelete.value ? [{ key: 'delete', label: '删除', type: 'error' as const, onClick: () => handleDelete(row.id) }] : []),
        ],
        moreActions: [
          ...(canResetPassword.value ? [{ key: 'reset-password', label: '重置密码', onClick: () => handleResetPassword(row.id) }] : []),
        ],
      }),
  },
])

async function loadOptions() {
  try {
    const result = await getRoleOptions()
    roleOptions.value = result.map((item) => ({
      label: item.name,
      value: item.id,
    }))
  } catch {
    loadError.value = '角色选项加载失败，请确认后台接口已启动后重试。'
    roleOptions.value = []
  }
}

async function loadData() {
  loading.value = true

  try {
    const normalized = {
      ...query,
      isEnable: query.isEnable === null ? null : query.isEnable === 'true',
    }
    const result = await getUsers(normalized)
    items.value = result.items
    totalCount.value = result.totalCount
    loadError.value = ''
  } catch {
    loadError.value = '用户列表加载失败，请确认后台接口已启动后重试。'
    items.value = []
    totalCount.value = 0
  } finally {
    loading.value = false
  }
}

function resetForm() {
  editingId.value = null
  formValue.nickName = ''
  formValue.email = ''
  formValue.phone = ''
  formValue.password = ''
  formValue.isEnable = true
  formValue.roleIds = []
}

function openCreateModal() {
  resetForm()
  showModal.value = true
  void nextTick(() => formRef.value?.restoreValidation())
}

async function openEditModal(id: string) {
  const detail = await getUserDetail(id)
  editingId.value = id
  formValue.nickName = detail.nickName
  formValue.email = detail.email
  formValue.phone = detail.phone
  formValue.password = ''
  formValue.isEnable = detail.isEnable
  formValue.roleIds = detail.roles.map((item) => item.id)
  showModal.value = true
  void nextTick(() => formRef.value?.restoreValidation())
}

async function handleSubmit() {
  try {
    await formRef.value?.validate()
  } catch {
    return
  }

  if (editingId.value) {
    await updateUser(editingId.value, {
      nickName: formValue.nickName.trim(),
      email: formValue.email.trim(),
      phone: formValue.phone.trim() || null,
      isEnable: formValue.isEnable,
      roleIds: formValue.roleIds,
    })
  } else {
    await createUser({
      nickName: formValue.nickName.trim(),
      email: formValue.email.trim(),
      phone: formValue.phone.trim() || null,
      password: formValue.password.trim(),
      isEnable: formValue.isEnable,
      roleIds: formValue.roleIds,
    })
  }

  message.success(editingId.value ? '用户已更新' : '用户已创建')
  showModal.value = false
  await loadData()
}

function handleResetPassword(id: string) {
  dialog.warning({
    title: '重置密码',
    content: '将密码重置为 123456，可在后续接成单独输入弹窗。',
    positiveText: '确认',
    negativeText: '取消',
    async onPositiveClick() {
      await resetUserPassword(id, { password: '123456' })
      message.success('密码已重置')
    },
  })
}

function handleDelete(id: string) {
  dialog.error({
    title: '删除用户',
    content: '删除后不可恢复，确认继续吗？',
    positiveText: '删除',
    negativeText: '取消',
    async onPositiveClick() {
      await deleteUser(id)
      message.success('用户已删除')
      await loadData()
    },
  })
}

onMounted(async () => {
  await loadOptions()

  if (canQuery.value) {
    await loadData()
  }
})
</script>

<template>
  <page-section title="用户管理" description="已接入用户分页、编辑、状态变更、密码重置和删除能力。">
    <n-card class="page-card" :bordered="false">
      <n-space justify="space-between">
        <n-space>
          <n-input v-model:value="query.keyword" clearable placeholder="昵称 / 邮箱 / 手机号" />
          <n-select
            v-model:value="query.isEnable"
            clearable
            placeholder="状态"
            :options="[
              { label: '启用', value: 'true' },
              { label: '禁用', value: 'false' },
            ]"
            style="width: 120px"
          />
          <n-select v-model:value="query.roleId" clearable placeholder="角色" :options="roleOptions" style="width: 180px" />
          <n-button v-if="canQuery" type="primary" @click="loadData">查询</n-button>
        </n-space>
        <n-button v-if="canCreate" type="primary" @click="openCreateModal">新增用户</n-button>
      </n-space>
    </n-card>

    <n-card class="page-card" :bordered="false">
      <n-data-table
        v-if="canQuery && !loadError"
        :columns="columns"
        :data="items"
        :loading="loading"
        :row-key="(row: UserListItem) => row.id"
      />
      <service-unavailable-state v-else-if="loadError" :description="loadError" @retry="loadOptions().then(loadData)" />
      <div v-else style="color: var(--text-3)">当前账号没有查看用户列表的权限。</div>
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

    <n-drawer v-model:show="showModal" :width="560" placement="right">
      <n-drawer-content :title="editingId ? '编辑用户' : '新增用户'" body-content-style="padding-bottom: 12px">
      <n-form ref="formRef" :model="formValue" :rules="rules" label-placement="top">
        <n-form-item label="昵称" path="nickName" required>
          <n-input v-model:value="formValue.nickName" />
        </n-form-item>
        <n-form-item label="邮箱" path="email" required>
          <n-input v-model:value="formValue.email" />
        </n-form-item>
        <n-form-item label="手机号" path="phone" required>
          <n-input v-model:value="formValue.phone" />
        </n-form-item>
        <n-form-item v-if="!editingId" label="初始密码" path="password" required>
          <n-input v-model:value="formValue.password" type="password" show-password-on="click" />
        </n-form-item>
        <n-form-item label="角色" path="roleIds" required>
          <n-select v-model:value="formValue.roleIds" multiple :options="roleOptions" />
        </n-form-item>
        <n-form-item label="启用">
          <n-switch v-model:value="formValue.isEnable" />
        </n-form-item>
      </n-form>
      <template #footer>
        <n-space justify="end">
          <n-button @click="showModal = false">取消</n-button>
          <n-button type="primary" @click="handleSubmit">保存</n-button>
        </n-space>
      </template>
      </n-drawer-content>
    </n-drawer>
  </page-section>
</template>
