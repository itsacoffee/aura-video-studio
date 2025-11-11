using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace Aura.Core.Data.Migrations
{
    /// <summary>
    /// Initial database schema migration for Aura Video Studio
    /// </summary>
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExportHistory",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", nullable: true),
                    FileSize = table.Column<long>(type: "INTEGER", nullable: true),
                    Format = table.Column<string>(type: "TEXT", nullable: true),
                    Resolution = table.Column<string>(type: "TEXT", nullable: true),
                    Duration = table.Column<double>(type: "REAL", nullable: true),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExportHistory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Templates",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Category = table.Column<string>(type: "TEXT", nullable: false),
                    SubCategory = table.Column<string>(type: "TEXT", nullable: true),
                    IsSystemTemplate = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsCommunityTemplate = table.Column<bool>(type: "INTEGER", nullable: false),
                    ThumbnailUrl = table.Column<string>(type: "TEXT", nullable: true),
                    ConfigurationJson = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Templates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IsSetupComplete = table.Column<bool>(type: "INTEGER", nullable: false),
                    FFmpegPath = table.Column<string>(type: "TEXT", nullable: true),
                    OutputDirectory = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemConfigurations", x => x.Id);
                });

            // Insert default system configuration
            migrationBuilder.InsertData(
                table: "SystemConfigurations",
                columns: new[] { "Id", "IsSetupComplete", "FFmpegPath", "OutputDirectory", "CreatedAt", "UpdatedAt" },
                values: new object[] { 1, false, null, 
                    System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        "AuraVideoStudio",
                        "Output"
                    ), 
                    DateTime.UtcNow, DateTime.UtcNow }
            );

            // Create indexes for performance
            migrationBuilder.CreateIndex(
                name: "IX_ExportHistory_Status",
                table: "ExportHistory",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ExportHistory_CreatedAt",
                table: "ExportHistory",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Templates_Category",
                table: "Templates",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Templates_IsSystemTemplate",
                table: "Templates",
                column: "IsSystemTemplate");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ExportHistory");
            migrationBuilder.DropTable(name: "Templates");
            migrationBuilder.DropTable(name: "SystemConfigurations");
        }
    }
}
