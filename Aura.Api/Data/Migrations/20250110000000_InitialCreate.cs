using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aura.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create system_configuration table
            migrationBuilder.CreateTable(
                name: "system_configuration",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    is_setup_complete = table.Column<bool>(type: "INTEGER", nullable: false),
                    ffmpeg_path = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    output_directory = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_system_configuration", x => x.id);
                });

            // Create user_setup table
            migrationBuilder.CreateTable(
                name: "user_setup",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    user_id = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    completed = table.Column<bool>(type: "INTEGER", nullable: false),
                    completed_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    version = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    last_step = table.Column<int>(type: "INTEGER", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    selected_tier = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    wizard_state = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_setup", x => x.id);
                });

            // Create export_history table
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

            // Create templates table
            migrationBuilder.CreateTable(
                name: "templates",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    category = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    sub_category = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    preview_image = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    preview_video = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    tags = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    template_data = table.Column<string>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    author = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    is_system_template = table.Column<bool>(type: "INTEGER", nullable: false),
                    is_community_template = table.Column<bool>(type: "INTEGER", nullable: false),
                    usage_count = table.Column<int>(type: "INTEGER", nullable: false),
                    rating = table.Column<double>(type: "REAL", nullable: false),
                    rating_count = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_templates", x => x.id);
                });

            // Create custom_templates table
            migrationBuilder.CreateTable(
                name: "custom_templates",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    category = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    tags = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    created_by = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    modified_by = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    author = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    is_default = table.Column<bool>(type: "INTEGER", nullable: false),
                    is_deleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    deleted_by = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    script_structure_json = table.Column<string>(type: "TEXT", nullable: false),
                    video_structure_json = table.Column<string>(type: "TEXT", nullable: false),
                    llm_pipeline_json = table.Column<string>(type: "TEXT", nullable: false),
                    visual_preferences_json = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_custom_templates", x => x.id);
                });

            // Create Configurations table
            migrationBuilder.CreateTable(
                name: "Configurations",
                columns: table => new
                {
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT", nullable: false),
                    ValueType = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    IsSensitive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Configurations", x => x.Key);
                });

            // Create ActionLogs table
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

            // Create ProjectStates table
            migrationBuilder.CreateTable(
                name: "ProjectStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    CurrentWizardStep = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
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

            // Create ContentBlobs table
            migrationBuilder.CreateTable(
                name: "ContentBlobs",
                columns: table => new
                {
                    ContentHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    SizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastReferencedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ReferenceCount = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentBlobs", x => x.ContentHash);
                });

            // Create SceneStates table (child of ProjectStates)
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

            // Create AssetStates table (child of ProjectStates)
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

            // Create RenderCheckpoints table (child of ProjectStates)
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

            // Create ProjectVersions table (child of ProjectStates)
            migrationBuilder.CreateTable(
                name: "ProjectVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    VersionNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    VersionType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Trigger = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    BriefJson = table.Column<string>(type: "TEXT", nullable: true),
                    PlanSpecJson = table.Column<string>(type: "TEXT", nullable: true),
                    VoiceSpecJson = table.Column<string>(type: "TEXT", nullable: true),
                    RenderSpecJson = table.Column<string>(type: "TEXT", nullable: true),
                    TimelineJson = table.Column<string>(type: "TEXT", nullable: true),
                    BriefHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    PlanHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    VoiceHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    RenderHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    TimelineHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    StorageSizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    IsMarkedImportant = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectVersions_ProjectStates_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "ProjectStates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create indexes for query performance
            CreateIndexes(migrationBuilder);

            // Seed initial data
            migrationBuilder.InsertData(
                table: "system_configuration",
                columns: new[] { "id", "is_setup_complete", "ffmpeg_path", "output_directory", "created_at", "updated_at" },
                values: new object[] { 
                    1, 
                    false, 
                    null, 
                    System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AuraVideoStudio", "Output"),
                    DateTime.UtcNow,
                    DateTime.UtcNow
                });
        }

        private void CreateIndexes(MigrationBuilder migrationBuilder)
        {
            // ExportHistory indexes
            migrationBuilder.CreateIndex(
                name: "IX_export_history_Status",
                table: "export_history",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_export_history_CreatedAt",
                table: "export_history",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_export_history_Status_CreatedAt",
                table: "export_history",
                columns: new[] { "status", "created_at" });

            // Templates indexes
            migrationBuilder.CreateIndex(
                name: "IX_templates_Category",
                table: "templates",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "IX_templates_IsSystemTemplate",
                table: "templates",
                column: "is_system_template");

            migrationBuilder.CreateIndex(
                name: "IX_templates_IsCommunityTemplate",
                table: "templates",
                column: "is_community_template");

            migrationBuilder.CreateIndex(
                name: "IX_templates_Category_SubCategory",
                table: "templates",
                columns: new[] { "category", "sub_category" });

            // UserSetup indexes
            migrationBuilder.CreateIndex(
                name: "IX_user_setup_UserId",
                table: "user_setup",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_setup_Completed",
                table: "user_setup",
                column: "completed");

            migrationBuilder.CreateIndex(
                name: "IX_user_setup_UpdatedAt",
                table: "user_setup",
                column: "updated_at");

            // CustomTemplates indexes
            migrationBuilder.CreateIndex(
                name: "IX_custom_templates_Category",
                table: "custom_templates",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "IX_custom_templates_IsDefault",
                table: "custom_templates",
                column: "is_default");

            migrationBuilder.CreateIndex(
                name: "IX_custom_templates_CreatedAt",
                table: "custom_templates",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_custom_templates_Category_CreatedAt",
                table: "custom_templates",
                columns: new[] { "category", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_custom_templates_IsDeleted",
                table: "custom_templates",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "IX_custom_templates_IsDeleted_DeletedAt",
                table: "custom_templates",
                columns: new[] { "is_deleted", "deleted_at" });

            // Configuration indexes
            migrationBuilder.CreateIndex(
                name: "IX_Configurations_Category",
                table: "Configurations",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Configurations_IsSensitive",
                table: "Configurations",
                column: "IsSensitive");

            migrationBuilder.CreateIndex(
                name: "IX_Configurations_IsActive",
                table: "Configurations",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Configurations_UpdatedAt",
                table: "Configurations",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Configurations_Category_IsActive",
                table: "Configurations",
                columns: new[] { "Category", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Configurations_Category_UpdatedAt",
                table: "Configurations",
                columns: new[] { "Category", "UpdatedAt" });

            // ActionLogs indexes
            migrationBuilder.CreateIndex(
                name: "IX_ActionLogs_UserId",
                table: "ActionLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ActionLogs_ActionType",
                table: "ActionLogs",
                column: "ActionType");

            migrationBuilder.CreateIndex(
                name: "IX_ActionLogs_Status",
                table: "ActionLogs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ActionLogs_Timestamp",
                table: "ActionLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_ActionLogs_UserId_Timestamp",
                table: "ActionLogs",
                columns: new[] { "UserId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_ActionLogs_Status_Timestamp",
                table: "ActionLogs",
                columns: new[] { "Status", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_ActionLogs_CorrelationId",
                table: "ActionLogs",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_ActionLogs_ExpiresAt",
                table: "ActionLogs",
                column: "ExpiresAt");

            // ProjectStates indexes
            migrationBuilder.CreateIndex(
                name: "IX_ProjectStates_Status",
                table: "ProjectStates",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectStates_UpdatedAt",
                table: "ProjectStates",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectStates_Status_UpdatedAt",
                table: "ProjectStates",
                columns: new[] { "Status", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectStates_JobId",
                table: "ProjectStates",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectStates_IsDeleted",
                table: "ProjectStates",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectStates_IsDeleted_DeletedAt",
                table: "ProjectStates",
                columns: new[] { "IsDeleted", "DeletedAt" });

            // SceneStates indexes
            migrationBuilder.CreateIndex(
                name: "IX_SceneStates_ProjectId",
                table: "SceneStates",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_SceneStates_ProjectId_SceneIndex",
                table: "SceneStates",
                columns: new[] { "ProjectId", "SceneIndex" });

            // AssetStates indexes
            migrationBuilder.CreateIndex(
                name: "IX_AssetStates_ProjectId",
                table: "AssetStates",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetStates_ProjectId_AssetType",
                table: "AssetStates",
                columns: new[] { "ProjectId", "AssetType" });

            migrationBuilder.CreateIndex(
                name: "IX_AssetStates_IsTemporary",
                table: "AssetStates",
                column: "IsTemporary");

            // RenderCheckpoints indexes
            migrationBuilder.CreateIndex(
                name: "IX_RenderCheckpoints_ProjectId",
                table: "RenderCheckpoints",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_RenderCheckpoints_ProjectId_StageName",
                table: "RenderCheckpoints",
                columns: new[] { "ProjectId", "StageName" });

            migrationBuilder.CreateIndex(
                name: "IX_RenderCheckpoints_CheckpointTime",
                table: "RenderCheckpoints",
                column: "CheckpointTime");

            // ProjectVersions indexes
            migrationBuilder.CreateIndex(
                name: "IX_ProjectVersions_ProjectId",
                table: "ProjectVersions",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectVersions_ProjectId_VersionNumber",
                table: "ProjectVersions",
                columns: new[] { "ProjectId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectVersions_VersionType",
                table: "ProjectVersions",
                column: "VersionType");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectVersions_CreatedAt",
                table: "ProjectVersions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectVersions_ProjectId_CreatedAt",
                table: "ProjectVersions",
                columns: new[] { "ProjectId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectVersions_IsDeleted",
                table: "ProjectVersions",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectVersions_IsDeleted_DeletedAt",
                table: "ProjectVersions",
                columns: new[] { "IsDeleted", "DeletedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectVersions_IsMarkedImportant",
                table: "ProjectVersions",
                column: "IsMarkedImportant");

            // ContentBlobs indexes
            migrationBuilder.CreateIndex(
                name: "IX_ContentBlobs_ContentType",
                table: "ContentBlobs",
                column: "ContentType");

            migrationBuilder.CreateIndex(
                name: "IX_ContentBlobs_LastReferencedAt",
                table: "ContentBlobs",
                column: "LastReferencedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ContentBlobs_ReferenceCount",
                table: "ContentBlobs",
                column: "ReferenceCount");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "system_configuration");
            migrationBuilder.DropTable(name: "user_setup");
            migrationBuilder.DropTable(name: "export_history");
            migrationBuilder.DropTable(name: "templates");
            migrationBuilder.DropTable(name: "custom_templates");
            migrationBuilder.DropTable(name: "Configurations");
            migrationBuilder.DropTable(name: "ActionLogs");
            migrationBuilder.DropTable(name: "SceneStates");
            migrationBuilder.DropTable(name: "AssetStates");
            migrationBuilder.DropTable(name: "RenderCheckpoints");
            migrationBuilder.DropTable(name: "ProjectVersions");
            migrationBuilder.DropTable(name: "ContentBlobs");
            migrationBuilder.DropTable(name: "ProjectStates");
        }
    }
}
