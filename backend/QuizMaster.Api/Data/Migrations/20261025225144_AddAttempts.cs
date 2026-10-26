using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuizMaster.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAttempts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "attempts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "text", nullable: false),
                    quiz_id = table.Column<Guid>(type: "uuid", nullable: true),
                    quiz_title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    correct_count = table.Column<int>(type: "integer", nullable: false),
                    total_questions = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_attempts", x => x.id);
                    table.ForeignKey(
                        name: "fk_attempts_quizzes_quiz_id",
                        column: x => x.quiz_id,
                        principalTable: "quizzes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_attempts_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "attempt_answers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    attempt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    question_text = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    options = table.Column<List<string>>(type: "text[]", nullable: false),
                    selected_option_index = table.Column<int>(type: "integer", nullable: true),
                    correct_option_index = table.Column<int>(type: "integer", nullable: false),
                    was_correct = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_attempt_answers", x => x.id);
                    table.ForeignKey(
                        name: "fk_attempt_answers_attempts_attempt_id",
                        column: x => x.attempt_id,
                        principalTable: "attempts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_attempt_answers_attempt_id_position",
                table: "attempt_answers",
                columns: new[] { "attempt_id", "position" });

            migrationBuilder.CreateIndex(
                name: "ix_attempts_quiz_id",
                table: "attempts",
                column: "quiz_id");

            migrationBuilder.CreateIndex(
                name: "ix_attempts_user_id_submitted_at",
                table: "attempts",
                columns: new[] { "user_id", "submitted_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "attempt_answers");

            migrationBuilder.DropTable(
                name: "attempts");
        }
    }
}
