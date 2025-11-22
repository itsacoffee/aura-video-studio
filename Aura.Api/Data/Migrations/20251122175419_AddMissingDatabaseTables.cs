using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Aura.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingDatabaseTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_CustomTemplates",
                table: "CustomTemplates");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "CustomTemplates");

            migrationBuilder.RenameTable(
                name: "CustomTemplates",
                newName: "custom_templates");

            migrationBuilder.RenameColumn(
                name: "DeletedByUserId",
                table: "ProjectStates",
                newName: "ModifiedBy");

            migrationBuilder.RenameColumn(
                name: "Tags",
                table: "custom_templates",
                newName: "tags");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "custom_templates",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "custom_templates",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "Category",
                table: "custom_templates",
                newName: "category");

            migrationBuilder.RenameColumn(
                name: "Author",
                table: "custom_templates",
                newName: "author");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "custom_templates",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "VisualPreferencesJson",
                table: "custom_templates",
                newName: "visual_preferences_json");

            migrationBuilder.RenameColumn(
                name: "VideoStructureJson",
                table: "custom_templates",
                newName: "video_structure_json");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "custom_templates",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "ScriptStructureJson",
                table: "custom_templates",
                newName: "script_structure_json");

            migrationBuilder.RenameColumn(
                name: "LLMPipelineJson",
                table: "custom_templates",
                newName: "llm_pipeline_json");

            migrationBuilder.RenameColumn(
                name: "IsDeleted",
                table: "custom_templates",
                newName: "is_deleted");

            migrationBuilder.RenameColumn(
                name: "IsDefault",
                table: "custom_templates",
                newName: "is_default");

            migrationBuilder.RenameColumn(
                name: "DeletedAt",
                table: "custom_templates",
                newName: "deleted_at");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "custom_templates",
                newName: "created_at");

            migrationBuilder.RenameIndex(
                name: "IX_CustomTemplates_IsDeleted_DeletedAt",
                table: "custom_templates",
                newName: "IX_custom_templates_is_deleted_deleted_at");

            migrationBuilder.RenameIndex(
                name: "IX_CustomTemplates_IsDeleted",
                table: "custom_templates",
                newName: "IX_custom_templates_is_deleted");

            migrationBuilder.RenameIndex(
                name: "IX_CustomTemplates_IsDefault",
                table: "custom_templates",
                newName: "IX_custom_templates_is_default");

            migrationBuilder.RenameIndex(
                name: "IX_CustomTemplates_CreatedAt",
                table: "custom_templates",
                newName: "IX_custom_templates_created_at");

            migrationBuilder.RenameIndex(
                name: "IX_CustomTemplates_Category_CreatedAt",
                table: "custom_templates",
                newName: "IX_custom_templates_category_created_at");

            migrationBuilder.RenameIndex(
                name: "IX_CustomTemplates_Category",
                table: "custom_templates",
                newName: "IX_custom_templates_category");

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "ProjectVersions",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "ProjectVersions",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "ProjectVersions",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "ProjectVersions",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "ProjectStates",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "ProjectStates",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "ProjectStates",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DurationSeconds",
                table: "ProjectStates",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastAutoSaveAt",
                table: "ProjectStates",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OutputFilePath",
                table: "ProjectStates",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "ProjectStates",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TemplateId",
                table: "ProjectStates",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ThumbnailPath",
                table: "ProjectStates",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "ContentBlobs",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "ContentBlobs",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "ContentBlobs",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "created_by",
                table: "custom_templates",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "deleted_by",
                table: "custom_templates",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "modified_by",
                table: "custom_templates",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_custom_templates",
                table: "custom_templates",
                column: "id");

            migrationBuilder.CreateTable(
                name: "AnalyticsRetentionSettings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    UsageStatisticsRetentionDays = table.Column<int>(type: "INTEGER", nullable: false),
                    CostTrackingRetentionDays = table.Column<int>(type: "INTEGER", nullable: false),
                    PerformanceMetricsRetentionDays = table.Column<int>(type: "INTEGER", nullable: false),
                    AutoCleanupEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CleanupHourUtc = table.Column<int>(type: "INTEGER", nullable: false),
                    TrackSuccessOnly = table.Column<bool>(type: "INTEGER", nullable: false),
                    CollectHardwareMetrics = table.Column<bool>(type: "INTEGER", nullable: false),
                    AggregateOldData = table.Column<bool>(type: "INTEGER", nullable: false),
                    AggregationThresholdDays = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxDatabaseSizeMB = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalyticsRetentionSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AnalyticsSummaries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PeriodType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    PeriodId = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TotalGenerations = table.Column<int>(type: "INTEGER", nullable: false),
                    SuccessfulGenerations = table.Column<int>(type: "INTEGER", nullable: false),
                    FailedGenerations = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalTokens = table.Column<long>(type: "INTEGER", nullable: false),
                    TotalInputTokens = table.Column<long>(type: "INTEGER", nullable: false),
                    TotalOutputTokens = table.Column<long>(type: "INTEGER", nullable: false),
                    TotalCostUSD = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    AverageDurationMs = table.Column<long>(type: "INTEGER", nullable: false),
                    TotalRenderingTimeMs = table.Column<long>(type: "INTEGER", nullable: false),
                    MostUsedProvider = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    MostUsedModel = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    MostUsedFeature = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    TotalVideoDurationSeconds = table.Column<double>(type: "REAL", nullable: false),
                    TotalScenes = table.Column<int>(type: "INTEGER", nullable: false),
                    AverageCpuUsage = table.Column<double>(type: "REAL", nullable: true),
                    AverageMemoryUsageMB = table.Column<double>(type: "REAL", nullable: true),
                    ProviderBreakdown = table.Column<string>(type: "text", nullable: true),
                    FeatureBreakdown = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalyticsSummaries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    user_id = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    username = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    action = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    resource_type = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    resource_id = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ip_address = table.Column<string>(type: "TEXT", maxLength: 45, nullable: true),
                    user_agent = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    success = table.Column<bool>(type: "INTEGER", nullable: false),
                    error_message = table.Column<string>(type: "TEXT", nullable: true),
                    changes = table.Column<string>(type: "TEXT", nullable: true),
                    metadata = table.Column<string>(type: "TEXT", nullable: true),
                    severity = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.id);
                });

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

            migrationBuilder.CreateTable(
                name: "JobQueue",
                columns: table => new
                {
                    JobId = table.Column<string>(type: "TEXT", nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    JobDataJson = table.Column<string>(type: "TEXT", nullable: false),
                    CorrelationId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    RetryCount = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxRetries = table.Column<int>(type: "INTEGER", nullable: false),
                    LastError = table.Column<string>(type: "TEXT", nullable: true),
                    EnqueuedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    NextRetryAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ProgressPercent = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentStage = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    OutputPath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    WorkerId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    IsQuickDemo = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobQueue", x => x.JobId);
                });

            migrationBuilder.CreateTable(
                name: "MediaCollections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ThumbnailUrl = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaCollections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PerformanceMetrics",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProjectId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    JobId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    OperationType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Stage = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    DurationMs = table.Column<long>(type: "INTEGER", nullable: false),
                    CpuUsagePercent = table.Column<double>(type: "REAL", nullable: true),
                    MemoryUsedMB = table.Column<double>(type: "REAL", nullable: true),
                    PeakMemoryMB = table.Column<double>(type: "REAL", nullable: true),
                    GpuUsagePercent = table.Column<double>(type: "REAL", nullable: true),
                    DiskIOOperations = table.Column<long>(type: "INTEGER", nullable: true),
                    NetworkBytesTransferred = table.Column<long>(type: "INTEGER", nullable: true),
                    OutputFileSizeBytes = table.Column<long>(type: "INTEGER", nullable: true),
                    Success = table.Column<bool>(type: "INTEGER", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    WorkerCount = table.Column<int>(type: "INTEGER", nullable: true),
                    QueueWaitMs = table.Column<long>(type: "INTEGER", nullable: true),
                    Throughput = table.Column<double>(type: "REAL", nullable: true),
                    ThroughputUnit = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SystemInfo = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerformanceMetrics", x => x.Id);
                });

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

            migrationBuilder.CreateTable(
                name: "QueueConfiguration",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    MaxConcurrentJobs = table.Column<int>(type: "INTEGER", nullable: false),
                    PauseOnBattery = table.Column<bool>(type: "INTEGER", nullable: false),
                    CpuThrottleThreshold = table.Column<int>(type: "INTEGER", nullable: false),
                    MemoryThrottleThreshold = table.Column<int>(type: "INTEGER", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    PollingIntervalSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    JobHistoryRetentionDays = table.Column<int>(type: "INTEGER", nullable: false),
                    FailedJobRetentionDays = table.Column<int>(type: "INTEGER", nullable: false),
                    RetryBaseDelaySeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    RetryMaxDelaySeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    EnableNotifications = table.Column<bool>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueueConfiguration", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    normalized_name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    is_system_role = table.Column<bool>(type: "INTEGER", nullable: false),
                    permissions = table.Column<string>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    created_by = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    modified_by = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    SettingsJson = table.Column<string>(type: "TEXT", nullable: false),
                    IsEncrypted = table.Column<bool>(type: "INTEGER", nullable: false),
                    Version = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UploadSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    TotalSize = table.Column<long>(type: "INTEGER", nullable: false),
                    UploadedSize = table.Column<long>(type: "INTEGER", nullable: false),
                    TotalChunks = table.Column<int>(type: "INTEGER", nullable: false),
                    CompletedChunksJson = table.Column<string>(type: "TEXT", nullable: false),
                    BlobUrl = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UploadSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UsageStatistics",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProjectId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    JobId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    GenerationType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Provider = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Model = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    InputTokens = table.Column<long>(type: "INTEGER", nullable: false),
                    OutputTokens = table.Column<long>(type: "INTEGER", nullable: false),
                    DurationMs = table.Column<long>(type: "INTEGER", nullable: false),
                    Success = table.Column<bool>(type: "INTEGER", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    FeatureUsed = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    OutputDurationSeconds = table.Column<double>(type: "REAL", nullable: true),
                    SceneCount = table.Column<int>(type: "INTEGER", nullable: true),
                    IsRetry = table.Column<bool>(type: "INTEGER", nullable: false),
                    RetryAttempt = table.Column<int>(type: "INTEGER", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsageStatistics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    username = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    display_name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    password_hash = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "INTEGER", nullable: false),
                    is_suspended = table.Column<bool>(type: "INTEGER", nullable: false),
                    suspended_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    suspended_reason = table.Column<string>(type: "TEXT", nullable: true),
                    last_login_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    last_login_ip = table.Column<string>(type: "TEXT", maxLength: 45, nullable: true),
                    failed_login_attempts = table.Column<int>(type: "INTEGER", nullable: false),
                    lockout_end = table.Column<DateTime>(type: "TEXT", nullable: true),
                    email_verified = table.Column<bool>(type: "INTEGER", nullable: false),
                    phone_number = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    phone_verified = table.Column<bool>(type: "INTEGER", nullable: false),
                    two_factor_enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    two_factor_secret = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    created_by = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    modified_by = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    metadata = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "JobProgressHistory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    JobId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Stage = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ProgressPercent = table.Column<int>(type: "INTEGER", nullable: false),
                    Message = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    SubstageDetail = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CurrentItem = table.Column<int>(type: "INTEGER", nullable: true),
                    TotalItems = table.Column<int>(type: "INTEGER", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ElapsedMilliseconds = table.Column<long>(type: "INTEGER", nullable: true),
                    EstimatedRemainingMilliseconds = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobProgressHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobProgressHistory_JobQueue_JobId",
                        column: x => x.JobId,
                        principalTable: "JobQueue",
                        principalColumn: "JobId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MediaItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    FileSize = table.Column<long>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    BlobUrl = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    ThumbnailUrl = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    PreviewUrl = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ContentHash = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    MetadataJson = table.Column<string>(type: "TEXT", nullable: true),
                    ProcessingStatus = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ProcessingError = table.Column<string>(type: "TEXT", nullable: true),
                    CollectionId = table.Column<Guid>(type: "TEXT", nullable: true),
                    UsageCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MediaItems_MediaCollections_CollectionId",
                        column: x => x.CollectionId,
                        principalTable: "MediaCollections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CostTracking",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UsageStatisticsId = table.Column<long>(type: "INTEGER", nullable: true),
                    ProjectId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    JobId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Provider = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Model = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    InputTokens = table.Column<long>(type: "INTEGER", nullable: false),
                    OutputTokens = table.Column<long>(type: "INTEGER", nullable: false),
                    InputPricePer1M = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    OutputPricePer1M = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    InputCost = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    OutputCost = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    TotalCost = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    YearMonth = table.Column<string>(type: "TEXT", maxLength: 7, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsEstimated = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostTracking", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CostTracking_UsageStatistics_UsageStatisticsId",
                        column: x => x.UsageStatisticsId,
                        principalTable: "UsageStatistics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "user_quotas",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    user_id = table.Column<string>(type: "TEXT", nullable: false),
                    api_requests_per_day = table.Column<int>(type: "INTEGER", nullable: true),
                    api_requests_used_today = table.Column<int>(type: "INTEGER", nullable: false),
                    api_requests_reset_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    videos_per_month = table.Column<int>(type: "INTEGER", nullable: true),
                    videos_generated_this_month = table.Column<int>(type: "INTEGER", nullable: false),
                    videos_reset_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    storage_limit_bytes = table.Column<long>(type: "INTEGER", nullable: true),
                    storage_used_bytes = table.Column<long>(type: "INTEGER", nullable: false),
                    ai_tokens_per_month = table.Column<long>(type: "INTEGER", nullable: true),
                    ai_tokens_used_this_month = table.Column<long>(type: "INTEGER", nullable: false),
                    ai_tokens_reset_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    max_concurrent_renders = table.Column<int>(type: "INTEGER", nullable: true),
                    max_concurrent_jobs = table.Column<int>(type: "INTEGER", nullable: true),
                    total_cost_usd = table.Column<decimal>(type: "TEXT", nullable: false),
                    cost_limit_usd = table.Column<decimal>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    created_by = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    modified_by = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_quotas", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_quotas_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    user_id = table.Column<string>(type: "TEXT", nullable: false),
                    role_id = table.Column<string>(type: "TEXT", nullable: false),
                    assigned_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    assigned_by = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_roles", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "FK_user_roles_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_roles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MediaTags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MediaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Tag = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MediaTags_MediaItems_MediaId",
                        column: x => x.MediaId,
                        principalTable: "MediaItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MediaUsages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MediaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ProjectName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    UsedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaUsages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MediaUsages_MediaItems_MediaId",
                        column: x => x.MediaId,
                        principalTable: "MediaItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "AnalyticsRetentionSettings",
                columns: new[] { "Id", "AggregateOldData", "AggregationThresholdDays", "AutoCleanupEnabled", "CleanupHourUtc", "CollectHardwareMetrics", "CostTrackingRetentionDays", "CreatedAt", "CreatedBy", "IsEnabled", "MaxDatabaseSizeMB", "ModifiedBy", "PerformanceMetricsRetentionDays", "TrackSuccessOnly", "UpdatedAt", "UsageStatisticsRetentionDays" },
                values: new object[] { "default", true, 30, true, 2, true, 365, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, 500, null, 30, false, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 90 });

            migrationBuilder.InsertData(
                table: "QueueConfiguration",
                columns: new[] { "Id", "CpuThrottleThreshold", "EnableNotifications", "FailedJobRetentionDays", "IsEnabled", "JobHistoryRetentionDays", "MaxConcurrentJobs", "MemoryThrottleThreshold", "PauseOnBattery", "PollingIntervalSeconds", "RetryBaseDelaySeconds", "RetryMaxDelaySeconds", "UpdatedAt" },
                values: new object[] { "default", 90, true, 90, true, 30, 2, 90, false, 5, 60, 3600, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "id", "created_at", "created_by", "description", "is_system_role", "modified_by", "name", "normalized_name", "permissions", "updated_at" },
                values: new object[,]
                {
                    { "role-admin", new DateTime(2025, 11, 22, 17, 54, 19, 112, DateTimeKind.Utc).AddTicks(3839), null, "Full system access", true, null, "Administrator", "ADMINISTRATOR", "[\"admin.full_access\",\"users.manage\",\"config.write\",\"audit.view\"]", new DateTime(2025, 11, 22, 17, 54, 19, 112, DateTimeKind.Utc).AddTicks(3840) },
                    { "role-user", new DateTime(2025, 11, 22, 17, 54, 19, 112, DateTimeKind.Utc).AddTicks(3853), null, "Standard user access", true, null, "User", "USER", "[\"projects.manage\",\"videos.create\",\"assets.manage\"]", new DateTime(2025, 11, 22, 17, 54, 19, 112, DateTimeKind.Utc).AddTicks(3853) },
                    { "role-viewer", new DateTime(2025, 11, 22, 17, 54, 19, 112, DateTimeKind.Utc).AddTicks(3865), null, "Read-only access", true, null, "Viewer", "VIEWER", "[\"projects.view\",\"videos.view\"]", new DateTime(2025, 11, 22, 17, 54, 19, 112, DateTimeKind.Utc).AddTicks(3865) }
                });

            migrationBuilder.UpdateData(
                table: "system_configuration",
                keyColumn: "id",
                keyValue: 1,
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectStates_Category",
                table: "ProjectStates",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectStates_Category_CreatedAt",
                table: "ProjectStates",
                columns: new[] { "Category", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectStates_CreatedAt",
                table: "ProjectStates",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectStates_Status_Category",
                table: "ProjectStates",
                columns: new[] { "Status", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectStates_TemplateId",
                table: "ProjectStates",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsRetentionSettings_IsEnabled",
                table: "AnalyticsRetentionSettings",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsRetentionSettings_UpdatedAt",
                table: "AnalyticsRetentionSettings",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsSummaries_PeriodId",
                table: "AnalyticsSummaries",
                column: "PeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsSummaries_PeriodStart",
                table: "AnalyticsSummaries",
                column: "PeriodStart");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsSummaries_PeriodType",
                table: "AnalyticsSummaries",
                column: "PeriodType");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsSummaries_PeriodType_PeriodId",
                table: "AnalyticsSummaries",
                columns: new[] { "PeriodType", "PeriodId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsSummaries_PeriodType_PeriodStart",
                table: "AnalyticsSummaries",
                columns: new[] { "PeriodType", "PeriodStart" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_action",
                table: "audit_logs",
                column: "action");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_action_timestamp",
                table: "audit_logs",
                columns: new[] { "action", "timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_resource_type",
                table: "audit_logs",
                column: "resource_type");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_timestamp",
                table: "audit_logs",
                column: "timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_user_id",
                table: "audit_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_user_id_timestamp",
                table: "audit_logs",
                columns: new[] { "user_id", "timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_CostTracking_JobId",
                table: "CostTracking",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_CostTracking_ProjectId",
                table: "CostTracking",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_CostTracking_Provider",
                table: "CostTracking",
                column: "Provider");

            migrationBuilder.CreateIndex(
                name: "IX_CostTracking_Provider_YearMonth",
                table: "CostTracking",
                columns: new[] { "Provider", "YearMonth" });

            migrationBuilder.CreateIndex(
                name: "IX_CostTracking_Timestamp",
                table: "CostTracking",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_CostTracking_UsageStatisticsId",
                table: "CostTracking",
                column: "UsageStatisticsId");

            migrationBuilder.CreateIndex(
                name: "IX_CostTracking_YearMonth",
                table: "CostTracking",
                column: "YearMonth");

            migrationBuilder.CreateIndex(
                name: "IX_CostTracking_YearMonth_Timestamp",
                table: "CostTracking",
                columns: new[] { "YearMonth", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_HardwareSettings_UpdatedAt",
                table: "HardwareSettings",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_HardwareSettings_Version",
                table: "HardwareSettings",
                column: "Version");

            migrationBuilder.CreateIndex(
                name: "IX_JobProgressHistory_JobId",
                table: "JobProgressHistory",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_JobProgressHistory_JobId_Timestamp",
                table: "JobProgressHistory",
                columns: new[] { "JobId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_JobProgressHistory_Timestamp",
                table: "JobProgressHistory",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_JobQueue_CreatedAt",
                table: "JobQueue",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_JobQueue_EnqueuedAt",
                table: "JobQueue",
                column: "EnqueuedAt");

            migrationBuilder.CreateIndex(
                name: "IX_JobQueue_NextRetryAt",
                table: "JobQueue",
                column: "NextRetryAt");

            migrationBuilder.CreateIndex(
                name: "IX_JobQueue_Priority",
                table: "JobQueue",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_JobQueue_Status",
                table: "JobQueue",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_JobQueue_Status_Priority",
                table: "JobQueue",
                columns: new[] { "Status", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_JobQueue_WorkerId",
                table: "JobQueue",
                column: "WorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaCollections_CreatedAt",
                table: "MediaCollections",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MediaCollections_IsDeleted",
                table: "MediaCollections",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_MediaCollections_IsDeleted_DeletedAt",
                table: "MediaCollections",
                columns: new[] { "IsDeleted", "DeletedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MediaCollections_Name",
                table: "MediaCollections",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_MediaItems_CollectionId",
                table: "MediaItems",
                column: "CollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaItems_ContentHash",
                table: "MediaItems",
                column: "ContentHash");

            migrationBuilder.CreateIndex(
                name: "IX_MediaItems_CreatedAt",
                table: "MediaItems",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MediaItems_IsDeleted",
                table: "MediaItems",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_MediaItems_IsDeleted_DeletedAt",
                table: "MediaItems",
                columns: new[] { "IsDeleted", "DeletedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MediaItems_ProcessingStatus",
                table: "MediaItems",
                column: "ProcessingStatus");

            migrationBuilder.CreateIndex(
                name: "IX_MediaItems_Source",
                table: "MediaItems",
                column: "Source");

            migrationBuilder.CreateIndex(
                name: "IX_MediaItems_Type",
                table: "MediaItems",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_MediaItems_Type_CreatedAt",
                table: "MediaItems",
                columns: new[] { "Type", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MediaTags_MediaId",
                table: "MediaTags",
                column: "MediaId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaTags_MediaId_Tag",
                table: "MediaTags",
                columns: new[] { "MediaId", "Tag" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MediaTags_Tag",
                table: "MediaTags",
                column: "Tag");

            migrationBuilder.CreateIndex(
                name: "IX_MediaUsages_MediaId",
                table: "MediaUsages",
                column: "MediaId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaUsages_MediaId_UsedAt",
                table: "MediaUsages",
                columns: new[] { "MediaId", "UsedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MediaUsages_ProjectId",
                table: "MediaUsages",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaUsages_UsedAt",
                table: "MediaUsages",
                column: "UsedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceMetrics_JobId",
                table: "PerformanceMetrics",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceMetrics_OperationType",
                table: "PerformanceMetrics",
                column: "OperationType");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceMetrics_OperationType_Timestamp",
                table: "PerformanceMetrics",
                columns: new[] { "OperationType", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceMetrics_ProjectId",
                table: "PerformanceMetrics",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceMetrics_Success",
                table: "PerformanceMetrics",
                column: "Success");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceMetrics_Success_Timestamp",
                table: "PerformanceMetrics",
                columns: new[] { "Success", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceMetrics_Timestamp",
                table: "PerformanceMetrics",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderConfigurations_UpdatedAt",
                table: "ProviderConfigurations",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderConfigurations_Version",
                table: "ProviderConfigurations",
                column: "Version");

            migrationBuilder.CreateIndex(
                name: "IX_QueueConfiguration_UpdatedAt",
                table: "QueueConfiguration",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_roles_is_system_role",
                table: "roles",
                column: "is_system_role");

            migrationBuilder.CreateIndex(
                name: "IX_roles_name",
                table: "roles",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_roles_normalized_name",
                table: "roles",
                column: "normalized_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Settings_UpdatedAt",
                table: "Settings",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Settings_Version",
                table: "Settings",
                column: "Version");

            migrationBuilder.CreateIndex(
                name: "IX_UploadSessions_CreatedAt",
                table: "UploadSessions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UploadSessions_ExpiresAt",
                table: "UploadSessions",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_UsageStatistics_GenerationType",
                table: "UsageStatistics",
                column: "GenerationType");

            migrationBuilder.CreateIndex(
                name: "IX_UsageStatistics_GenerationType_Timestamp",
                table: "UsageStatistics",
                columns: new[] { "GenerationType", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_UsageStatistics_JobId",
                table: "UsageStatistics",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_UsageStatistics_ProjectId",
                table: "UsageStatistics",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_UsageStatistics_Provider",
                table: "UsageStatistics",
                column: "Provider");

            migrationBuilder.CreateIndex(
                name: "IX_UsageStatistics_Provider_Timestamp",
                table: "UsageStatistics",
                columns: new[] { "Provider", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_UsageStatistics_Success",
                table: "UsageStatistics",
                column: "Success");

            migrationBuilder.CreateIndex(
                name: "IX_UsageStatistics_Success_Timestamp",
                table: "UsageStatistics",
                columns: new[] { "Success", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_UsageStatistics_Timestamp",
                table: "UsageStatistics",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_user_quotas_user_id",
                table: "user_quotas",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_role_id",
                table: "user_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_created_at",
                table: "users",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_is_active",
                table: "users",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "IX_users_is_suspended",
                table: "users",
                column: "is_suspended");

            migrationBuilder.CreateIndex(
                name: "IX_users_username",
                table: "users",
                column: "username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnalyticsRetentionSettings");

            migrationBuilder.DropTable(
                name: "AnalyticsSummaries");

            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "CostTracking");

            migrationBuilder.DropTable(
                name: "HardwareSettings");

            migrationBuilder.DropTable(
                name: "JobProgressHistory");

            migrationBuilder.DropTable(
                name: "MediaTags");

            migrationBuilder.DropTable(
                name: "MediaUsages");

            migrationBuilder.DropTable(
                name: "PerformanceMetrics");

            migrationBuilder.DropTable(
                name: "ProviderConfigurations");

            migrationBuilder.DropTable(
                name: "QueueConfiguration");

            migrationBuilder.DropTable(
                name: "Settings");

            migrationBuilder.DropTable(
                name: "UploadSessions");

            migrationBuilder.DropTable(
                name: "user_quotas");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "UsageStatistics");

            migrationBuilder.DropTable(
                name: "JobQueue");

            migrationBuilder.DropTable(
                name: "MediaItems");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "MediaCollections");

            migrationBuilder.DropIndex(
                name: "IX_ProjectStates_Category",
                table: "ProjectStates");

            migrationBuilder.DropIndex(
                name: "IX_ProjectStates_Category_CreatedAt",
                table: "ProjectStates");

            migrationBuilder.DropIndex(
                name: "IX_ProjectStates_CreatedAt",
                table: "ProjectStates");

            migrationBuilder.DropIndex(
                name: "IX_ProjectStates_Status_Category",
                table: "ProjectStates");

            migrationBuilder.DropIndex(
                name: "IX_ProjectStates_TemplateId",
                table: "ProjectStates");

            migrationBuilder.DropPrimaryKey(
                name: "PK_custom_templates",
                table: "custom_templates");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "ProjectVersions");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "ProjectVersions");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "ProjectVersions");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ProjectVersions");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "ProjectStates");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "ProjectStates");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "ProjectStates");

            migrationBuilder.DropColumn(
                name: "DurationSeconds",
                table: "ProjectStates");

            migrationBuilder.DropColumn(
                name: "LastAutoSaveAt",
                table: "ProjectStates");

            migrationBuilder.DropColumn(
                name: "OutputFilePath",
                table: "ProjectStates");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "ProjectStates");

            migrationBuilder.DropColumn(
                name: "TemplateId",
                table: "ProjectStates");

            migrationBuilder.DropColumn(
                name: "ThumbnailPath",
                table: "ProjectStates");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "ContentBlobs");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "ContentBlobs");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ContentBlobs");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "custom_templates");

            migrationBuilder.DropColumn(
                name: "deleted_by",
                table: "custom_templates");

            migrationBuilder.DropColumn(
                name: "modified_by",
                table: "custom_templates");

            migrationBuilder.RenameTable(
                name: "custom_templates",
                newName: "CustomTemplates");

            migrationBuilder.RenameColumn(
                name: "ModifiedBy",
                table: "ProjectStates",
                newName: "DeletedByUserId");

            migrationBuilder.RenameColumn(
                name: "tags",
                table: "CustomTemplates",
                newName: "Tags");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "CustomTemplates",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "description",
                table: "CustomTemplates",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "category",
                table: "CustomTemplates",
                newName: "Category");

            migrationBuilder.RenameColumn(
                name: "author",
                table: "CustomTemplates",
                newName: "Author");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "CustomTemplates",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "visual_preferences_json",
                table: "CustomTemplates",
                newName: "VisualPreferencesJson");

            migrationBuilder.RenameColumn(
                name: "video_structure_json",
                table: "CustomTemplates",
                newName: "VideoStructureJson");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "CustomTemplates",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "script_structure_json",
                table: "CustomTemplates",
                newName: "ScriptStructureJson");

            migrationBuilder.RenameColumn(
                name: "llm_pipeline_json",
                table: "CustomTemplates",
                newName: "LLMPipelineJson");

            migrationBuilder.RenameColumn(
                name: "is_deleted",
                table: "CustomTemplates",
                newName: "IsDeleted");

            migrationBuilder.RenameColumn(
                name: "is_default",
                table: "CustomTemplates",
                newName: "IsDefault");

            migrationBuilder.RenameColumn(
                name: "deleted_at",
                table: "CustomTemplates",
                newName: "DeletedAt");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "CustomTemplates",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_custom_templates_is_deleted_deleted_at",
                table: "CustomTemplates",
                newName: "IX_CustomTemplates_IsDeleted_DeletedAt");

            migrationBuilder.RenameIndex(
                name: "IX_custom_templates_is_deleted",
                table: "CustomTemplates",
                newName: "IX_CustomTemplates_IsDeleted");

            migrationBuilder.RenameIndex(
                name: "IX_custom_templates_is_default",
                table: "CustomTemplates",
                newName: "IX_CustomTemplates_IsDefault");

            migrationBuilder.RenameIndex(
                name: "IX_custom_templates_created_at",
                table: "CustomTemplates",
                newName: "IX_CustomTemplates_CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_custom_templates_category_created_at",
                table: "CustomTemplates",
                newName: "IX_CustomTemplates_Category_CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_custom_templates_category",
                table: "CustomTemplates",
                newName: "IX_CustomTemplates_Category");

            migrationBuilder.AddColumn<string>(
                name: "DeletedByUserId",
                table: "CustomTemplates",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_CustomTemplates",
                table: "CustomTemplates",
                column: "Id");

            migrationBuilder.UpdateData(
                table: "system_configuration",
                keyColumn: "id",
                keyValue: 1,
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2025, 11, 9, 17, 4, 31, 236, DateTimeKind.Utc).AddTicks(5323), new DateTime(2025, 11, 9, 17, 4, 31, 236, DateTimeKind.Utc).AddTicks(5324) });
        }
    }
}
