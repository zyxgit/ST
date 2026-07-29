using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST.MS.Payment.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderNoToPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "order_no",
                table: "payments",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_payments_order_no",
                table: "payments",
                column: "order_no");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_payments_order_no",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "order_no",
                table: "payments");
        }
    }
}
