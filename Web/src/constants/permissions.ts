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

  DeadLetterQuery: 'system:deadletter:query',
  DeadLetterReplay: 'system:deadletter:replay',

  TenantQuery: 'system:tenant:query',
  TenantCreate: 'system:tenant:create',
  TenantUpdate: 'system:tenant:update',
  TenantDelete: 'system:tenant:delete',
  TenantUser: 'system:tenant:user',
  TenantQuota: 'system:tenant:quota',

  FileQuery: 'system:file:query',
  FileUpload: 'system:file:upload',
  FileDelete: 'system:file:delete',

  OrderQuery: 'order:list:query',
  OrderCreate: 'order:list:create',
  OrderCancel: 'order:list:cancel',

  InventorySkuQuery: 'inventory:sku:query',
  InventorySkuCreate: 'inventory:sku:create',
  InventorySkuStock: 'inventory:sku:stock',

  PaymentRecordQuery: 'payment:record:query',
  PaymentOrderPay: 'payment:order:pay',
} as const
