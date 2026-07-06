using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST.MS.Identity.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. 创建 tenants 表
            migrationBuilder.CreateTable(
                name: "tenants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, comment: "租户编码（唯一标识，如 \"acme\"）"),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, comment: "租户名称"),
                    status = table.Column<int>(type: "integer", nullable: false, comment: "租户状态"),
                    package_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true, comment: "套餐 ID（预留）"),
                    expire_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "过期时间"),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, comment: "是否已删除"),
                    create_by = table.Column<Guid>(type: "uuid", nullable: false),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modify_by = table.Column<Guid>(type: "uuid", nullable: false),
                    modify_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenants", x => x.id);
                },
                comment: "租户");

            migrationBuilder.CreateIndex(
                name: "ix_tenants_code",
                table: "tenants",
                column: "code",
                unique: true);

            // 2. 创建 tenant_users 表
            migrationBuilder.CreateTable(
                name: "tenant_users",
                columns: table => new
                {
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "租户 ID"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "用户 ID"),
                    role_in_tenant = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true, comment: "租户内角色（owner / admin / member）"),
                    joined_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "加入时间")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_users", x => new { x.tenant_id, x.user_id });
                    table.ForeignKey(
                        name: "fk_tenant_users_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "租户用户关联");

            // 3. 创建 tenant_quotas 表
            migrationBuilder.CreateTable(
                name: "tenant_quotas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "租户 ID"),
                    max_users = table.Column<int>(type: "integer", nullable: false, comment: "用户数上限"),
                    max_storage_bytes = table.Column<long>(type: "bigint", nullable: false, comment: "存储容量上限（字节）"),
                    max_api_calls_per_day = table.Column<int>(type: "integer", nullable: false, comment: "每日 API 调用上限"),
                    max_file_size = table.Column<long>(type: "bigint", nullable: false, comment: "单文件大小上限（字节）"),
                    max_orders_per_day = table.Column<int>(type: "integer", nullable: false, comment: "每日订单上限"),
                    create_by = table.Column<Guid>(type: "uuid", nullable: false),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modify_by = table.Column<Guid>(type: "uuid", nullable: false),
                    modify_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_quotas", x => x.id);
                },
                comment: "租户配额");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_quotas_tenant_id",
                table: "tenant_quotas",
                column: "tenant_id",
                unique: true);

            // 4. 给 users 表添加 lock_reason 和 locked_at_utc 字段
            migrationBuilder.AddColumn<string>(
                name: "lock_reason",
                table: "users",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                comment: "锁定原因（如 \"login_fail_exceeded\"、\"admin_disable\"）");

            migrationBuilder.AddColumn<DateTime>(
                name: "locked_at_utc",
                table: "users",
                type: "timestamp with time zone",
                nullable: true,
                comment: "锁定时间");

            // 5. 给 refresh_tokens 表添加 tenant_id 和 tenant_code 字段
            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "refresh_tokens",
                type: "uuid",
                nullable: true,
                comment: "租户 ID（登录时指定的租户）");

            migrationBuilder.AddColumn<string>(
                name: "tenant_code",
                table: "refresh_tokens",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                comment: "租户编码");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "tenant_quotas");
            migrationBuilder.DropTable(name: "tenant_users");
            migrationBuilder.DropTable(name: "tenants");

            migrationBuilder.DropColumn(name: "lock_reason", table: "users");
            migrationBuilder.DropColumn(name: "locked_at_utc", table: "users");

            migrationBuilder.DropColumn(name: "tenant_id", table: "refresh_tokens");
            migrationBuilder.DropColumn(name: "tenant_code", table: "refresh_tokens");
        }
    }
}
