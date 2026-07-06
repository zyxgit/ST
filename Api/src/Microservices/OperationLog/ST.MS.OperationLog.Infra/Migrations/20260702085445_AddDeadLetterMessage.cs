using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST.MS.OperationLog.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddDeadLetterMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "operation_logs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "dead_letter_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    original_message = table.Column<string>(type: "jsonb", nullable: false),
                    queue_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    exchange_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    routing_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    error_stack_trace = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    retry_count = table.Column<int>(type: "integer", nullable: false),
                    max_retry_count = table.Column<int>(type: "integer", nullable: false),
                    message_created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    replayed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    replay_result = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dead_letter_messages", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_dead_letter_messages_created_at_utc",
                table: "dead_letter_messages",
                column: "created_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_dead_letter_messages_queue_name_created_at_utc",
                table: "dead_letter_messages",
                columns: new[] { "queue_name", "created_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dead_letter_messages");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "operation_logs");
        }
    }
}
