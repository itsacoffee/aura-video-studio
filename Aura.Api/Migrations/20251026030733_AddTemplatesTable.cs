using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aura.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTemplatesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.CreateIndex(
                name: "IX_templates_category",
                table: "templates",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "IX_templates_is_system_template",
                table: "templates",
                column: "is_system_template");

            migrationBuilder.CreateIndex(
                name: "IX_templates_is_community_template",
                table: "templates",
                column: "is_community_template");

            migrationBuilder.CreateIndex(
                name: "IX_templates_category_sub_category",
                table: "templates",
                columns: new[] { "category", "sub_category" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "templates");
        }
    }
}
