using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace QuizMaster.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Quizzes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OwnerId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quizzes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Questions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuizId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Questions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Questions_Quizzes_QuizId",
                        column: x => x.QuizId,
                        principalTable: "Quizzes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AnswerOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnswerOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnswerOptions_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Quizzes",
                columns: new[] { "Id", "CreatedAt", "Description", "OwnerId", "Title" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "A quick check of C# types and control flow.", "00000000-0000-0000-0000-00000000feed", "C# fundamentals" },
                    { new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Requests, responses, and useful status codes.", "00000000-0000-0000-0000-00000000feed", "HTTP basics" },
                    { new Guid("33333333-3333-3333-3333-333333333333"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Resources, HTTP methods, and idempotency.", "00000000-0000-0000-0000-00000000feed", "REST principles" }
                });

            migrationBuilder.InsertData(
                table: "Questions",
                columns: new[] { "Id", "Position", "QuizId", "Text" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000001"), 0, new Guid("11111111-1111-1111-1111-111111111111"), "Which C# keyword declares a Boolean value?" },
                    { new Guid("10000000-0000-0000-0000-000000000002"), 0, new Guid("22222222-2222-2222-2222-222222222222"), "Which status code means a resource was created?" },
                    { new Guid("10000000-0000-0000-0000-000000000003"), 0, new Guid("33333333-3333-3333-3333-333333333333"), "Which HTTP method replaces an existing resource?" }
                });

            migrationBuilder.InsertData(
                table: "AnswerOptions",
                columns: new[] { "Id", "IsCorrect", "Position", "QuestionId", "Text" },
                values: new object[,]
                {
                    { new Guid("20000000-0000-0000-0000-000000000010"), true, 0, new Guid("10000000-0000-0000-0000-000000000001"), "bool" },
                    { new Guid("20000000-0000-0000-0000-000000000011"), false, 1, new Guid("10000000-0000-0000-0000-000000000001"), "string" },
                    { new Guid("20000000-0000-0000-0000-000000000012"), false, 2, new Guid("10000000-0000-0000-0000-000000000001"), "decimal" },
                    { new Guid("20000000-0000-0000-0000-000000000013"), false, 3, new Guid("10000000-0000-0000-0000-000000000001"), "char" },
                    { new Guid("20000000-0000-0000-0000-000000000020"), true, 0, new Guid("10000000-0000-0000-0000-000000000002"), "201" },
                    { new Guid("20000000-0000-0000-0000-000000000021"), false, 1, new Guid("10000000-0000-0000-0000-000000000002"), "200" },
                    { new Guid("20000000-0000-0000-0000-000000000022"), false, 2, new Guid("10000000-0000-0000-0000-000000000002"), "404" },
                    { new Guid("20000000-0000-0000-0000-000000000023"), false, 3, new Guid("10000000-0000-0000-0000-000000000002"), "500" },
                    { new Guid("20000000-0000-0000-0000-000000000030"), true, 0, new Guid("10000000-0000-0000-0000-000000000003"), "PUT" },
                    { new Guid("20000000-0000-0000-0000-000000000031"), false, 1, new Guid("10000000-0000-0000-0000-000000000003"), "GET" },
                    { new Guid("20000000-0000-0000-0000-000000000032"), false, 2, new Guid("10000000-0000-0000-0000-000000000003"), "POST" },
                    { new Guid("20000000-0000-0000-0000-000000000033"), false, 3, new Guid("10000000-0000-0000-0000-000000000003"), "DELETE" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnswerOptions_QuestionId_Position",
                table: "AnswerOptions",
                columns: new[] { "QuestionId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_Questions_QuizId_Position",
                table: "Questions",
                columns: new[] { "QuizId", "Position" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnswerOptions");

            migrationBuilder.DropTable(
                name: "Questions");

            migrationBuilder.DropTable(
                name: "Quizzes");
        }
    }
}
