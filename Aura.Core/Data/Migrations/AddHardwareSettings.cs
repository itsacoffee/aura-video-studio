using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace Aura.Core.Data.Migrations
{
    /// <summary>
    /// Migration to add HardwareSettings table for hardware performance settings
    /// </summary>
    public partial class AddHardwareSettings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HardwareSettings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    SettingsJson = table.Column<string>(type: "TEXT", nullable: false),
                    Version = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HardwareSettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HardwareSettings_UpdatedAt",
                table: "HardwareSettings",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_HardwareSettings_Version",
                table: "HardwareSettings",
                column: "Version");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "HardwareSettings");
        }
    }
}
