using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    AcceptanceCriteria = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 5),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeveloperStories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WorkItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    StoryType = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    Instructions = table.Column<string>(type: "TEXT", maxLength: 8000, nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 5),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    GitBranch = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    GitWorktree = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Metadata = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeveloperStories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeveloperStories_WorkItems_WorkItemId",
                        column: x => x.WorkItemId,
                        principalTable: "WorkItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeveloperStoryDependencies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DependentStoryId = table.Column<int>(type: "INTEGER", nullable: false),
                    RequiredStoryId = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeveloperStoryDependencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeveloperStoryDependencies_DeveloperStories_DependentStoryId",
                        column: x => x.DependentStoryId,
                        principalTable: "DeveloperStories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DeveloperStoryDependencies_DeveloperStories_RequiredStoryId",
                        column: x => x.RequiredStoryId,
                        principalTable: "DeveloperStories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExecutionLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DeveloperStoryId = table.Column<int>(type: "INTEGER", nullable: false),
                    EventType = table.Column<int>(type: "INTEGER", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    Details = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Metadata = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutionLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExecutionLogs_DeveloperStories_DeveloperStoryId",
                        column: x => x.DeveloperStoryId,
                        principalTable: "DeveloperStories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeveloperStories_Priority",
                table: "DeveloperStories",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_DeveloperStories_Status",
                table: "DeveloperStories",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DeveloperStories_StoryType",
                table: "DeveloperStories",
                column: "StoryType");

            migrationBuilder.CreateIndex(
                name: "IX_DeveloperStories_WorkItemId",
                table: "DeveloperStories",
                column: "WorkItemId");

            migrationBuilder.CreateIndex(
                name: "IX_DeveloperStoryDependencies_DependentStoryId",
                table: "DeveloperStoryDependencies",
                column: "DependentStoryId");

            migrationBuilder.CreateIndex(
                name: "IX_DeveloperStoryDependencies_DependentStoryId_RequiredStoryId",
                table: "DeveloperStoryDependencies",
                columns: new[] { "DependentStoryId", "RequiredStoryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeveloperStoryDependencies_RequiredStoryId",
                table: "DeveloperStoryDependencies",
                column: "RequiredStoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionLogs_DeveloperStoryId",
                table: "ExecutionLogs",
                column: "DeveloperStoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionLogs_EventType",
                table: "ExecutionLogs",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionLogs_Timestamp",
                table: "ExecutionLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_WorkItems_Priority",
                table: "WorkItems",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_WorkItems_Status",
                table: "WorkItems",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_WorkItems_Type",
                table: "WorkItems",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeveloperStoryDependencies");

            migrationBuilder.DropTable(
                name: "ExecutionLogs");

            migrationBuilder.DropTable(
                name: "DeveloperStories");

            migrationBuilder.DropTable(
                name: "WorkItems");
        }
    }
}
