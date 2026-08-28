using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Toeic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Candidates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CandidateCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
                    Gender = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Candidates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Candidates_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Exams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExamCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ExamName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ExamDate = table.Column<DateOnly>(type: "date", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Exams_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByIp = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReplacedByTokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RubricSets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    QuestionType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RubricSets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RubricSets_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Tests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TestCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TestName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ListeningDurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    ReadingDurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    WritingDurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tests_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Rubrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RubricSetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Criterion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    MinScore = table.Column<decimal>(type: "numeric(6,2)", nullable: false),
                    MaxScore = table.Column<decimal>(type: "numeric(6,2)", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ScoringRule = table.Column<string>(type: "text", nullable: true),
                    Weight = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rubrics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Rubrics_RubricSets_RubricSetId",
                        column: x => x.RubricSetId,
                        principalTable: "RubricSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Rubrics_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ExamSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExamId = table.Column<Guid>(type: "uuid", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uuid", nullable: false),
                    TestId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ListeningScore = table.Column<decimal>(type: "numeric(6,2)", nullable: true),
                    ReadingScore = table.Column<decimal>(type: "numeric(6,2)", nullable: true),
                    WritingScore = table.Column<decimal>(type: "numeric(6,2)", nullable: true),
                    TotalScore = table.Column<decimal>(type: "numeric(6,2)", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamSessions_Candidates_CandidateId",
                        column: x => x.CandidateId,
                        principalTable: "Candidates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamSessions_Exams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "Exams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamSessions_Tests_TestId",
                        column: x => x.TestId,
                        principalTable: "Tests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExamTests",
                columns: table => new
                {
                    ExamId = table.Column<Guid>(type: "uuid", nullable: false),
                    TestId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamTests", x => new { x.ExamId, x.TestId });
                    table.ForeignKey(
                        name: "FK_ExamTests_Exams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "Exams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamTests_Tests_TestId",
                        column: x => x.TestId,
                        principalTable: "Tests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Questions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TestId = table.Column<Guid>(type: "uuid", nullable: false),
                    Section = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    QuestionType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    QuestionNumber = table.Column<int>(type: "integer", nullable: false),
                    QuestionText = table.Column<string>(type: "text", nullable: false),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    AudioUrl = table.Column<string>(type: "text", nullable: true),
                    RequiredWords = table.Column<string>(type: "text", nullable: true),
                    Points = table.Column<decimal>(type: "numeric(6,2)", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Questions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Questions_Tests_TestId",
                        column: x => x.TestId,
                        principalTable: "Tests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Questions_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "QuestionOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    OptionLabel = table.Column<char>(type: "character(1)", maxLength: 1, nullable: false),
                    OptionText = table.Column<string>(type: "text", nullable: false),
                    IsCorrect = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestionOptions_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CandidateAnswers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExamSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SelectedOptionId = table.Column<Guid>(type: "uuid", nullable: true),
                    AnswerText = table.Column<string>(type: "text", nullable: true),
                    AnsweredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsCorrect = table.Column<bool>(type: "boolean", nullable: true),
                    Score = table.Column<decimal>(type: "numeric(6,2)", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CandidateAnswers_ExamSessions_ExamSessionId",
                        column: x => x.ExamSessionId,
                        principalTable: "ExamSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CandidateAnswers_QuestionOptions_SelectedOptionId",
                        column: x => x.SelectedOptionId,
                        principalTable: "QuestionOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CandidateAnswers_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HumanAssessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CandidateAnswerId = table.Column<Guid>(type: "uuid", nullable: false),
                    RaterId = table.Column<Guid>(type: "uuid", nullable: false),
                    OverallScore = table.Column<decimal>(type: "numeric(6,2)", nullable: true),
                    Feedback = table.Column<string>(type: "text", nullable: true),
                    IsFinal = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HumanAssessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HumanAssessments_CandidateAnswers_CandidateAnswerId",
                        column: x => x.CandidateAnswerId,
                        principalTable: "CandidateAnswers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HumanAssessments_Users_RaterId",
                        column: x => x.RaterId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WritingAssessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CandidateAnswerId = table.Column<Guid>(type: "uuid", nullable: false),
                    RubricSetId = table.Column<Guid>(type: "uuid", nullable: false),
                    OverallScore = table.Column<decimal>(type: "numeric(6,2)", nullable: true),
                    ErrorsJson = table.Column<string>(type: "jsonb", nullable: true),
                    AnalysisJson = table.Column<string>(type: "jsonb", nullable: true),
                    Feedback = table.Column<string>(type: "text", nullable: true),
                    ModelName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PromptVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsCurrent = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WritingAssessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WritingAssessments_CandidateAnswers_CandidateAnswerId",
                        column: x => x.CandidateAnswerId,
                        principalTable: "CandidateAnswers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WritingAssessments_RubricSets_RubricSetId",
                        column: x => x.RubricSetId,
                        principalTable: "RubricSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentCriterionScores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssessmentType = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    WritingAssessmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    HumanAssessmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    RubricId = table.Column<Guid>(type: "uuid", nullable: false),
                    CriterionCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Score = table.Column<decimal>(type: "numeric(6,2)", nullable: false),
                    Reasoning = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentCriterionScores", x => x.Id);
                    table.CheckConstraint("CK_AssessmentCriterionScores_ExactlyOneParent", "\"WritingAssessmentId\" IS NOT NULL OR \"HumanAssessmentId\" IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_AssessmentCriterionScores_HumanAssessments_HumanAssessmentId",
                        column: x => x.HumanAssessmentId,
                        principalTable: "HumanAssessments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssessmentCriterionScores_Rubrics_RubricId",
                        column: x => x.RubricId,
                        principalTable: "Rubrics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssessmentCriterionScores_WritingAssessments_WritingAssessm~",
                        column: x => x.WritingAssessmentId,
                        principalTable: "WritingAssessments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentCriterionScores_HumanAssessmentId_RubricId",
                table: "AssessmentCriterionScores",
                columns: new[] { "HumanAssessmentId", "RubricId" });

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentCriterionScores_RubricId",
                table: "AssessmentCriterionScores",
                column: "RubricId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentCriterionScores_WritingAssessmentId_RubricId",
                table: "AssessmentCriterionScores",
                columns: new[] { "WritingAssessmentId", "RubricId" });

            migrationBuilder.CreateIndex(
                name: "IX_CandidateAnswers_ExamSessionId_QuestionId",
                table: "CandidateAnswers",
                columns: new[] { "ExamSessionId", "QuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CandidateAnswers_QuestionId",
                table: "CandidateAnswers",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateAnswers_SelectedOptionId",
                table: "CandidateAnswers",
                column: "SelectedOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_Candidates_CandidateCode",
                table: "Candidates",
                column: "CandidateCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Candidates_UserId",
                table: "Candidates",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Exams_CreatedByUserId",
                table: "Exams",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Exams_ExamCode",
                table: "Exams",
                column: "ExamCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExamSessions_Active_ExamId_CandidateId",
                table: "ExamSessions",
                columns: new[] { "ExamId", "CandidateId" },
                unique: true,
                filter: "\"Status\" IN ('NOT_STARTED', 'IN_PROGRESS')");

            migrationBuilder.CreateIndex(
                name: "IX_ExamSessions_CandidateId",
                table: "ExamSessions",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamSessions_TestId",
                table: "ExamSessions",
                column: "TestId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamTests_TestId",
                table: "ExamTests",
                column: "TestId");

            migrationBuilder.CreateIndex(
                name: "IX_HumanAssessments_Final_CandidateAnswerId",
                table: "HumanAssessments",
                column: "CandidateAnswerId",
                unique: true,
                filter: "\"IsFinal\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_HumanAssessments_RaterId",
                table: "HumanAssessments",
                column: "RaterId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionOptions_QuestionId_OptionLabel",
                table: "QuestionOptions",
                columns: new[] { "QuestionId", "OptionLabel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Questions_CreatedByUserId",
                table: "Questions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_TestId_Section_QuestionNumber",
                table: "Questions",
                columns: new[] { "TestId", "Section", "QuestionNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TokenHash",
                table: "RefreshTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId_RevokedAt_ExpiresAt",
                table: "RefreshTokens",
                columns: new[] { "UserId", "RevokedAt", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Rubrics_CreatedByUserId",
                table: "Rubrics",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Rubrics_RubricSetId_Criterion_Level",
                table: "Rubrics",
                columns: new[] { "RubricSetId", "Criterion", "Level" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RubricSets_CreatedByUserId",
                table: "RubricSets",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RubricSets_QuestionType_Version",
                table: "RubricSets",
                columns: new[] { "QuestionType", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tests_CreatedByUserId",
                table: "Tests",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Tests_TestCode",
                table: "Tests",
                column: "TestCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WritingAssessments_Current_CandidateAnswerId",
                table: "WritingAssessments",
                column: "CandidateAnswerId",
                unique: true,
                filter: "\"IsCurrent\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_WritingAssessments_RubricSetId",
                table: "WritingAssessments",
                column: "RubricSetId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssessmentCriterionScores");

            migrationBuilder.DropTable(
                name: "ExamTests");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "HumanAssessments");

            migrationBuilder.DropTable(
                name: "Rubrics");

            migrationBuilder.DropTable(
                name: "WritingAssessments");

            migrationBuilder.DropTable(
                name: "CandidateAnswers");

            migrationBuilder.DropTable(
                name: "RubricSets");

            migrationBuilder.DropTable(
                name: "ExamSessions");

            migrationBuilder.DropTable(
                name: "QuestionOptions");

            migrationBuilder.DropTable(
                name: "Candidates");

            migrationBuilder.DropTable(
                name: "Exams");

            migrationBuilder.DropTable(
                name: "Questions");

            migrationBuilder.DropTable(
                name: "Tests");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
