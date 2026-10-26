using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuizMaster.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class UniqueDisplayNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "normalized_display_name",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("UPDATE users SET normalized_display_name = upper(trim(display_name));");

            migrationBuilder.CreateIndex(
                name: "ix_users_normalized_display_name",
                table: "users",
                column: "normalized_display_name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_users_normalized_display_name",
                table: "users");

            migrationBuilder.DropColumn(
                name: "normalized_display_name",
                table: "users");
        }
    }
}
