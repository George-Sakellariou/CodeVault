using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CodeVault.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.CreateTable(
                name: "CodeSnippets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Language = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TagString = table.Column<string>(type: "text", nullable: false),
                    ViewCount = table.Column<int>(type: "integer", nullable: false),
                    UsageCount = table.Column<int>(type: "integer", nullable: false),
                    Rating = table.Column<double>(type: "double precision", nullable: false),
                    RatingCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodeSnippets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CodeTags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    UsageCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodeTags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Conversations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conversations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CodeEmbeddings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CodeSnippetId = table.Column<int>(type: "integer", nullable: false),
                    Embedding = table.Column<string>(type: "vector(1536)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodeEmbeddings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CodeEmbeddings_CodeSnippets_CodeSnippetId",
                        column: x => x.CodeSnippetId,
                        principalTable: "CodeSnippets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CodePerformanceMetrics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CodeSnippetId = table.Column<int>(type: "integer", nullable: false),
                    MetricName = table.Column<string>(type: "text", nullable: false),
                    MetricValue = table.Column<string>(type: "text", nullable: false),
                    NumericValue = table.Column<double>(type: "double precision", nullable: true),
                    Unit = table.Column<string>(type: "text", nullable: false),
                    Environment = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodePerformanceMetrics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CodePerformanceMetrics_CodeSnippets_CodeSnippetId",
                        column: x => x.CodeSnippetId,
                        principalTable: "CodeSnippets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CodeSecurityScans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CodeSnippetId = table.Column<int>(type: "integer", nullable: false),
                    ScanDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Scanner = table.Column<string>(type: "text", nullable: false),
                    SecurityScore = table.Column<int>(type: "integer", nullable: true),
                    CriticalIssues = table.Column<int>(type: "integer", nullable: false),
                    HighIssues = table.Column<int>(type: "integer", nullable: false),
                    MediumIssues = table.Column<int>(type: "integer", nullable: false),
                    LowIssues = table.Column<int>(type: "integer", nullable: false),
                    FindingsJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodeSecurityScans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CodeSecurityScans_CodeSnippets_CodeSnippetId",
                        column: x => x.CodeSnippetId,
                        principalTable: "CodeSnippets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChatMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Content = table.Column<string>(type: "text", nullable: false),
                    IsFromUser = table.Column<bool>(type: "boolean", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConversationId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatMessages_Conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "Conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_ConversationId",
                table: "ChatMessages",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_CodeEmbeddings_CodeSnippetId",
                table: "CodeEmbeddings",
                column: "CodeSnippetId");

            migrationBuilder.CreateIndex(
                name: "IX_CodePerformanceMetrics_CodeSnippetId",
                table: "CodePerformanceMetrics",
                column: "CodeSnippetId");

            migrationBuilder.CreateIndex(
                name: "IX_CodeSecurityScans_CodeSnippetId",
                table: "CodeSecurityScans",
                column: "CodeSnippetId");

            migrationBuilder.CreateIndex(
                name: "IX_CodeSecurityScans_ScanDate",
                table: "CodeSecurityScans",
                column: "ScanDate");

            migrationBuilder.CreateIndex(
                name: "IX_CodeTags_Name",
                table: "CodeTags",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatMessages");

            migrationBuilder.DropTable(
                name: "CodeEmbeddings");

            migrationBuilder.DropTable(
                name: "CodePerformanceMetrics");

            migrationBuilder.DropTable(
                name: "CodeSecurityScans");

            migrationBuilder.DropTable(
                name: "CodeTags");

            migrationBuilder.DropTable(
                name: "Conversations");

            migrationBuilder.DropTable(
                name: "CodeSnippets");
        }
    }
}
