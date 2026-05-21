using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommandCenter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LearningSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    LearnerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    MediaStoragePath = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AudioStoragePath = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    TranscriptStoragePath = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Duration = table.Column<TimeSpan>(type: "interval", nullable: true),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LearningSignals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Timestamp = table.Column<TimeSpan>(type: "interval", nullable: false),
                    SignalType = table.Column<string>(type: "text", nullable: false),
                    Level = table.Column<string>(type: "text", nullable: false),
                    ConfidenceScore = table.Column<double>(type: "double precision", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SourceEvidence = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningSignals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LearningSignals_LearningSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "LearningSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Recommendations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recommendations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Recommendations_LearningSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "LearningSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionAnalyses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Summary = table.Column<string>(type: "text", nullable: false),
                    KeyTopics = table.Column<string>(type: "text", nullable: false),
                    LearningObjectivesInferred = table.Column<string>(type: "text", nullable: false),
                    StrengthsObserved = table.Column<string>(type: "text", nullable: false),
                    AreasForImprovement = table.Column<string>(type: "text", nullable: false),
                    ModelVersion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AnalyzedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionAnalyses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionAnalyses_LearningSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "LearningSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionMetrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    OverallEngagementScore = table.Column<double>(type: "double precision", nullable: false),
                    OverallAttentionScore = table.Column<double>(type: "double precision", nullable: false),
                    OverallFrustrationScore = table.Column<double>(type: "double precision", nullable: false),
                    OverallConfusionScore = table.Column<double>(type: "double precision", nullable: false),
                    OverallComprehensionScore = table.Column<double>(type: "double precision", nullable: false),
                    TotalWordsSpoken = table.Column<int>(type: "integer", nullable: false),
                    SpeakingRateWordsPerMinute = table.Column<double>(type: "double precision", nullable: false),
                    PauseCount = table.Column<int>(type: "integer", nullable: false),
                    TotalPauseDuration = table.Column<TimeSpan>(type: "interval", nullable: false),
                    QuestionCount = table.Column<int>(type: "integer", nullable: false),
                    FillerWordCount = table.Column<int>(type: "integer", nullable: false),
                    ComputedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionMetrics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionMetrics_LearningSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "LearningSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TranscriptSegments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SequenceIndex = table.Column<int>(type: "integer", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    Confidence = table.Column<double>(type: "double precision", nullable: false),
                    SpeakerTag = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TranscriptSegments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TranscriptSegments_LearningSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "LearningSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VideoAnalysisResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AnalyzedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoAnalysisResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VideoAnalysisResults_LearningSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "LearningSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VideoLabels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VideoAnalysisResultId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Confidence = table.Column<double>(type: "double precision", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "interval", nullable: true),
                    EndTime = table.Column<TimeSpan>(type: "interval", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoLabels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VideoLabels_VideoAnalysisResults_VideoAnalysisResultId",
                        column: x => x.VideoAnalysisResultId,
                        principalTable: "VideoAnalysisResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VideoShots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VideoAnalysisResultId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "interval", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoShots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VideoShots_VideoAnalysisResults_VideoAnalysisResultId",
                        column: x => x.VideoAnalysisResultId,
                        principalTable: "VideoAnalysisResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LearningSessions_CreatedAt",
                table: "LearningSessions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LearningSessions_Status",
                table: "LearningSessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_LearningSignals_SessionId",
                table: "LearningSignals",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Recommendations_SessionId",
                table: "Recommendations",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionAnalyses_SessionId",
                table: "SessionAnalyses",
                column: "SessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessionMetrics_SessionId",
                table: "SessionMetrics",
                column: "SessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TranscriptSegments_SessionId",
                table: "TranscriptSegments",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_VideoAnalysisResults_SessionId",
                table: "VideoAnalysisResults",
                column: "SessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VideoLabels_VideoAnalysisResultId",
                table: "VideoLabels",
                column: "VideoAnalysisResultId");

            migrationBuilder.CreateIndex(
                name: "IX_VideoShots_VideoAnalysisResultId",
                table: "VideoShots",
                column: "VideoAnalysisResultId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LearningSignals");

            migrationBuilder.DropTable(
                name: "Recommendations");

            migrationBuilder.DropTable(
                name: "SessionAnalyses");

            migrationBuilder.DropTable(
                name: "SessionMetrics");

            migrationBuilder.DropTable(
                name: "TranscriptSegments");

            migrationBuilder.DropTable(
                name: "VideoLabels");

            migrationBuilder.DropTable(
                name: "VideoShots");

            migrationBuilder.DropTable(
                name: "VideoAnalysisResults");

            migrationBuilder.DropTable(
                name: "LearningSessions");
        }
    }
}
