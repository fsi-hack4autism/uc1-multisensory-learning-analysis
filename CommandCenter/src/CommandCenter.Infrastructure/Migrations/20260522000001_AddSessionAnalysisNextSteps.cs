using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommandCenter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionAnalysisNextSteps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NextSteps",
                table: "SessionAnalyses",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "InsufficientData",
                table: "SessionAnalyses",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NextSteps",
                table: "SessionAnalyses");

            migrationBuilder.DropColumn(
                name: "InsufficientData",
                table: "SessionAnalyses");
        }
    }
}
