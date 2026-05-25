using Microsoft.Extensions.DependencyInjection;
using ST.Infra.EntityFramework.Npgsql.Extensions;
using ST.MS.Identity.Infra.DbContext;
using ST.Shared.Module;

namespace ST.MS.Identity.Infra;

public sealed class InfraModule : ServiceModule
{
	private const string AdminRoleId = "11111111-1111-1111-1111-111111111111";
	private const string RootPermissionId = "11111111-1111-1111-1111-111111111201";
	private const string UserPermissionId = "11111111-1111-1111-1111-111111111202";
	private const string RolePermissionId = "11111111-1111-1111-1111-111111111203";
	private const string MenuPermissionId = "11111111-1111-1111-1111-111111111204";
	private const string UserQueryPermissionId = "11111111-1111-1111-1111-111111111212";
	private const string UserCreatePermissionId = "11111111-1111-1111-1111-111111111213";
	private const string UserUpdatePermissionId = "11111111-1111-1111-1111-111111111214";
	private const string UserDeletePermissionId = "11111111-1111-1111-1111-111111111215";
	private const string UserResetPasswordPermissionId = "11111111-1111-1111-1111-111111111216";
	private const string UserChangeStatusPermissionId = "11111111-1111-1111-1111-111111111217";
	private const string RoleQueryPermissionId = "11111111-1111-1111-1111-111111111222";
	private const string RoleCreatePermissionId = "11111111-1111-1111-1111-111111111223";
	private const string RoleUpdatePermissionId = "11111111-1111-1111-1111-111111111224";
	private const string RoleDeletePermissionId = "11111111-1111-1111-1111-111111111225";
	private const string MenuQueryPermissionId = "11111111-1111-1111-1111-111111111232";
	private const string MenuCreatePermissionId = "11111111-1111-1111-1111-111111111233";
	private const string MenuUpdatePermissionId = "11111111-1111-1111-1111-111111111234";
	private const string MenuDeletePermissionId = "11111111-1111-1111-1111-111111111235";
	private const string OperationLogPermissionId = "11111111-1111-1111-1111-111111111205";
	private const string OperationLogQueryPermissionId = "11111111-1111-1111-1111-111111111242";
	private const string SeedUserId = "019d2988-fd04-7510-ae5b-61bff91c18cf";
	private const string SeedUserEmail = "test@qq.com";

	public override void ConfigureServices(IServiceCollection services)
	{
		base.ConfigureServices(services);

		services.AddNpgsqlDbContextFromConfig<IdentityDbContext>(seeds =>
		{
			seeds.AddSqlFile("Seeds/001_permissions.sql", order: 100);
			seeds.AddSql(
				$"""
				INSERT INTO role (id, code, name, description, is_system, is_default, is_deleted, modify_by, modify_time, create_by, create_time)
				SELECT '{AdminRoleId}'::uuid, 'admin', '系统管理员', '系统初始化管理员角色', true, false, false, '{Guid.Empty}'::uuid, NOW(), '{Guid.Empty}'::uuid, NOW()
				WHERE NOT EXISTS (SELECT 1 FROM role WHERE id = '{AdminRoleId}'::uuid);

				INSERT INTO role_permission (role_id, permission_id)
				SELECT '{AdminRoleId}'::uuid, permission_id
				FROM (VALUES
					('{RootPermissionId}'::uuid),
					('{UserPermissionId}'::uuid),
					('{RolePermissionId}'::uuid),
					('{MenuPermissionId}'::uuid),
					('{UserQueryPermissionId}'::uuid),
					('{UserCreatePermissionId}'::uuid),
					('{UserUpdatePermissionId}'::uuid),
					('{UserDeletePermissionId}'::uuid),
					('{UserResetPasswordPermissionId}'::uuid),
					('{UserChangeStatusPermissionId}'::uuid),
					('{RoleQueryPermissionId}'::uuid),
					('{RoleCreatePermissionId}'::uuid),
					('{RoleUpdatePermissionId}'::uuid),
					('{RoleDeletePermissionId}'::uuid),
					('{MenuQueryPermissionId}'::uuid),
					('{MenuCreatePermissionId}'::uuid),
					('{MenuUpdatePermissionId}'::uuid),
					('{MenuDeletePermissionId}'::uuid),
					('{OperationLogPermissionId}'::uuid),
					('{OperationLogQueryPermissionId}'::uuid)
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
		});
	}
}
