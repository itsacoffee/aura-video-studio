using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aura.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddActionLogAndSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "ProjectStates",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedByUserId",
                table: "ProjectStates",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ProjectStates",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ActionLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ActionType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    AffectedResourceIds = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    PayloadJson = table.Column<string>(type: "TEXT", nullable: true),
                    InverseActionType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    InversePayloadJson = table.Column<string>(type: "TEXT", nullable: true),
                    CanBatch = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsPersistent = table.Column<bool>(type: "INTEGER", nullable: false),
                    UndoneAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UndoneByUserId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true),
                    CorrelationId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActionLogs", x => x.Id);
                });

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
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedByUserId = table.Column<string>(type: "TEXT", nullable: true),
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
                name: "IX_ProjectStates_IsDeleted",
                table: "ProjectStates",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectStates_IsDeleted_DeletedAt",
                table: "ProjectStates",
                columns: new[] { "IsDeleted", "DeletedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ActionLogs_ActionType",
                table: "ActionLogs",
                column: "ActionType");

            migrationBuilder.CreateIndex(
                name: "IX_ActionLogs_CorrelationId",
                table: "ActionLogs",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_ActionLogs_ExpiresAt",
                table: "ActionLogs",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ActionLogs_Status",
                table: "ActionLogs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ActionLogs_Status_Timestamp",
                table: "ActionLogs",
                columns: new[] { "Status", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_ActionLogs_Timestamp",
                table: "ActionLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_ActionLogs_UserId",
                table: "ActionLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ActionLogs_UserId_Timestamp",
                table: "ActionLogs",
                columns: new[] { "UserId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomTemplates_Category",
                table: "CustomTemplates",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_CustomTemplates_Category_CreatedAt",
                table: "CustomTemplates",
                columns: new[] { "Category", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomTemplates_CreatedAt",
                table: "CustomTemplates",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CustomTemplates_IsDefault",
                table: "CustomTemplates",
                column: "IsDefault");

            migrationBuilder.CreateIndex(
                name: "IX_CustomTemplates_IsDeleted",
                table: "CustomTemplates",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_CustomTemplates_IsDeleted_DeletedAt",
                table: "CustomTemplates",
                columns: new[] { "IsDeleted", "DeletedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActionLogs");

            migrationBuilder.DropTable(
                name: "CustomTemplates");

            migrationBuilder.DropIndex(
                name: "IX_ProjectStates_IsDeleted",
                table: "ProjectStates");

            migrationBuilder.DropIndex(
                name: "IX_ProjectStates_IsDeleted_DeletedAt",
                table: "ProjectStates");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "ProjectStates");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "ProjectStates");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ProjectStates");
        }
    }
}
