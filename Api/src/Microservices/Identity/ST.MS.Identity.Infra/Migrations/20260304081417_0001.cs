using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST.MS.Identity.Infra.Migrations
{
    /// <inheritdoc />
    public partial class _0001 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_user_role_role_id",
                table: "user_role",
                column: "role_id");

            migrationBuilder.AddForeignKey(
                name: "fk_user_role_role_role_id",
                table: "user_role",
                column: "role_id",
                principalTable: "role",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_user_role_role_role_id",
                table: "user_role");

            migrationBuilder.DropIndex(
                name: "ix_user_role_role_id",
                table: "user_role");
        }
    }
}
