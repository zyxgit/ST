using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST.MS.FileUpload.Infra.Migrations
{
    /// <inheritdoc />
    public partial class CheckPendingChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "uploader_name",
                table: "files",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "file_path",
                table: "files",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "file_name",
                table: "files",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "extension",
                table: "files",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<string>(
                name: "file_hash",
                table: "files",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "files",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "upload_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    file_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    file_size = table.Column<long>(type: "bigint", nullable: false),
                    chunk_size = table.Column<int>(type: "integer", nullable: false),
                    total_chunks = table.Column<int>(type: "integer", nullable: false),
                    uploaded_chunks = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    creator_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    access_level = table.Column<int>(type: "integer", nullable: false),
                    file_id = table.Column<Guid>(type: "uuid", nullable: true),
                    error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_upload_sessions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "upload_chunks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    upload_id = table.Column<Guid>(type: "uuid", nullable: false),
                    chunk_index = table.Column<int>(type: "integer", nullable: false),
                    chunk_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    size = table.Column<long>(type: "bigint", nullable: false),
                    storage_path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_upload_chunks", x => x.id);
                    table.ForeignKey(
                        name: "fk_upload_chunks_upload_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "upload_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_files_file_hash",
                table: "files",
                column: "file_hash");

            migrationBuilder.CreateIndex(
                name: "ix_upload_chunks_session_id",
                table: "upload_chunks",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "ix_upload_chunks_upload_id_chunk_index",
                table: "upload_chunks",
                columns: new[] { "upload_id", "chunk_index" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_upload_sessions_created_by_status",
                table: "upload_sessions",
                columns: new[] { "created_by", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_upload_sessions_expires_at_utc",
                table: "upload_sessions",
                column: "expires_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_upload_sessions_file_hash",
                table: "upload_sessions",
                column: "file_hash");

            migrationBuilder.CreateIndex(
                name: "ix_upload_sessions_status",
                table: "upload_sessions",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "upload_chunks");

            migrationBuilder.DropTable(
                name: "upload_sessions");

            migrationBuilder.DropIndex(
                name: "ix_files_file_hash",
                table: "files");

            migrationBuilder.DropColumn(
                name: "file_hash",
                table: "files");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "files");

            migrationBuilder.AlterColumn<string>(
                name: "uploader_name",
                table: "files",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "file_path",
                table: "files",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AlterColumn<string>(
                name: "file_name",
                table: "files",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "extension",
                table: "files",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);
        }
    }
}
