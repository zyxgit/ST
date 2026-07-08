import type { RouteRecordRaw } from 'vue-router'

import AppLayout from '@/components/layout/AppLayout.vue'
import { PermissionCode } from '@/constants/permissions'

export const adminRoutes: RouteRecordRaw[] = [
  {
    path: '/',
    component: AppLayout,
    redirect: '/dashboard',
    children: [
      {
        path: 'dashboard',
        name: 'dashboard',
        component: () => import('@/views/admin/DashboardView.vue'),
        meta: { title: '工作台' },
      },
      {
        path: 'system',
        name: 'system',
        redirect: '/dashboard',
        meta: { title: '系统管理' },
      },
      {
        path: 'system/users',
        name: 'users',
        component: () => import('@/views/admin/UsersView.vue'),
        alias: ['/users'],
        meta: { title: '用户管理', permission: PermissionCode.UserQuery },
      },
      {
        path: 'system/roles',
        name: 'roles',
        component: () => import('@/views/admin/RolesView.vue'),
        alias: ['/roles'],
        meta: { title: '角色管理', permission: PermissionCode.RoleQuery },
      },
      {
        path: 'system/menus',
        name: 'menus',
        component: () => import('@/views/admin/MenusView.vue'),
        alias: ['/menus'],
        meta: { title: '菜单权限', permission: PermissionCode.MenuQuery },
      },
      {
        path: 'system/tenants',
        name: 'tenants',
        component: () => import('@/views/admin/TenantsView.vue'),
        alias: ['/tenants'],
        meta: { title: '租户管理', permission: PermissionCode.TenantQuery },
      },
      {
        path: 'operation-logs',
        name: 'operation-logs',
        component: () => import('@/views/admin/OperationLogsView.vue'),
        meta: { title: '操作日志', permission: PermissionCode.OperationLogQuery },
      },
      {
        path: 'system/dead-letters',
        name: 'dead-letters',
        component: () => import('@/views/admin/DeadLettersView.vue'),
        alias: ['/dead-letters'],
        meta: { title: '死信队列', permission: PermissionCode.DeadLetterQuery },
      },
      {
        path: 'file',
        name: 'file',
        redirect: '/file/list',
        meta: { title: '文件管理' },
      },
      {
        path: 'file/list',
        name: 'files',
        component: () => import('@/views/admin/FilesView.vue'),
        alias: ['/files'],
        meta: { title: '文件列表', permission: PermissionCode.FileQuery },
      },
      {
        path: 'file/upload-test',
        name: 'file-upload-test',
        component: () => import('@/views/admin/FileUploadTestView.vue'),
        meta: { title: '文件上传测试', permission: PermissionCode.FileQuery },
      },
      {
        path: 'order',
        name: 'order',
        redirect: '/order/list',
        meta: { title: '订单管理' },
      },
      {
        path: 'order/list',
        name: 'order-list',
        component: () => import('@/views/admin/OrdersView.vue'),
        meta: { title: '订单列表', permission: PermissionCode.OrderQuery },
      },
      {
        path: 'inventory',
        name: 'inventory',
        redirect: '/inventory/skus',
        meta: { title: '库存管理' },
      },
      {
        path: 'inventory/skus',
        name: 'inventory-skus',
        component: () => import('@/views/admin/InventoryView.vue'),
        meta: { title: 'SKU 管理', permission: PermissionCode.InventorySkuQuery },
      },
      {
        path: 'payment',
        name: 'payment',
        redirect: '/payment/records',
        meta: { title: '支付管理' },
      },
      {
        path: 'payment/records',
        name: 'payment-records',
        component: () => import('@/views/admin/PaymentsView.vue'),
        meta: { title: '支付记录', permission: PermissionCode.PaymentRecordQuery },
      },
    ],
  },
]

export const publicRoutes: RouteRecordRaw[] = [
  {
    path: '/login',
    name: 'login',
    component: () => import('@/views/LoginView.vue'),
    meta: { public: true, title: '登录' },
  },
  {
    path: '/:pathMatch(.*)*',
    name: 'not-found',
    component: () => import('@/views/NotFoundView.vue'),
    meta: { public: true, title: '页面不存在' },
  },
]
