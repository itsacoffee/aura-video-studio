using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aura.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddLocalAnalytics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create UsageStatistics table
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

            // Create indexes for UsageStatistics
            migrationBuilder.CreateIndex(
                name: "IX_UsageStatistics_Provider",
                table: "UsageStatistics",
                column: "Provider");

            migrationBuilder.CreateIndex(
                name: "IX_UsageStatistics_GenerationType",
                table: "UsageStatistics",
                column: "GenerationType");

            migrationBuilder.CreateIndex(
                name: "IX_UsageStatistics_Timestamp",
                table: "UsageStatistics",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_UsageStatistics_Success",
                table: "UsageStatistics",
                column: "Success");

            migrationBuilder.CreateIndex(
                name: "IX_UsageStatistics_Provider_Timestamp",
                table: "UsageStatistics",
                columns: new[] { "Provider", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_UsageStatistics_GenerationType_Timestamp",
                table: "UsageStatistics",
                columns: new[] { "GenerationType", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_UsageStatistics_Success_Timestamp",
                table: "UsageStatistics",
                columns: new[] { "Success", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_UsageStatistics_ProjectId",
                table: "UsageStatistics",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_UsageStatistics_JobId",
                table: "UsageStatistics",
                column: "JobId");

            // Create CostTracking table
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
                    InputPricePer1M = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    OutputPricePer1M = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    InputCost = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    OutputCost = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    TotalCost = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
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

            // Create indexes for CostTracking
            migrationBuilder.CreateIndex(
                name: "IX_CostTracking_Provider",
                table: "CostTracking",
                column: "Provider");

            migrationBuilder.CreateIndex(
                name: "IX_CostTracking_YearMonth",
                table: "CostTracking",
                column: "YearMonth");

            migrationBuilder.CreateIndex(
                name: "IX_CostTracking_Timestamp",
                table: "CostTracking",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_CostTracking_Provider_YearMonth",
                table: "CostTracking",
                columns: new[] { "Provider", "YearMonth" });

            migrationBuilder.CreateIndex(
                name: "IX_CostTracking_YearMonth_Timestamp",
                table: "CostTracking",
                columns: new[] { "YearMonth", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_CostTracking_ProjectId",
                table: "CostTracking",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_CostTracking_JobId",
                table: "CostTracking",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_CostTracking_UsageStatisticsId",
                table: "CostTracking",
                column: "UsageStatisticsId");

            // Create PerformanceMetrics table
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
                    SystemInfo = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerformanceMetrics", x => x.Id);
                });

            // Create indexes for PerformanceMetrics
            migrationBuilder.CreateIndex(
                name: "IX_PerformanceMetrics_OperationType",
                table: "PerformanceMetrics",
                column: "OperationType");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceMetrics_Timestamp",
                table: "PerformanceMetrics",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceMetrics_Success",
                table: "PerformanceMetrics",
                column: "Success");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceMetrics_OperationType_Timestamp",
                table: "PerformanceMetrics",
                columns: new[] { "OperationType", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceMetrics_Success_Timestamp",
                table: "PerformanceMetrics",
                columns: new[] { "Success", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceMetrics_ProjectId",
                table: "PerformanceMetrics",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceMetrics_JobId",
                table: "PerformanceMetrics",
                column: "JobId");

            // Create AnalyticsRetentionSettings table
            migrationBuilder.CreateTable(
                name: "AnalyticsRetentionSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
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

            // Seed default settings
            migrationBuilder.InsertData(
                table: "AnalyticsRetentionSettings",
                columns: new[] { "Id", "IsEnabled", "UsageStatisticsRetentionDays", "CostTrackingRetentionDays", 
                    "PerformanceMetricsRetentionDays", "AutoCleanupEnabled", "CleanupHourUtc", "TrackSuccessOnly", 
                    "CollectHardwareMetrics", "AggregateOldData", "AggregationThresholdDays", "MaxDatabaseSizeMB", 
                    "CreatedAt", "UpdatedAt", "CreatedBy", "ModifiedBy" },
                values: new object[] { 1, true, 90, 365, 30, true, 3, false, true, true, 30, 500, 
                    DateTime.UtcNow, DateTime.UtcNow, null, null });

            // Create AnalyticsSummaries table
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
                    TotalCostUSD = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    AverageDurationMs = table.Column<long>(type: "INTEGER", nullable: false),
                    TotalRenderingTimeMs = table.Column<long>(type: "INTEGER", nullable: false),
                    MostUsedProvider = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    MostUsedModel = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    MostUsedFeature = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    TotalVideoDurationSeconds = table.Column<double>(type: "REAL", nullable: false),
                    TotalScenes = table.Column<int>(type: "INTEGER", nullable: false),
                    AverageCpuUsage = table.Column<double>(type: "REAL", nullable: true),
                    AverageMemoryUsageMB = table.Column<double>(type: "REAL", nullable: true),
                    ProviderBreakdown = table.Column<string>(type: "TEXT", nullable: true),
                    FeatureBreakdown = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalyticsSummaries", x => x.Id);
                });

            // Create indexes for AnalyticsSummaries
            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsSummaries_PeriodType",
                table: "AnalyticsSummaries",
                column: "PeriodType");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsSummaries_PeriodId",
                table: "AnalyticsSummaries",
                column: "PeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsSummaries_PeriodType_PeriodId",
                table: "AnalyticsSummaries",
                columns: new[] { "PeriodType", "PeriodId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsSummaries_PeriodStart",
                table: "AnalyticsSummaries",
                column: "PeriodStart");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsSummaries_PeriodType_PeriodStart",
                table: "AnalyticsSummaries",
                columns: new[] { "PeriodType", "PeriodStart" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "CostTracking");
            migrationBuilder.DropTable(name: "UsageStatistics");
            migrationBuilder.DropTable(name: "PerformanceMetrics");
            migrationBuilder.DropTable(name: "AnalyticsRetentionSettings");
            migrationBuilder.DropTable(name: "AnalyticsSummaries");
        }
    }
}
