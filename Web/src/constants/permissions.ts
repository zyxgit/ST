export const PermissionCode = {
  UserQuery: 'system:user:query',
  UserCreate: 'system:user:create',
  UserUpdate: 'system:user:update',
  UserDelete: 'system:user:delete',
  UserResetPassword: 'system:user:reset-password',
  UserChangeStatus: 'system:user:change-status',

  RoleQuery: 'system:role:query',
  RoleCreate: 'system:role:create',
  RoleUpdate: 'system:role:update',
  RoleDelete: 'system:role:delete',

  MenuQuery: 'system:menu:query',
  MenuCreate: 'system:menu:create',
  MenuUpdate: 'system:menu:update',
  MenuDelete: 'system:menu:delete',

  OperationLogQuery: 'system:operationlog:query',

  TenantQuery: 'system:tenant:query',
  TenantCreate: 'system:tenant:create',
  TenantUpdate: 'system:tenant:update',
  TenantDelete: 'system:tenant:delete',
  TenantUser: 'system:tenant:user',
  TenantQuota: 'system:tenant:quota',
} as const
