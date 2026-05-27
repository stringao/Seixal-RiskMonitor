using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional
#pragma warning disable S1192 // String literals should not be duplicated

namespace GeoRisk.API.Migrations
{
    /// <inheritdoc />
    public partial class AddAiProviderConfigs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApiKey",
                table: "AppSettings");

            migrationBuilder.DropColumn(
                name: "LlmProvider",
                table: "AppSettings");

            migrationBuilder.DropColumn(
                name: "MaxTokens",
                table: "AppSettings");

            migrationBuilder.RenameColumn(
                name: "ModelName",
                table: "AppSettings",
                newName: "ActiveProvider");

            migrationBuilder.CreateTable(
                name: "AiProviderConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ApiKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BaseUrl = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    MaxTokens = table.Column<int>(type: "integer", nullable: false, defaultValue: 1024),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiProviderConfigs", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "AiProviderConfigs",
                columns: new[] { "Id", "ApiKey", "BaseUrl", "CreatedAt", "IsEnabled", "MaxTokens", "Model", "Provider", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "", null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, 1024, "deepseek-chat", "DeepSeek", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, "", "http://localhost:11434/v1", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, 1024, "qwen3:8b", "Ollama", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, "", null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, 1024, "qwen-turbo", "Qwen", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 4, "", null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, 1024, "claude-sonnet-4-20250514", "Anthropic", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 5, "", null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, 1024, "gpt-4o", "OpenAI", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.UpdateData(
                table: "AppSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "ActiveProvider",
                value: "DeepSeek");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiProviderConfigs");

            migrationBuilder.RenameColumn(
                name: "ActiveProvider",
                table: "AppSettings",
                newName: "ModelName");

            migrationBuilder.AddColumn<string>(
                name: "ApiKey",
                table: "AppSettings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LlmProvider",
                table: "AppSettings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "MaxTokens",
                table: "AppSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "AppSettings",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ApiKey", "LlmProvider", "MaxTokens", "ModelName" },
                values: new object[] { "", "OpenAI", 1024, "gpt-4o" });
        }
    }
}
