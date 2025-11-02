using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aura.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectStatePersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProjectStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CurrentStage = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ProgressPercent = table.Column<int>(type: "INTEGER", nullable: false),
                    JobId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    BriefJson = table.Column<string>(type: "TEXT", nullable: true),
                    PlanSpecJson = table.Column<string>(type: "TEXT", nullable: true),
                    VoiceSpecJson = table.Column<string>(type: "TEXT", nullable: true),
                    RenderSpecJson = table.Column<string>(type: "TEXT", nullable: true),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectStates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AssetStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AssetType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    MimeType = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    IsTemporary = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssetStates_ProjectStates_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "ProjectStates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RenderCheckpoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StageName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CheckpointTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedScenes = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalScenes = table.Column<int>(type: "INTEGER", nullable: false),
                    CheckpointData = table.Column<string>(type: "TEXT", nullable: true),
                    OutputFilePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsValid = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RenderCheckpoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RenderCheckpoints_ProjectStates_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "ProjectStates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SceneStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SceneIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    ScriptText = table.Column<string>(type: "TEXT", nullable: false),
                    AudioFilePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ImageFilePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    DurationSeconds = table.Column<double>(type: "REAL", nullable: false),
                    IsCompleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SceneStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SceneStates_ProjectStates_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "ProjectStates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssetStates_IsTemporary",
                table: "AssetStates",
                column: "IsTemporary");

            migrationBuilder.CreateIndex(
                name: "IX_AssetStates_ProjectId",
                table: "AssetStates",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetStates_ProjectId_AssetType",
                table: "AssetStates",
                columns: new[] { "ProjectId", "AssetType" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectStates_JobId",
                table: "ProjectStates",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectStates_Status",
                table: "ProjectStates",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectStates_Status_UpdatedAt",
                table: "ProjectStates",
                columns: new[] { "Status", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectStates_UpdatedAt",
                table: "ProjectStates",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RenderCheckpoints_CheckpointTime",
                table: "RenderCheckpoints",
                column: "CheckpointTime");

            migrationBuilder.CreateIndex(
                name: "IX_RenderCheckpoints_ProjectId",
                table: "RenderCheckpoints",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_RenderCheckpoints_ProjectId_StageName",
                table: "RenderCheckpoints",
                columns: new[] { "ProjectId", "StageName" });

            migrationBuilder.CreateIndex(
                name: "IX_SceneStates_ProjectId",
                table: "SceneStates",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_SceneStates_ProjectId_SceneIndex",
                table: "SceneStates",
                columns: new[] { "ProjectId", "SceneIndex" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssetStates");

            migrationBuilder.DropTable(
                name: "RenderCheckpoints");

            migrationBuilder.DropTable(
                name: "SceneStates");

            migrationBuilder.DropTable(
                name: "ProjectStates");
        }
    }
}
