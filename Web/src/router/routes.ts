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
