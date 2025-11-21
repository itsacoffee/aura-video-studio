using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace Aura.Core.Data.Migrations
{
    /// <summary>
    /// Migration to add ProviderConfigurations table for encrypted provider configuration storage
    /// </summary>
    public partial class AddProviderConfigurations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProviderConfigurations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ConfigJson = table.Column<string>(type: "TEXT", nullable: false),
                    IsEncrypted = table.Column<bool>(type: "INTEGER", nullable: false),
                    Version = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderConfigurations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProviderConfigurations_UpdatedAt",
                table: "ProviderConfigurations",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderConfigurations_Version",
                table: "ProviderConfigurations",
                column: "Version");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ProviderConfigurations");
        }
    }
}
