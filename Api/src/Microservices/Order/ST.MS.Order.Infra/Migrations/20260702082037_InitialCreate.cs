using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST.MS.Order.Infra.Migrations
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
                name: "orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    saga_instance_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cancel_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    create_by = table.Column<Guid>(type: "uuid", nullable: false),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modify_by = table.Column<Guid>(type: "uuid", nullable: false),
                    modify_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_orders", x => x.id);
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
                name: "saga_instances",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_id = table.Column<Guid>(type: "uuid", nullable: false),
                    saga_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    current_step = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    retry_count = table.Column<int>(type: "integer", nullable: false),
                    last_error = table.Column<string>(type: "text", nullable: true),
                    create_by = table.Column<Guid>(type: "uuid", nullable: false),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modify_by = table.Column<Guid>(type: "uuid", nullable: false),
                    modify_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_saga_instances", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "order_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sku_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_order_items_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "saga_steps",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    saga_id = table.Column<Guid>(type: "uuid", nullable: false),
                    step_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    request_json = table.Column<string>(type: "jsonb", nullable: true),
                    response_json = table.Column<string>(type: "jsonb", nullable: true),
                    compensation_event = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    saga_instance_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_saga_steps", x => x.id);
                    table.ForeignKey(
                        name: "fk_saga_steps_saga_instances_saga_instance_id",
                        column: x => x.saga_instance_id,
                        principalTable: "saga_instances",
                        principalColumn: "id");
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
                name: "ix_order_items_order_id",
                table: "order_items",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_orders_order_no",
                table: "orders",
                column: "order_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_orders_status",
                table: "orders",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_orders_user_id",
                table: "orders",
                column: "user_id");

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
                name: "ix_saga_instances_business_id",
                table: "saga_instances",
                column: "business_id");

            migrationBuilder.CreateIndex(
                name: "ix_saga_instances_status",
                table: "saga_instances",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_saga_steps_saga_instance_id",
                table: "saga_steps",
                column: "saga_instance_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inbox_messages");

            migrationBuilder.DropTable(
                name: "order_items");

            migrationBuilder.DropTable(
                name: "outbox_messages");

            migrationBuilder.DropTable(
                name: "saga_steps");

            migrationBuilder.DropTable(
                name: "orders");

            migrationBuilder.DropTable(
                name: "saga_instances");
        }
    }
}
