using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST.MS.Identity.Infra.Migrations
{
    /// <inheritdoc />
    public partial class RemoveForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_role_users_user_id",
                table: "role");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "role",
                newName: "user_id1");

            migrationBuilder.RenameIndex(
                name: "ix_role_user_id",
                table: "role",
                newName: "ix_role_user_id1");

            migrationBuilder.AddForeignKey(
                name: "fk_role_users_user_id1",
                table: "role",
                column: "user_id1",
                principalTable: "users",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_role_users_user_id1",
                table: "role");

            migrationBuilder.RenameColumn(
                name: "user_id1",
                table: "role",
                newName: "user_id");

            migrationBuilder.RenameIndex(
                name: "ix_role_user_id1",
                table: "role",
                newName: "ix_role_user_id");

            migrationBuilder.AddForeignKey(
                name: "fk_role_users_user_id",
                table: "role",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id");
        }
    }
}
