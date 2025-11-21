using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace Aura.Core.Data.Migrations
{
    /// <summary>
    /// Migration to add AnalyticsRetentionSettings table for analytics data retention configuration
    /// </summary>
    public partial class AddAnalyticsRetentionSettings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnalyticsRetentionSettings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    UsageStatisticsRetentionDays = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 90),
                    CostTrackingRetentionDays = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 365),
                    PerformanceMetricsRetentionDays = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 30),
                    AutoCleanupEnabled = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    CleanupHourUtc = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 2),
                    TrackSuccessOnly = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    CollectHardwareMetrics = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    AggregateOldData = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    AggregationThresholdDays = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 30),
                    MaxDatabaseSizeMB = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 500),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalyticsRetentionSettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsRetentionSettings_IsEnabled",
                table: "AnalyticsRetentionSettings",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsRetentionSettings_UpdatedAt",
                table: "AnalyticsRetentionSettings",
                column: "UpdatedAt");

            // Insert default settings
            migrationBuilder.InsertData(
                table: "AnalyticsRetentionSettings",
                columns: new[] { 
                    "Id", "IsEnabled", "UsageStatisticsRetentionDays", "CostTrackingRetentionDays", 
                    "PerformanceMetricsRetentionDays", "AutoCleanupEnabled", "CleanupHourUtc", 
                    "TrackSuccessOnly", "CollectHardwareMetrics", "AggregateOldData", 
                    "AggregationThresholdDays", "MaxDatabaseSizeMB", "CreatedAt", "UpdatedAt", 
                    "CreatedBy", "ModifiedBy" 
                },
                values: new object[] { 
                    "default", true, 90, 365, 30, true, 2, false, true, true, 30, 500, 
                    DateTime.UtcNow, DateTime.UtcNow, null, null 
                }
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AnalyticsRetentionSettings");
        }
    }
}
