using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST.MS.Inventory.Infra.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "inbox_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    consumer = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    event_type = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    received_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    retry_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inbox_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "inventory_freeze_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sku_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_freeze_records", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    aggregate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    retry_count = table.Column<int>(type: "integer", nullable: false),
                    next_retry_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    occurred_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    sent_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    trace_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "skus",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sku_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    available = table.Column<int>(type: "integer", nullable: false),
                    frozen = table.Column<int>(type: "integer", nullable: false),
                    sold = table.Column<int>(type: "integer", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    create_by = table.Column<Guid>(type: "uuid", nullable: false),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modify_by = table.Column<Guid>(type: "uuid", nullable: false),
                    modify_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_skus", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_inbox_messages_message_id_consumer",
                table: "inbox_messages",
                columns: new[] { "message_id", "consumer" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_inbox_messages_processed_at_utc",
                table: "inbox_messages",
                column: "processed_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_freeze_records_order_id",
                table: "inventory_freeze_records",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_freeze_records_order_id_sku_id",
                table: "inventory_freeze_records",
                columns: new[] { "order_id", "sku_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_inventory_freeze_records_sku_id",
                table: "inventory_freeze_records",
                column: "sku_id");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_next_retry_at_utc",
                table: "outbox_messages",
                column: "next_retry_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_status",
                table: "outbox_messages",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_status_next_retry",
                table: "outbox_messages",
                columns: new[] { "status", "next_retry_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_skus_sku_id",
                table: "skus",
                column: "sku_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inbox_messages");

            migrationBuilder.DropTable(
                name: "inventory_freeze_records");

            migrationBuilder.DropTable(
                name: "outbox_messages");

            migrationBuilder.DropTable(
                name: "skus");
        }
    }
}
