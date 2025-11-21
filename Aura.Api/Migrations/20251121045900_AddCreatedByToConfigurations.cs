using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aura.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatedByToConfigurations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Configurations",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Configurations");
        }
    }
}
