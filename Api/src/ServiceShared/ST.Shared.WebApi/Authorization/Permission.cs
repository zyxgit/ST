namespace ST.Shared.WebApi.Authorization;

/// <summary>
/// 权限码常量。
/// 使用方式：<c>[PermissionAuthorize(Permission.UserQuery)]</c>
/// </summary>
public static class Permission
{
	// ======================== 系统管理 ========================

	// 用户管理
	public const string UserQuery = "system:user:query";
	public const string UserCreate = "system:user:create";
	public const string UserUpdate = "system:user:update";
	public const string UserDelete = "system:user:delete";
	public const string UserResetPassword = "system:user:reset-password";
	public const string UserChangeStatus = "system:user:change-status";

	// 角色管理
	public const string RoleQuery = "system:role:query";
	public const string RoleCreate = "system:role:create";
	public const string RoleUpdate = "system:role:update";
	public const string RoleDelete = "system:role:delete";

	// 菜单管理
	public const string MenuQuery = "system:menu:query";
	public const string MenuCreate = "system:menu:create";
	public const string MenuUpdate = "system:menu:update";
	public const string MenuDelete = "system:menu:delete";

	// 操作日志
	public const string OperationLogQuery = "system:operationlog:query";

	// 租户管理
	public const string TenantQuery = "system:tenant:query";
	public const string TenantCreate = "system:tenant:create";
	public const string TenantUpdate = "system:tenant:update";
	public const string TenantDelete = "system:tenant:delete";
	public const string TenantUser = "system:tenant:user";
	public const string TenantQuota = "system:tenant:quota";

	// 死信队列
	public const string DeadLetterQuery = "system:deadletter:query";
	public const string DeadLetterReplay = "system:deadletter:replay";

	// 文件管理
	public const string FileQuery = "system:file:query";
	public const string FileUpload = "system:file:upload";
	public const string FileDelete = "system:file:delete";

	// ======================== 订单管理 ========================

	public const string OrderQuery = "order:list:query";
	public const string OrderCreate = "order:list:create";
	public const string OrderCancel = "order:list:cancel";

	// ======================== 库存管理 ========================

	public const string InventorySkuQuery = "inventory:sku:query";
	public const string InventorySkuCreate = "inventory:sku:create";
	public const string InventorySkuStock = "inventory:sku:stock";

	// ======================== 支付管理 ========================

	public const string PaymentRecordQuery = "payment:record:query";
	public const string PaymentOrderPay = "payment:order:pay";
}
