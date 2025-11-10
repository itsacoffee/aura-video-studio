using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aura.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create Users table
            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    username = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    display_name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    password_hash = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    is_suspended = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    suspended_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    suspended_reason = table.Column<string>(type: "TEXT", nullable: true),
                    last_login_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    last_login_ip = table.Column<string>(type: "TEXT", maxLength: 45, nullable: true),
                    failed_login_attempts = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    lockout_end = table.Column<DateTime>(type: "TEXT", nullable: true),
                    email_verified = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    phone_number = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    phone_verified = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    two_factor_enabled = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    two_factor_secret = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    metadata = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_users_username",
                table: "users",
                column: "username",
                unique: true);

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
                name: "IX_users_created_at",
                table: "users",
                column: "created_at");

            // Create Roles table
            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    normalized_name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    is_system_role = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    permissions = table.Column<string>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.id);
                });

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
                name: "IX_roles_is_system_role",
                table: "roles",
                column: "is_system_role");

            // Create UserRoles junction table
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
                        name: "FK_user_roles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_roles_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create UserQuotas table
            migrationBuilder.CreateTable(
                name: "user_quotas",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    user_id = table.Column<string>(type: "TEXT", nullable: false),
                    api_requests_per_day = table.Column<int>(type: "INTEGER", nullable: true),
                    api_requests_used_today = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    api_requests_reset_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    videos_per_month = table.Column<int>(type: "INTEGER", nullable: true),
                    videos_generated_this_month = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    videos_reset_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    storage_limit_bytes = table.Column<long>(type: "INTEGER", nullable: true),
                    storage_used_bytes = table.Column<long>(type: "INTEGER", nullable: false, defaultValue: 0),
                    ai_tokens_per_month = table.Column<long>(type: "INTEGER", nullable: true),
                    ai_tokens_used_this_month = table.Column<long>(type: "INTEGER", nullable: false, defaultValue: 0),
                    ai_tokens_reset_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    max_concurrent_renders = table.Column<int>(type: "INTEGER", nullable: true),
                    max_concurrent_jobs = table.Column<int>(type: "INTEGER", nullable: true),
                    total_cost_usd = table.Column<decimal>(type: "TEXT", nullable: false, defaultValue: 0),
                    cost_limit_usd = table.Column<decimal>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_user_quotas_user_id",
                table: "user_quotas",
                column: "user_id",
                unique: true);

            // Create AuditLogs table
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
                    success = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    error_message = table.Column<string>(type: "TEXT", nullable: true),
                    changes = table.Column<string>(type: "TEXT", nullable: true),
                    metadata = table.Column<string>(type: "TEXT", nullable: true),
                    severity = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true, defaultValue: "Information")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_timestamp",
                table: "audit_logs",
                column: "timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_user_id",
                table: "audit_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_action",
                table: "audit_logs",
                column: "action");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_resource_type",
                table: "audit_logs",
                column: "resource_type");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_user_id_timestamp",
                table: "audit_logs",
                columns: new[] { "user_id", "timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_action_timestamp",
                table: "audit_logs",
                columns: new[] { "action", "timestamp" });

            // Insert default roles
            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "id", "name", "normalized_name", "description", "is_system_role", "permissions", "created_at", "updated_at" },
                values: new object[,]
                {
                    { "role-admin", "Administrator", "ADMINISTRATOR", "Full system access", true, @"[""admin.full_access"",""users.manage"",""config.write"",""audit.view""]", DateTime.UtcNow, DateTime.UtcNow },
                    { "role-user", "User", "USER", "Standard user access", true, @"[""projects.manage"",""videos.create"",""assets.manage""]", DateTime.UtcNow, DateTime.UtcNow },
                    { "role-viewer", "Viewer", "VIEWER", "Read-only access", true, @"[""projects.view"",""videos.view""]", DateTime.UtcNow, DateTime.UtcNow }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "user_roles");
            migrationBuilder.DropTable(name: "user_quotas");
            migrationBuilder.DropTable(name: "audit_logs");
            migrationBuilder.DropTable(name: "users");
            migrationBuilder.DropTable(name: "roles");
        }
    }
}
