using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aura.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddJobQueueSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create JobQueue table
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
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobQueue", x => x.JobId);
                });

            // Create indexes for JobQueue
            migrationBuilder.CreateIndex(
                name: "IX_JobQueue_Status",
                table: "JobQueue",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_JobQueue_Priority",
                table: "JobQueue",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_JobQueue_Status_Priority",
                table: "JobQueue",
                columns: new[] { "Status", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_JobQueue_EnqueuedAt",
                table: "JobQueue",
                column: "EnqueuedAt");

            migrationBuilder.CreateIndex(
                name: "IX_JobQueue_NextRetryAt",
                table: "JobQueue",
                column: "NextRetryAt");

            migrationBuilder.CreateIndex(
                name: "IX_JobQueue_WorkerId",
                table: "JobQueue",
                column: "WorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_JobQueue_CreatedAt",
                table: "JobQueue",
                column: "CreatedAt");

            // Create JobProgressHistory table
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

            // Create indexes for JobProgressHistory
            migrationBuilder.CreateIndex(
                name: "IX_JobProgressHistory_JobId",
                table: "JobProgressHistory",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_JobProgressHistory_Timestamp",
                table: "JobProgressHistory",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_JobProgressHistory_JobId_Timestamp",
                table: "JobProgressHistory",
                columns: new[] { "JobId", "Timestamp" });

            // Create QueueConfiguration table
            migrationBuilder.CreateTable(
                name: "QueueConfiguration",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
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

            // Insert default configuration
            migrationBuilder.InsertData(
                table: "QueueConfiguration",
                columns: new[] { "Id", "MaxConcurrentJobs", "PauseOnBattery", "CpuThrottleThreshold", 
                    "MemoryThrottleThreshold", "IsEnabled", "PollingIntervalSeconds", 
                    "JobHistoryRetentionDays", "FailedJobRetentionDays", "RetryBaseDelaySeconds", 
                    "RetryMaxDelaySeconds", "EnableNotifications", "UpdatedAt" },
                values: new object[] { 1, 2, true, 85, 85, true, 5, 7, 30, 5, 300, true, DateTime.UtcNow });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "JobProgressHistory");
            migrationBuilder.DropTable(name: "JobQueue");
            migrationBuilder.DropTable(name: "QueueConfiguration");
        }
    }
}
