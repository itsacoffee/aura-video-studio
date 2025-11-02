using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aura.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomTemplatesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomTemplates",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT", nullable: false),
                    Tags = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Author = table.Column<string>(type: "TEXT", nullable: false),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    ScriptStructureJson = table.Column<string>(type: "TEXT", nullable: false),
                    VideoStructureJson = table.Column<string>(type: "TEXT", nullable: false),
                    LLMPipelineJson = table.Column<string>(type: "TEXT", nullable: false),
                    VisualPreferencesJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomTemplates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomTemplates_Category",
                table: "CustomTemplates",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_CustomTemplates_IsDefault",
                table: "CustomTemplates",
                column: "IsDefault");

            migrationBuilder.CreateIndex(
                name: "IX_CustomTemplates_CreatedAt",
                table: "CustomTemplates",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CustomTemplates_Category_CreatedAt",
                table: "CustomTemplates",
                columns: new[] { "Category", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomTemplates");
        }
    }
}
