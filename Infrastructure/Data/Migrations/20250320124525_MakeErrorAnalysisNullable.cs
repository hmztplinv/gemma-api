using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LanguageLearningApp.API.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class MakeErrorAnalysisNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ErrorAnalysis",
                table: "Messages",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "{}",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ErrorAnalysis",
                table: "Messages",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldDefaultValue: "{}");
        }
    }
}
