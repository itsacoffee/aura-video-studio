using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aura.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "export_history",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    input_file = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    output_file = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    preset_name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    progress = table.Column<double>(type: "REAL", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    started_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    completed_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    error_message = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    file_size = table.Column<long>(type: "INTEGER", nullable: true),
                    duration_seconds = table.Column<double>(type: "REAL", nullable: true),
                    platform = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    resolution = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    codec = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_export_history", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_export_history_created_at",
                table: "export_history",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_export_history_status",
                table: "export_history",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_export_history_status_created_at",
                table: "export_history",
                columns: new[] { "status", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "export_history");
        }
    }
}
