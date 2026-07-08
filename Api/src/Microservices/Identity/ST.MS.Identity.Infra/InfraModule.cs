using Microsoft.Extensions.DependencyInjection;
using ST.Infra.EntityFramework.Npgsql.Extensions;
using ST.MS.Identity.Infra.DbContext;
using ST.Shared.Module;

namespace ST.MS.Identity.Infra;

public sealed class InfraModule : ServiceModule
{
	// ======================== 固定 ID ========================
	private const string AdminRoleId = "11111111-1111-1111-1111-111111111111";
	private const string SeedUserId = "019d2988-fd04-7510-ae5b-61bff91c18cf";
	private const string SeedUserEmail = "test@qq.com";
	private const string DefaultTenantId = "11111111-1111-1111-1111-111111111000";
	private const string DefaultTenantQuotaId = "11111111-1111-1111-1111-111111111001";

	// ======================== 权限 ID ========================
	// 系统管理
	private const string RootPermissionId = "11111111-1111-1111-1111-111111111201";
	private const string UserPermissionId = "11111111-1111-1111-1111-111111111202";
	private const string RolePermissionId = "11111111-1111-1111-1111-111111111203";
	private const string MenuPermissionId = "11111111-1111-1111-1111-111111111204";
	private const string OperationLogPermissionId = "11111111-1111-1111-1111-111111111205";
	private const string TenantMenuPermissionId = "11111111-1111-1111-1111-111111111206";
	private const string DeadLetterMenuPermissionId = "11111111-1111-1111-1111-111111111207";
	// 用户操作
	private const string UserQueryPermissionId = "11111111-1111-1111-1111-111111111212";
	private const string UserCreatePermissionId = "11111111-1111-1111-1111-111111111213";
	private const string UserUpdatePermissionId = "11111111-1111-1111-1111-111111111214";
	private const string UserDeletePermissionId = "11111111-1111-1111-1111-111111111215";
	private const string UserResetPasswordPermissionId = "11111111-1111-1111-1111-111111111216";
	private const string UserChangeStatusPermissionId = "11111111-1111-1111-1111-111111111217";
	// 角色操作
	private const string RoleQueryPermissionId = "11111111-1111-1111-1111-111111111222";
	private const string RoleCreatePermissionId = "11111111-1111-1111-1111-111111111223";
	private const string RoleUpdatePermissionId = "11111111-1111-1111-1111-111111111224";
	private const string RoleDeletePermissionId = "11111111-1111-1111-1111-111111111225";
	// 菜单操作
	private const string MenuQueryPermissionId = "11111111-1111-1111-1111-111111111232";
	private const string MenuCreatePermissionId = "11111111-1111-1111-1111-111111111233";
	private const string MenuUpdatePermissionId = "11111111-1111-1111-1111-111111111234";
	private const string MenuDeletePermissionId = "11111111-1111-1111-1111-111111111235";
	// 操作日志
	private const string OperationLogQueryPermissionId = "11111111-1111-1111-1111-111111111242";
	// 租户操作
	private const string TenantQueryPermissionId = "11111111-1111-1111-1111-111111111252";
	private const string TenantCreatePermissionId = "11111111-1111-1111-1111-111111111253";
	private const string TenantUpdatePermissionId = "11111111-1111-1111-1111-111111111254";
	private const string TenantDeletePermissionId = "11111111-1111-1111-1111-111111111255";
	private const string TenantUserPermissionId = "11111111-1111-1111-1111-111111111256";
	private const string TenantQuotaPermissionId = "11111111-1111-1111-1111-111111111257";
	// 死信队列
	private const string DeadLetterQueryPermissionId = "11111111-1111-1111-1111-111111111262";
	private const string DeadLetterReplayPermissionId = "11111111-1111-1111-1111-111111111263";
	// 订单管理
	private const string OrderMenuPermissionId = "11111111-1111-1111-1111-111111111301";
	private const string OrderListPermissionId = "11111111-1111-1111-1111-111111111302";
	private const string OrderQueryPermissionId = "11111111-1111-1111-1111-111111111312";
	private const string OrderCancelPermissionId = "11111111-1111-1111-1111-111111111313";
	// 库存管理
	private const string InventoryMenuPermissionId = "11111111-1111-1111-1111-111111111401";
	private const string InventorySkuPermissionId = "11111111-1111-1111-1111-111111111402";
	private const string InventorySkuQueryPermissionId = "11111111-1111-1111-1111-111111111412";
	private const string InventorySkuCreatePermissionId = "11111111-1111-1111-1111-111111111413";
	private const string InventorySkuStockPermissionId = "11111111-1111-1111-1111-111111111414";
	// 支付管理
	private const string PaymentMenuPermissionId = "11111111-1111-1111-1111-111111111501";
	private const string PaymentRecordPermissionId = "11111111-1111-1111-1111-111111111502";
	private const string PaymentRecordQueryPermissionId = "11111111-1111-1111-1111-111111111512";
	// 文件管理
	private const string FileMenuPermissionId = "11111111-1111-1111-1111-111111111601";
	private const string FileQueryPermissionId = "11111111-1111-1111-1111-111111111612";
	private const string FileDeletePermissionId = "11111111-1111-1111-1111-111111111616";

	public override void ConfigureServices(IServiceCollection services)
	{
		base.ConfigureServices(services);

		services.AddNpgsqlDbContextFromConfig<IdentityDbContext>(seeds =>
		{
			// 1. 权限数据
			seeds.AddSqlFile("Seeds/001_permissions.sql", order: 100);

			// 2. 默认用户
			seeds.AddSql(
				$"""
				INSERT INTO "public"."users" (
					"id",
					"nick_name",
					"phone",
					"email",
					"password_hash",
					"password_salt",
					"is_enable",
					"is_deleted",
					"last_login_time",
					"last_login_ip",
					"modify_by",
					"modify_time",
					"create_by",
					"create_time")
				SELECT
					'{SeedUserId}'::uuid,
					'用户2515',
					'',
					'{SeedUserEmail}',
					'pfQI6rwsyympJJU3arYEQlQLYWAdibVJ4FJJ28uCK7o=',
					'Axuvzi8c0p/QGCpZv2RQ9Q==',
					true,
					false,
					NULL,
					NULL,
					'{Guid.Empty}'::uuid,
					'2026-03-26 09:45:37.023783+00'::timestamptz,
					'{Guid.Empty}'::uuid,
					'2026-03-26 09:45:37.023783+00'::timestamptz
				WHERE NOT EXISTS (
					SELECT 1
					FROM "public"."users"
					WHERE "id" = '{SeedUserId}'::uuid
					   OR "email" = '{SeedUserEmail}');
				""",
				name: "seed-default-user",
				order: 200);

			// 3. 默认租户
			seeds.AddSql(
				$"""
				INSERT INTO tenants (id, code, name, status, package_id, expire_at_utc, is_deleted, modify_by, modify_time, create_by, create_time)
				SELECT '{DefaultTenantId}'::uuid, 'default', '默认租户', 0, NULL, NULL, false, '{Guid.Empty}'::uuid, NOW(), '{Guid.Empty}'::uuid, NOW()
				WHERE NOT EXISTS (SELECT 1 FROM tenants WHERE id = '{DefaultTenantId}'::uuid);
				""",
				name: "seed-default-tenant",
				order: 250);

			// 4. 用户-租户关联
			seeds.AddSql(
				$"""
				INSERT INTO tenant_users (tenant_id, user_id, role_in_tenant, joined_at_utc)
				SELECT '{DefaultTenantId}'::uuid, '{SeedUserId}'::uuid, 'owner', NOW()
				WHERE NOT EXISTS (
					SELECT 1 FROM tenant_users
					WHERE tenant_id = '{DefaultTenantId}'::uuid AND user_id = '{SeedUserId}'::uuid);
				""",
				name: "seed-tenant-user",
				order: 260);

			// 5. 租户配额
			seeds.AddSql(
				$"""
				INSERT INTO tenant_quotas (id, tenant_id, max_users, max_storage_bytes, max_api_calls_per_day, max_file_size, max_orders_per_day, modify_by, modify_time, create_by, create_time)
				SELECT '{DefaultTenantQuotaId}'::uuid, '{DefaultTenantId}'::uuid, 100, 10737418240, 100000, 104857600, 10000, '{Guid.Empty}'::uuid, NOW(), '{Guid.Empty}'::uuid, NOW()
				WHERE NOT EXISTS (SELECT 1 FROM tenant_quotas WHERE tenant_id = '{DefaultTenantId}'::uuid);
				""",
				name: "seed-tenant-quota",
				order: 270);

			// 6. 管理员角色 + 全部权限 + 种子用户绑定
			seeds.AddSql(
				$"""
				INSERT INTO role (id, code, name, description, is_system, is_default, is_deleted, modify_by, modify_time, create_by, create_time)
				SELECT '{AdminRoleId}'::uuid, 'admin', '系统管理员', '系统初始化管理员角色', true, false, false, '{Guid.Empty}'::uuid, NOW(), '{Guid.Empty}'::uuid, NOW()
				WHERE NOT EXISTS (SELECT 1 FROM role WHERE id = '{AdminRoleId}'::uuid);

				INSERT INTO role_permission (role_id, permission_id)
				SELECT '{AdminRoleId}'::uuid, permission_id
				FROM (VALUES
					-- 系统管理菜单
					('{RootPermissionId}'::uuid),
					('{UserPermissionId}'::uuid),
					('{RolePermissionId}'::uuid),
					('{MenuPermissionId}'::uuid),
					('{OperationLogPermissionId}'::uuid),
					('{TenantMenuPermissionId}'::uuid),
					('{DeadLetterMenuPermissionId}'::uuid),
					-- 用户操作
					('{UserQueryPermissionId}'::uuid),
					('{UserCreatePermissionId}'::uuid),
					('{UserUpdatePermissionId}'::uuid),
					('{UserDeletePermissionId}'::uuid),
					('{UserResetPasswordPermissionId}'::uuid),
					('{UserChangeStatusPermissionId}'::uuid),
					-- 角色操作
					('{RoleQueryPermissionId}'::uuid),
					('{RoleCreatePermissionId}'::uuid),
					('{RoleUpdatePermissionId}'::uuid),
					('{RoleDeletePermissionId}'::uuid),
					-- 菜单操作
					('{MenuQueryPermissionId}'::uuid),
					('{MenuCreatePermissionId}'::uuid),
					('{MenuUpdatePermissionId}'::uuid),
					('{MenuDeletePermissionId}'::uuid),
					-- 操作日志
					('{OperationLogQueryPermissionId}'::uuid),
					-- 租户操作
					('{TenantQueryPermissionId}'::uuid),
					('{TenantCreatePermissionId}'::uuid),
					('{TenantUpdatePermissionId}'::uuid),
					('{TenantDeletePermissionId}'::uuid),
					('{TenantUserPermissionId}'::uuid),
					('{TenantQuotaPermissionId}'::uuid),
					-- 死信队列
					('{DeadLetterQueryPermissionId}'::uuid),
					('{DeadLetterReplayPermissionId}'::uuid),
					-- 订单管理
					('{OrderMenuPermissionId}'::uuid),
					('{OrderListPermissionId}'::uuid),
					('{OrderQueryPermissionId}'::uuid),
					('{OrderCancelPermissionId}'::uuid),
					-- 库存管理
					('{InventoryMenuPermissionId}'::uuid),
					('{InventorySkuPermissionId}'::uuid),
					('{InventorySkuQueryPermissionId}'::uuid),
					('{InventorySkuCreatePermissionId}'::uuid),
					('{InventorySkuStockPermissionId}'::uuid),
					-- 支付管理
					('{PaymentMenuPermissionId}'::uuid),
					('{PaymentRecordPermissionId}'::uuid),
					('{PaymentRecordQueryPermissionId}'::uuid),
					-- 文件管理
					('{FileMenuPermissionId}'::uuid),
					('{FileQueryPermissionId}'::uuid),
					('{FileDeletePermissionId}'::uuid)
				) AS seeded(permission_id)
				WHERE NOT EXISTS (
					SELECT 1
					FROM role_permission rp
					WHERE rp.role_id = '{AdminRoleId}'::uuid
					  AND rp.permission_id = seeded.permission_id);

				INSERT INTO user_role (user_id, role_id)
				SELECT '{SeedUserId}'::uuid, '{AdminRoleId}'::uuid
				WHERE NOT EXISTS (
					SELECT 1
					FROM user_role ur
					WHERE ur.user_id = '{SeedUserId}'::uuid
					  AND ur.role_id = '{AdminRoleId}'::uuid);
				""",
				name: "seed-admin-role",
				order: 300);
		});
	}
}
