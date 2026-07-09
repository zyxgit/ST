using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST.MS.FileUpload.Infra.Migrations
{
    /// <inheritdoc />
    public partial class DropUploadIdColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_upload_chunks_session_id",
                table: "upload_chunks");

            migrationBuilder.DropIndex(
                name: "ix_upload_chunks_upload_id_chunk_index",
                table: "upload_chunks");

            migrationBuilder.DropColumn(
                name: "upload_id",
                table: "upload_chunks");

            migrationBuilder.CreateIndex(
                name: "ix_upload_chunks_session_id_chunk_index",
                table: "upload_chunks",
                columns: new[] { "session_id", "chunk_index" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_upload_chunks_session_id_chunk_index",
                table: "upload_chunks");

            migrationBuilder.AddColumn<Guid>(
                name: "upload_id",
                table: "upload_chunks",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "ix_upload_chunks_session_id",
                table: "upload_chunks",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "ix_upload_chunks_upload_id_chunk_index",
                table: "upload_chunks",
                columns: new[] { "upload_id", "chunk_index" },
                unique: true);
        }
    }
}
