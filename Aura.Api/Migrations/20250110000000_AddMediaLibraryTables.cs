using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aura.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMediaLibraryTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create MediaCollections table
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

            // Create MediaItems table
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

            // Create MediaTags table
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

            // Create MediaUsages table
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

            // Create UploadSessions table
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

            // Create indexes
            migrationBuilder.CreateIndex(
                name: "IX_MediaItems_CollectionId",
                table: "MediaItems",
                column: "CollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaItems_Type",
                table: "MediaItems",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_MediaItems_Source",
                table: "MediaItems",
                column: "Source");

            migrationBuilder.CreateIndex(
                name: "IX_MediaItems_ContentHash",
                table: "MediaItems",
                column: "ContentHash");

            migrationBuilder.CreateIndex(
                name: "IX_MediaItems_CreatedAt",
                table: "MediaItems",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MediaTags_MediaId",
                table: "MediaTags",
                column: "MediaId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaTags_Tag",
                table: "MediaTags",
                column: "Tag");

            migrationBuilder.CreateIndex(
                name: "IX_MediaUsages_MediaId",
                table: "MediaUsages",
                column: "MediaId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaUsages_ProjectId",
                table: "MediaUsages",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_UploadSessions_ExpiresAt",
                table: "UploadSessions",
                column: "ExpiresAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "MediaUsages");
            migrationBuilder.DropTable(name: "MediaTags");
            migrationBuilder.DropTable(name: "UploadSessions");
            migrationBuilder.DropTable(name: "MediaItems");
            migrationBuilder.DropTable(name: "MediaCollections");
        }
    }
}
