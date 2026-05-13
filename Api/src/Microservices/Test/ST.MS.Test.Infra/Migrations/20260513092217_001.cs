using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST.MS.Test.Infra.Migrations
{
	/// <inheritdoc />
	public partial class _001 : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.AddColumn<Guid>(
				name: "create_by",
				table: "tests",
				type: "uuid",
				nullable: false,
				defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

			migrationBuilder.AddColumn<DateTime>(
				name: "create_time",
				table: "tests",
				type: "timestamp with time zone",
				nullable: false,
				defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropColumn(
				name: "create_by",
				table: "tests");

			migrationBuilder.DropColumn(
				name: "create_time",
				table: "tests");
		}
	}
}
