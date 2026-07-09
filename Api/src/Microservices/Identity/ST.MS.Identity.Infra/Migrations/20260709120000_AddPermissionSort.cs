using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST.MS.Identity.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddPermissionSort : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "sort",
                table: "permissions",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                comment: "排序号（越小越靠前）");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "sort",
                table: "permissions");
        }
    }
}
