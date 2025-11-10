using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aura.Api.Migrations;

/// <summary>
/// Migration to add project management enhancements (PR #5)
/// Adds tags, categories, output paths, and thumbnails to ProjectStateEntity
/// </summary>
public partial class AddProjectManagementEnhancements : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Add new columns to ProjectStates table
        migrationBuilder.AddColumn<string>(
            name: "Tags",
            table: "ProjectStates",
            type: "TEXT",
            maxLength: 1000,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "OutputFilePath",
            table: "ProjectStates",
            type: "TEXT",
            maxLength: 1000,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ThumbnailPath",
            table: "ProjectStates",
            type: "TEXT",
            maxLength: 1000,
            nullable: true);

        migrationBuilder.AddColumn<double>(
            name: "DurationSeconds",
            table: "ProjectStates",
            type: "REAL",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "TemplateId",
            table: "ProjectStates",
            type: "TEXT",
            maxLength: 50,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Category",
            table: "ProjectStates",
            type: "TEXT",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "LastAutoSaveAt",
            table: "ProjectStates",
            type: "TEXT",
            nullable: true);

        // Add indexes for new columns
        migrationBuilder.CreateIndex(
            name: "IX_ProjectStates_Category",
            table: "ProjectStates",
            column: "Category");

        migrationBuilder.CreateIndex(
            name: "IX_ProjectStates_TemplateId",
            table: "ProjectStates",
            column: "TemplateId");

        migrationBuilder.CreateIndex(
            name: "IX_ProjectStates_CreatedAt",
            table: "ProjectStates",
            column: "CreatedAt");

        migrationBuilder.CreateIndex(
            name: "IX_ProjectStates_Category_CreatedAt",
            table: "ProjectStates",
            columns: new[] { "Category", "CreatedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_ProjectStates_Status_Category",
            table: "ProjectStates",
            columns: new[] { "Status", "Category" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Drop indexes
        migrationBuilder.DropIndex(
            name: "IX_ProjectStates_Category",
            table: "ProjectStates");

        migrationBuilder.DropIndex(
            name: "IX_ProjectStates_TemplateId",
            table: "ProjectStates");

        migrationBuilder.DropIndex(
            name: "IX_ProjectStates_CreatedAt",
            table: "ProjectStates");

        migrationBuilder.DropIndex(
            name: "IX_ProjectStates_Category_CreatedAt",
            table: "ProjectStates");

        migrationBuilder.DropIndex(
            name: "IX_ProjectStates_Status_Category",
            table: "ProjectStates");

        // Drop columns
        migrationBuilder.DropColumn(
            name: "Tags",
            table: "ProjectStates");

        migrationBuilder.DropColumn(
            name: "OutputFilePath",
            table: "ProjectStates");

        migrationBuilder.DropColumn(
            name: "ThumbnailPath",
            table: "ProjectStates");

        migrationBuilder.DropColumn(
            name: "DurationSeconds",
            table: "ProjectStates");

        migrationBuilder.DropColumn(
            name: "TemplateId",
            table: "ProjectStates");

        migrationBuilder.DropColumn(
            name: "Category",
            table: "ProjectStates");

        migrationBuilder.DropColumn(
            name: "LastAutoSaveAt",
            table: "ProjectStates");
    }
}
