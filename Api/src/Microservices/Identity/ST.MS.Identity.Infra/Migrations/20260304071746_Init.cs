using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST.MS.Identity.Infra.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
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
                    modify_by = table.Column<Guid>(type: "uuid", nullable: false),
                    modify_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    create_by = table.Column<Guid>(type: "uuid", nullable: false),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_permissions", x => x.id);
                },
                comment: "权限表");

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
                    modify_by = table.Column<Guid>(type: "uuid", nullable: false),
                    modify_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    create_by = table.Column<Guid>(type: "uuid", nullable: false),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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
                    modify_by = table.Column<Guid>(type: "uuid", nullable: false),
                    modify_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    create_by = table.Column<Guid>(type: "uuid", nullable: false),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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
                        name: "fk_user_role_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "用户角色");

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
                        name: "fk_role_permission_role_role_id",
                        column: x => x.role_id,
                        principalTable: "role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_permissions_code",
                table: "permissions",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_role_role_id",
                table: "role",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_role_user_id",
                table: "role",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "permissions");

            migrationBuilder.DropTable(
                name: "role_permission");

            migrationBuilder.DropTable(
                name: "user_role");

            migrationBuilder.DropTable(
                name: "role");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
