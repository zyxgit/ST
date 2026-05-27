using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST.MS.Identity.Infra.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "permissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    p_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "父级权限Id"),
                    code = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, comment: "权限编码"),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, comment: "权限名称"),
                    type = table.Column<int>(type: "integer", nullable: false, comment: "权限类型"),
                    path = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true, comment: "路由"),
                    menu_icon = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true, comment: "图标"),
                    component = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true, comment: "组件路径"),
                    is_link = table.Column<bool>(type: "boolean", nullable: false, comment: "是否外链"),
                    keep_alive = table.Column<bool>(type: "boolean", nullable: false, comment: "是否缓存"),
                    is_hide = table.Column<bool>(type: "boolean", nullable: false, comment: "是否隐藏"),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, comment: "是否删除"),
                    create_by = table.Column<Guid>(type: "uuid", nullable: false),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modify_by = table.Column<Guid>(type: "uuid", nullable: false),
                    modify_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_permissions", x => x.id);
                },
                comment: "权限表");

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false, comment: "RefreshToken 的 SHA256(Base64)"),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    revoked_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_ip = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    revoked_by_ip = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    replaced_by_token_hash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_tokens", x => x.id);
                },
                comment: "刷新 Token（仅保存哈希值，不保存明文）");

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nick_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, comment: "昵称"),
                    phone = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, comment: "手机号"),
                    email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, comment: "邮箱"),
                    password_hash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    password_salt = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_enable = table.Column<bool>(type: "boolean", nullable: false, comment: "激活状态"),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, comment: "是否已删除"),
                    last_login_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "最后登录时间"),
                    last_login_ip = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true, comment: "最后登录IP"),
                    avatar_file_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "头像文件ID（来自 FileUpload 服务）"),
                    create_by = table.Column<Guid>(type: "uuid", nullable: false),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modify_by = table.Column<Guid>(type: "uuid", nullable: false),
                    modify_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                },
                comment: "用户信息");

            migrationBuilder.CreateTable(
                name: "role",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, comment: "角色编码"),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, comment: "角色名称"),
                    description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, comment: "角色描述"),
                    is_system = table.Column<bool>(type: "boolean", nullable: false, comment: "是否系统角色"),
                    is_default = table.Column<bool>(type: "boolean", nullable: false, comment: "是否默认角色"),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, comment: "是否删除"),
                    role_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    create_by = table.Column<Guid>(type: "uuid", nullable: false),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modify_by = table.Column<Guid>(type: "uuid", nullable: false),
                    modify_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role", x => x.id);
                    table.ForeignKey(
                        name: "fk_role_role_role_id",
                        column: x => x.role_id,
                        principalTable: "role",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_role_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                },
                comment: "角色信息");

            migrationBuilder.CreateTable(
                name: "role_permission",
                columns: table => new
                {
                    role_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "角色id"),
                    permission_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "权限id")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_permission", x => new { x.role_id, x.permission_id });
                    table.ForeignKey(
                        name: "fk_role_permission_permissions_permission_id",
                        column: x => x.permission_id,
                        principalTable: "permissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_role_permission_role_role_id",
                        column: x => x.role_id,
                        principalTable: "role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_role",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "用户Id"),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "角色Id")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_role", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "fk_user_role_role_role_id",
                        column: x => x.role_id,
                        principalTable: "role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_role_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "用户角色");

            migrationBuilder.CreateIndex(
                name: "ix_permissions_code",
                table: "permissions",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_token_hash",
                table: "refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_role_role_id",
                table: "role",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_role_user_id",
                table: "role",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_role_permission_permission_id",
                table: "role_permission",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_role_role_id",
                table: "user_role",
                column: "role_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "role_permission");

            migrationBuilder.DropTable(
                name: "user_role");

            migrationBuilder.DropTable(
                name: "permissions");

            migrationBuilder.DropTable(
                name: "role");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
