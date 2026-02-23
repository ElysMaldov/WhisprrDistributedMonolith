using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Whisprr.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:sentiment", "negative,neutral,positive")
                .Annotation("Npgsql:Enum:task_progress_status", "processing,success,failed");

            migrationBuilder.CreateTable(
                name: "SocialTopics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Keywords = table.Column<string[]>(type: "text[]", nullable: false),
                    Language = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SocialTopics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SourcePlatforms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SourceUrl = table.Column<string>(type: "text", nullable: false),
                    IconUrl = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SourcePlatforms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SocialTopicListeningTask",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "task_progress_status", nullable: false),
                    SocialTopicId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourcePlatformId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SocialTopicListeningTask", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SocialTopicListeningTask_SocialTopics_SocialTopicId",
                        column: x => x.SocialTopicId,
                        principalTable: "SocialTopics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SocialTopicListeningTask_SourcePlatforms_SourcePlatformId",
                        column: x => x.SourcePlatformId,
                        principalTable: "SourcePlatforms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SocialInfos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Content = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Sentiment = table.Column<int>(type: "sentiment", nullable: false),
                    OriginalUrl = table.Column<string>(type: "text", nullable: false),
                    SourcePlatformId = table.Column<Guid>(type: "uuid", nullable: false),
                    GeneratedFromTaskId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SocialInfos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SocialInfos_SocialTopicListeningTask_GeneratedFromTaskId",
                        column: x => x.GeneratedFromTaskId,
                        principalTable: "SocialTopicListeningTask",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SocialInfos_SourcePlatforms_SourcePlatformId",
                        column: x => x.SourcePlatformId,
                        principalTable: "SourcePlatforms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SocialInfos_GeneratedFromTaskId",
                table: "SocialInfos",
                column: "GeneratedFromTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_SocialInfos_SourcePlatformId",
                table: "SocialInfos",
                column: "SourcePlatformId");

            migrationBuilder.CreateIndex(
                name: "IX_SocialTopicListeningTask_SocialTopicId",
                table: "SocialTopicListeningTask",
                column: "SocialTopicId");

            migrationBuilder.CreateIndex(
                name: "IX_SocialTopicListeningTask_SourcePlatformId",
                table: "SocialTopicListeningTask",
                column: "SourcePlatformId");

            migrationBuilder.CreateIndex(
                name: "IX_SourcePlatforms_Name",
                table: "SourcePlatforms",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SocialInfos");

            migrationBuilder.DropTable(
                name: "SocialTopicListeningTask");

            migrationBuilder.DropTable(
                name: "SocialTopics");

            migrationBuilder.DropTable(
                name: "SourcePlatforms");
        }
    }
}
