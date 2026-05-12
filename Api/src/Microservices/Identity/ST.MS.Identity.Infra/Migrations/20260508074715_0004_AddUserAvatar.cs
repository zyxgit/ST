using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST.MS.Identity.Infra.Migrations
{
    /// <inheritdoc />
    public partial class _0004_AddUserAvatar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "avatar_file_id",
                table: "users",
                type: "uuid",
                nullable: true,
                comment: "头像文件ID（来自 FileUpload 服务）");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "avatar_file_id",
                table: "users");
        }
    }
}
