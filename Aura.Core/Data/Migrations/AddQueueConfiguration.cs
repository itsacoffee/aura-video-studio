using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace Aura.Core.Data.Migrations
{
    /// <summary>
    /// Migration to add QueueConfiguration table for background job queue settings
    /// </summary>
    public partial class AddQueueConfiguration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QueueConfiguration",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    MaxConcurrentJobs = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 2),
                    PauseOnBattery = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    CpuThrottleThreshold = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 90),
                    MemoryThrottleThreshold = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 90),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    PollingIntervalSeconds = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 5),
                    JobHistoryRetentionDays = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 30),
                    FailedJobRetentionDays = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 90),
                    RetryBaseDelaySeconds = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 60),
                    RetryMaxDelaySeconds = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 3600),
                    EnableNotifications = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueueConfiguration", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QueueConfiguration_UpdatedAt",
                table: "QueueConfiguration",
                column: "UpdatedAt");

            // Insert default configuration
            migrationBuilder.InsertData(
                table: "QueueConfiguration",
                columns: new[] { 
                    "Id", "MaxConcurrentJobs", "PauseOnBattery", "CpuThrottleThreshold", 
                    "MemoryThrottleThreshold", "IsEnabled", "PollingIntervalSeconds", 
                    "JobHistoryRetentionDays", "FailedJobRetentionDays", "RetryBaseDelaySeconds", 
                    "RetryMaxDelaySeconds", "EnableNotifications", "UpdatedAt" 
                },
                values: new object[] { 
                    "default", 2, false, 90, 90, true, 5, 30, 90, 60, 3600, true, 
                    new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "QueueConfiguration");
        }
    }
}
