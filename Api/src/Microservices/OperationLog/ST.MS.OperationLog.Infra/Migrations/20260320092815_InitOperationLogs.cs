using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ST.MS.OperationLog.Infra.Migrations
{
    /// <inheritdoc />
    public partial class InitOperationLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "operation_logs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    service_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    trace_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    span_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    operation_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    path = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    method = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status_code = table.Column<int>(type: "integer", nullable: false),
                    success = table.Column<bool>(type: "boolean", nullable: false),
                    duration_ms = table.Column<long>(type: "bigint", nullable: false),
                    request_json = table.Column<string>(type: "jsonb", nullable: true),
                    response_json = table.Column<string>(type: "jsonb", nullable: true),
                    exception_type = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    exception_message = table.Column<string>(type: "text", nullable: true),
                    exception_stack_trace = table.Column<string>(type: "text", nullable: true),
                    tags_json = table.Column<string>(type: "jsonb", nullable: true),
                    extra_json = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_operation_logs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_operation_logs_created_at_utc",
                table: "operation_logs",
                column: "created_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_operation_logs_operation_name",
                table: "operation_logs",
                column: "operation_name");

            migrationBuilder.CreateIndex(
                name: "ix_operation_logs_trace_id",
                table: "operation_logs",
                column: "trace_id");

            migrationBuilder.CreateIndex(
                name: "ix_operation_logs_user_id",
                table: "operation_logs",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "operation_logs");
        }
    }
}
