using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommandCenter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExtendedSessionMetrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "EnergyLevel",
                table: "SessionMetrics",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "InstructorTalkRatio",
                table: "SessionMetrics",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LearnerTalkRatio",
                table: "SessionMetrics",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "MetricsConfidenceLevel",
                table: "SessionMetrics",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ParticipationScore",
                table: "SessionMetrics",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnergyLevel",
                table: "SessionMetrics");

            migrationBuilder.DropColumn(
                name: "InstructorTalkRatio",
                table: "SessionMetrics");

            migrationBuilder.DropColumn(
                name: "LearnerTalkRatio",
                table: "SessionMetrics");

            migrationBuilder.DropColumn(
                name: "MetricsConfidenceLevel",
                table: "SessionMetrics");

            migrationBuilder.DropColumn(
                name: "ParticipationScore",
                table: "SessionMetrics");
        }
    }
}
