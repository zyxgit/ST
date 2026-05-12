<script setup lang="ts">
import type { DataTableColumns, FormInst, FormRules } from 'naive-ui'
import { NButton, NCard, NDataTable, NDrawer, NDrawerContent, NForm, NFormItem, NInput, NPagination, NSpace, NSwitch, NTag, NTree } from 'naive-ui'
import { computed, h, nextTick, onMounted, reactive, ref } from 'vue'

import PageSection from '@/components/common/PageSection.vue'
import ServiceUnavailableState from '@/components/common/ServiceUnavailableState.vue'
import TableActions from '@/components/common/TableActions.vue'
import { getMenuTree } from '@/api/menu'
import { createRole, deleteRole, getRoleDetail, getRoles, updateRole } from '@/api/role'
import { PermissionCode } from '@/constants/permissions'
import { formatDateTime } from '@/lib/dayjs'
import { codeRule, requiredRule } from '@/lib/form-rules'
import { useDiscrete } from '@/lib/naive'
import { useAuthStore } from '@/stores/auth'
import type { MenuTreeNode } from '@/types/menu'
import type { RoleListItem } from '@/types/role'

const { message, dialog } = useDiscrete()
const authStore = useAuthStore()

const loading = ref(false)
const loadError = ref('')
const items = ref<RoleListItem[]>([])
const totalCount = ref(0)
const permissions = ref<MenuTreeNode[]>([])
const checkedPermissionIds = ref<string[]>([])
const cascadeCheck = ref(true)
const showModal = ref(false)
const editingId = ref<string | null>(null)
const formRef = ref<FormInst | null>(null)

const query = reactive({
  keyword: '',
  pageIndex: 1,
  pageSize: 10,
})

const formValue = reactive({
  code: '',
  name: '',
  description: '',
  isSystem: false,
  isDefault: false,
})
const rules: FormRules = {
  code: [codeRule()],
  name: [requiredRule('角色名称')],
}
const canQuery = computed(() => authStore.hasPermission(PermissionCode.RoleQuery))
const canCreate = computed(() => authStore.hasPermission(PermissionCode.RoleCreate))
const canUpdate = computed(() => authStore.hasPermission(PermissionCode.RoleUpdate))
const canDelete = computed(() => authStore.hasPermission(PermissionCode.RoleDelete))

const columns = computed<DataTableColumns<RoleListItem>>(() => [
  { title: '编码', key: 'code' },
  { title: '名称', key: 'name' },
  { title: '描述', key: 'description' },
  {
    title: '系统角色',
    key: 'isSystem',
    render: (row: RoleListItem) => h(NTag, { type: row.isSystem ? 'warning' : 'default' }, { default: () => (row.isSystem ? '是' : '否') }),
  },
  {
    title: '默认角色',
    key: 'isDefault',
    render: (row: RoleListItem) => h(NSwitch, { value: row.isDefault, disabled: true }),
  },
  { title: '权限数', key: 'permissionCount' },
  {
    title: '创建时间',
    key: 'createTime',
    render: (row: RoleListItem) => formatDateTime(row.createTime),
  },
  {
    title: '操作',
    key: 'actions',
    width: 160,
    align: 'center',
    render: (row: RoleListItem) =>
      h(TableActions, {
        actions: [
          ...(canUpdate.value ? [{ key: 'edit', label: '编辑', onClick: () => openEditModal(row.id) }] : []),
          ...(canDelete.value ? [{ key: 'delete', label: '删除', type: 'error' as const, onClick: () => handleDelete(row.id) }] : []),
        ],
      }),
  },
])

async function loadData() {
  loading.value = true

  try {
    const result = await getRoles(query)
    items.value = result.items
    totalCount.value = result.totalCount
    loadError.value = ''
  } catch {
    loadError.value = '角色列表加载失败，请确认后台接口已启动后重试。'
    items.value = []
    totalCount.value = 0
  } finally {
    loading.value = false
  }
}

async function loadPermissions() {
  try {
    permissions.value = await getMenuTree()
  } catch {
    loadError.value = '权限树加载失败，请确认后台接口已启动后重试。'
    permissions.value = []
  }
}

function resetForm() {
  editingId.value = null
  formValue.code = ''
  formValue.name = ''
  formValue.description = ''
  formValue.isSystem = false
  formValue.isDefault = false
  checkedPermissionIds.value = []
  cascadeCheck.value = true
}

function openCreateModal() {
  resetForm()
  showModal.value = true
  void nextTick(() => formRef.value?.restoreValidation())
}

async function openEditModal(id: string) {
  const detail = await getRoleDetail(id)
  editingId.value = id
  formValue.code = detail.code
  formValue.name = detail.name
  formValue.description = detail.description
  formValue.isSystem = detail.isSystem
  formValue.isDefault = detail.isDefault
  checkedPermissionIds.value = detail.permissionIds
  showModal.value = true
  void nextTick(() => formRef.value?.restoreValidation())
}

async function handleSubmit() {
  try {
    await formRef.value?.validate()
  } catch {
    return
  }

  const payload = {
    code: formValue.code.trim(),
    name: formValue.name.trim(),
    description: formValue.description.trim(),
    isSystem: formValue.isSystem,
    isDefault: formValue.isDefault,
    permissionIds: checkedPermissionIds.value,
  }

  if (editingId.value) {
    await updateRole(editingId.value, payload)
  } else {
    await createRole(payload)
  }

  message.success(editingId.value ? '角色已更新' : '角色已创建')
  showModal.value = false
  await loadData()
}

function handleDelete(id: string) {
  dialog.error({
    title: '删除角色',
    content: '确认删除这个角色吗？',
    positiveText: '删除',
    negativeText: '取消',
    async onPositiveClick() {
      await deleteRole(id)
      message.success('角色已删除')
      await loadData()
    },
  })
}

onMounted(async () => {
  await loadPermissions()

  if (canQuery.value) {
    await loadData()
  }
})
</script>

<template>
  <page-section title="角色管理" description="角色列表与权限分配已经接到后端菜单权限树。">
    <n-card class="page-card" :bordered="false">
      <n-space justify="space-between">
        <n-space>
          <n-input v-model:value="query.keyword" clearable placeholder="角色名称 / 编码" />
          <n-button v-if="canQuery" type="primary" @click="loadData">查询</n-button>
        </n-space>
        <n-button v-if="canCreate" type="primary" @click="openCreateModal">新增角色</n-button>
      </n-space>
    </n-card>

    <n-card class="page-card" :bordered="false">
      <n-data-table
        v-if="canQuery && !loadError"
        :columns="columns"
        :data="items"
        :loading="loading"
        :row-key="(row: RoleListItem) => row.id"
      />
      <service-unavailable-state v-else-if="loadError" :description="loadError" @retry="loadPermissions().then(loadData)" />
      <div v-else style="color: var(--text-3)">当前账号没有查看角色列表的权限。</div>
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

    <n-drawer v-model:show="showModal" :width="760" placement="right">
      <n-drawer-content :title="editingId ? '编辑角色' : '新增角色'" body-content-style="padding-bottom: 12px">
      <n-form ref="formRef" :model="formValue" :rules="rules" label-placement="top">
        <n-form-item label="编码" path="code">
          <n-input v-model:value="formValue.code" />
        </n-form-item>
        <n-form-item label="名称" path="name">
          <n-input v-model:value="formValue.name" />
        </n-form-item>
        <n-form-item label="描述">
          <n-input v-model:value="formValue.description" type="textarea" />
        </n-form-item>
        <n-space>
          <n-form-item label="系统角色">
            <n-switch v-model:value="formValue.isSystem" />
          </n-form-item>
          <n-form-item label="默认角色">
            <n-switch v-model:value="formValue.isDefault" />
          </n-form-item>
        </n-space>
        <n-form-item label="权限">
          <div style="width: 100%">
            <div class="permission-toolbar">
              <div class="permission-toolbar__meta">
                <div style="color: var(--text-3)">已选 {{ checkedPermissionIds.length }} 项</div>
                <div class="permission-cascade">
                  <span class="permission-cascade__label">级联选择</span>
                  <div class="permission-cascade__group">
                    <button
                      class="permission-cascade__option"
                      :class="{ 'permission-cascade__option--active': cascadeCheck }"
                      type="button"
                      @click="cascadeCheck = true"
                    >
                      开启
                    </button>
                    <button
                      class="permission-cascade__option"
                      :class="{ 'permission-cascade__option--active': !cascadeCheck }"
                      type="button"
                      @click="cascadeCheck = false"
                    >
                      关闭
                    </button>
                  </div>
                </div>
              </div>
              <n-tag :type="cascadeCheck ? 'warning' : 'default'" round size="small">
                {{ cascadeCheck ? '父子节点联动中' : '独立勾选模式' }}
              </n-tag>
            </div>
            <div
              class="permission-tree-panel"
              style="
                max-height: 320px;
                overflow: auto;
                padding: 12px;
                border: 1px solid var(--border-color);
                border-radius: 12px;
                background: var(--card-color);
              "
            >
              <n-tree
                v-model:checked-keys="checkedPermissionIds"
                class="permission-tree"
                checkable
                :cascade="cascadeCheck"
                block-line
                expand-on-click
                default-expand-all
                key-field="id"
                label-field="name"
                :data="permissions"
              />
            </div>
          </div>
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

<style scoped>
.permission-toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  margin-bottom: 12px;
}

.permission-toolbar__meta {
  display: flex;
  align-items: center;
  gap: 16px;
  flex-wrap: wrap;
}

.permission-cascade {
  display: flex;
  align-items: center;
  gap: 10px;
}

.permission-cascade__label {
  color: var(--text-2);
  font-size: 14px;
  font-weight: 600;
}

.permission-cascade__group {
  display: inline-flex;
  padding: 4px;
  border: 1px solid color-mix(in srgb, var(--panel-border) 90%, transparent 10%);
  border-radius: 999px;
  background: color-mix(in srgb, var(--panel-bg-soft) 88%, white 12%);
}

.permission-cascade__option {
  min-width: 58px;
  padding: 6px 14px;
  border: none;
  border-radius: 999px;
  background: transparent;
  color: var(--text-3);
  cursor: pointer;
  font-size: 13px;
  font-weight: 600;
  transition:
    color 0.2s ease,
    background-color 0.2s ease,
    box-shadow 0.2s ease,
    transform 0.2s ease;
}

.permission-cascade__option:hover {
  color: var(--text-2);
}

.permission-cascade__option--active {
  background: var(--n-primary-color, #2563eb);
  color: #ffffff;
  box-shadow: 0 8px 18px color-mix(in srgb, var(--n-primary-color, #2563eb) 26%, transparent 74%);
}

.permission-tree-panel {
  transition:
    border-color 0.2s ease,
    box-shadow 0.2s ease,
    background-color 0.2s ease;
}

.permission-tree-panel:hover {
  border-color: rgba(15, 118, 110, 0.35);
  box-shadow: inset 0 0 0 1px rgba(15, 118, 110, 0.08);
}

.permission-tree :deep(.n-tree-node-content) {
  border-radius: 10px;
  cursor: pointer;
  transition:
    background-color 0.2s ease,
    color 0.2s ease,
    box-shadow 0.2s ease;
}

.permission-tree :deep(.n-tree-node-content:hover) {
  background: rgba(15, 118, 110, 0.1);
}

.permission-tree :deep(.n-tree-node-content__text) {
  cursor: pointer;
}

.permission-tree :deep(.n-tree-node-checkbox),
.permission-tree :deep(.n-tree-node-switcher) {
  cursor: pointer;
}

.permission-tree :deep(.n-tree-node--selected > .n-tree-node-content),
.permission-tree :deep(.n-tree-node:not(.n-tree-node--disabled) > .n-tree-node-content:focus-visible) {
  background: rgba(15, 118, 110, 0.14);
  box-shadow: inset 0 0 0 1px rgba(15, 118, 110, 0.22);
}

.permission-tree :deep(.n-tree-node-checkbox.n-checkbox--checked + .n-tree-node-content),
.permission-tree :deep(.n-tree-node-checkbox.n-checkbox--indeterminate + .n-tree-node-content) {
  background: rgba(15, 118, 110, 0.12);
}
</style>
