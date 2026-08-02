<script setup lang="ts">
import type { DataTableColumns, FormInst, FormRules } from 'naive-ui'
import { NButton, NCard, NDataTable, NDrawer, NDrawerContent, NForm, NFormItem, NInput, NInputNumber, NSelect, NSpace, NSwitch, NTag } from 'naive-ui'
import { computed, h, nextTick, onMounted, reactive, ref } from 'vue'

import IconPicker from '@/components/common/IconPicker.vue'
import PageSection from '@/components/common/PageSection.vue'
import ServiceUnavailableState from '@/components/common/ServiceUnavailableState.vue'
import TableActions from '@/components/common/TableActions.vue'
import { createMenu, deleteMenu, getMenuDetail, getMenuTree, updateMenu } from '@/api/menu'
import { PermissionCode } from '@/constants/permissions'
import { codeRule, pathRule, requiredRule } from '@/lib/form-rules'
import { useDiscrete } from '@/lib/naive'
import { useAuthStore } from '@/stores/auth'
import { PermissionType, type MenuTreeNode } from '@/types/menu'

const { message, dialog } = useDiscrete()
const authStore = useAuthStore()

const loading = ref(false)
const loadError = ref('')
const items = ref<MenuTreeNode[]>([])
const showModal = ref(false)
const editingId = ref<string | null>(null)
const formRef = ref<FormInst | null>(null)

const formValue = reactive({
  parentId: '' as string,
  code: '',
  name: '',
  type: PermissionType.Menu,
  path: '',
  menuIcon: '',
  component: '',
  isLink: false,
  keepAlive: false,
  isHide: false,
  sort: 0,
})

const typeOptions = [
  { label: '目录', value: PermissionType.Catalogue },
  { label: '菜单', value: PermissionType.Menu },
  { label: '按钮', value: PermissionType.Button },
]

const parentOptions = ref<{ label: string; value: string }[]>([{ label: '顶级节点', value: '' }])
const rules = computed<FormRules>(() => ({
  name: [requiredRule('菜单名称')],
  code: [codeRule()],
  type: [requiredRule('菜单类型')],
  ...(formValue.type === PermissionType.Button || formValue.isLink ? {} : { path: [pathRule()] }),
  ...(formValue.type === PermissionType.Menu && !formValue.isLink ? { component: [requiredRule('组件名')] } : {}),
}))
const canQuery = computed(() => authStore.hasPermission(PermissionCode.MenuQuery))
const canCreate = computed(() => authStore.hasPermission(PermissionCode.MenuCreate))
const canUpdate = computed(() => authStore.hasPermission(PermissionCode.MenuUpdate))
const canDelete = computed(() => authStore.hasPermission(PermissionCode.MenuDelete))

const columns = computed<DataTableColumns<MenuTreeNode>>(() => [
  { title: '名称', key: 'name' },
  { title: '编码', key: 'code' },
  { title: '排序', key: 'sort', width: 70, align: 'center' },
  {
    title: '类型',
    key: 'type',
    render: (row) =>
      h(
        NTag,
        { type: row.type === PermissionType.Button ? 'warning' : row.type === PermissionType.Catalogue ? 'info' : 'success' },
        { default: () => typeOptions.find((item) => item.value === row.type)?.label ?? '未知' },
      ),
  },
  { title: '路径', key: 'path' },
  { title: '缓存', key: 'keepAlive', render: (row) => (row.keepAlive ? '是' : '否') },
  { title: '隐藏', key: 'isHide', render: (row) => (row.isHide ? '是' : '否') },
  {
    title: '操作',
    key: 'actions',
    width: 180,
    align: 'center',
    render: (row) =>
      h(TableActions, {
        actions: [
          ...(canUpdate.value ? [{ key: 'edit', label: '编辑', onClick: () => openEditModal(row.id) }] : []),
          ...(canDelete.value ? [{ key: 'delete', label: '删除', type: 'error' as const, onClick: () => handleDelete(row.id) }] : []),
        ],
        moreActions: [
          ...(canCreate.value ? [{ key: 'create-child', label: '新增子级', onClick: () => openCreateModal(row.id) }] : []),
        ],
      }),
  },
])

function flattenMenus(tree: MenuTreeNode[], prefix = ''): Array<{ label: string; value: string }> {
  return tree.flatMap((item) => {
    const label = prefix ? `${prefix} / ${item.name}` : item.name
    return [{ label, value: item.id }, ...flattenMenus(item.children, label)]
  })
}

async function loadData() {
  loading.value = true

  try {
    const result = await getMenuTree()
    items.value = result
    parentOptions.value = [{ label: '顶级节点', value: '' }, ...flattenMenus(result)]
    loadError.value = ''
  } catch {
    loadError.value = '菜单树加载失败，请确认后台接口已启动后重试。'
    items.value = []
    parentOptions.value = [{ label: '顶级节点', value: '' }]
  } finally {
    loading.value = false
  }
}

function resetForm(parentId: string | null = null) {
  editingId.value = null
  formValue.parentId = parentId ?? ''
  formValue.code = ''
  formValue.name = ''
  formValue.type = PermissionType.Menu
  formValue.path = ''
  formValue.menuIcon = ''
  formValue.component = ''
  formValue.isLink = false
  formValue.keepAlive = false
  formValue.isHide = false
  formValue.sort = 0
}

function openCreateModal(parentId: string | null = null) {
  resetForm(parentId)
  showModal.value = true
  void nextTick(() => formRef.value?.restoreValidation())
}

async function openEditModal(id: string) {
  const detail = await getMenuDetail(id)
  editingId.value = id
  formValue.parentId = detail.parentId ?? ''
  formValue.code = detail.code
  formValue.name = detail.name
  formValue.type = detail.type
  formValue.path = detail.path ?? ''
  formValue.menuIcon = detail.menuIcon ?? ''
  formValue.component = detail.component ?? ''
  formValue.isLink = detail.isLink
  formValue.keepAlive = detail.keepAlive
  formValue.isHide = detail.isHide
  formValue.sort = detail.sort ?? 0
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
    parentId: formValue.parentId || null,
    code: formValue.code.trim(),
    name: formValue.name.trim(),
    type: formValue.type,
    path: formValue.type === PermissionType.Button ? null : formValue.path.trim() || null,
    menuIcon: formValue.menuIcon.trim() || null,
    component:
      formValue.type === PermissionType.Menu && !formValue.isLink
        ? formValue.component.trim() || null
        : null,
    isLink: formValue.isLink,
    keepAlive: formValue.keepAlive,
    isHide: formValue.isHide,
    sort: formValue.sort,
  }

  if (editingId.value) {
    await updateMenu(editingId.value, payload)
  } else {
    await createMenu(payload)
  }

  message.success(editingId.value ? '菜单已更新' : '菜单已创建')
  showModal.value = false
  await authStore.refreshMenuTree()
  await loadData()
}

function handleDelete(id: string) {
  dialog.error({
    title: '删除菜单',
    content: '删除菜单前请确认没有子节点和角色仍在引用。',
    positiveText: '删除',
    negativeText: '取消',
    async onPositiveClick() {
      await deleteMenu(id)
      message.success('菜单已删除')
      await authStore.refreshMenuTree()
      await loadData()
    },
  })
}

onMounted(async () => {
  if (canQuery.value) {
    await loadData()
  }
})
</script>

<template>
  <page-section title="菜单权限" description="直接基于后端菜单树进行维护，目录/菜单/按钮统一管理。">
    <n-card class="page-card" :bordered="false">
      <n-space justify="end">
        <n-button v-if="canCreate" type="primary" @click="openCreateModal()">新增顶级菜单</n-button>
      </n-space>
    </n-card>

    <n-card class="page-card" :bordered="false">
      <n-data-table
        v-if="canQuery && !loadError"
        :columns="columns"
        :data="items"
        :loading="loading"
        children-key="children"
        default-expand-all
        :row-key="(row: MenuTreeNode) => row.id"
      />
      <service-unavailable-state v-else-if="loadError" :description="loadError" @retry="loadData" />
      <div v-else style="color: var(--text-3)">当前账号没有查看菜单权限列表的权限。</div>
    </n-card>

    <n-drawer v-model:show="showModal" :width="620" placement="right">
      <n-drawer-content :title="editingId ? '编辑菜单' : '新增菜单'" body-content-style="padding-bottom: 12px">
      <n-form ref="formRef" :model="formValue" :rules="rules" label-placement="top">
        <n-form-item label="上级菜单">
          <n-select v-model:value="formValue.parentId" :options="parentOptions" />
        </n-form-item>
        <n-form-item label="名称" path="name" required>
          <n-input v-model:value="formValue.name" />
        </n-form-item>
        <n-form-item label="编码" path="code" required>
          <n-input v-model:value="formValue.code" />
        </n-form-item>
        <n-form-item label="类型" path="type" required>
          <n-select v-model:value="formValue.type" :options="typeOptions" />
        </n-form-item>
        <n-form-item v-if="formValue.type !== PermissionType.Button" label="路由路径" path="path" required>
          <n-input v-model:value="formValue.path" placeholder="/users" />
        </n-form-item>
        <n-form-item v-if="formValue.type === PermissionType.Menu && !formValue.isLink" label="组件名" path="component" required>
          <n-input v-model:value="formValue.component" placeholder="admin/UsersView" />
        </n-form-item>
        <n-form-item label="图标">
          <IconPicker v-model:value="formValue.menuIcon" title="选择菜单图标" />
        </n-form-item>
        <n-form-item label="排序号">
          <n-input-number v-model:value="formValue.sort" :min="0" :max="999" style="width: 120px" />
        </n-form-item>
        <n-space>
          <n-form-item label="外链">
            <n-switch v-model:value="formValue.isLink" />
          </n-form-item>
          <n-form-item label="缓存">
            <n-switch v-model:value="formValue.keepAlive" />
          </n-form-item>
          <n-form-item label="隐藏">
            <n-switch v-model:value="formValue.isHide" />
          </n-form-item>
        </n-space>
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
