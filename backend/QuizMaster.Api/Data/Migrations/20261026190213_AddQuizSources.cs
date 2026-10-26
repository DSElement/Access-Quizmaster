using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuizMaster.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddQuizSources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "source_document_id",
                table: "quizzes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "source_chunk_id",
                table: "questions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_quizzes_source_document_id",
                table: "quizzes",
                column: "source_document_id");

            migrationBuilder.CreateIndex(
                name: "ix_questions_source_chunk_id",
                table: "questions",
                column: "source_chunk_id");

            migrationBuilder.AddForeignKey(
                name: "fk_questions_document_chunks_source_chunk_id",
                table: "questions",
                column: "source_chunk_id",
                principalTable: "document_chunks",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_quizzes_documents_source_document_id",
                table: "quizzes",
                column: "source_document_id",
                principalTable: "documents",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_questions_document_chunks_source_chunk_id",
                table: "questions");

            migrationBuilder.DropForeignKey(
                name: "fk_quizzes_documents_source_document_id",
                table: "quizzes");

            migrationBuilder.DropIndex(
                name: "ix_quizzes_source_document_id",
                table: "quizzes");

            migrationBuilder.DropIndex(
                name: "ix_questions_source_chunk_id",
                table: "questions");

            migrationBuilder.DropColumn(
                name: "source_document_id",
                table: "quizzes");

            migrationBuilder.DropColumn(
                name: "source_chunk_id",
                table: "questions");
        }
    }
}
