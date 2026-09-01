using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Toeic.Application.Common.Exceptions;
using Toeic.Application.WritingAssessment;
using Toeic.Application.WritingAssessment.Dtos;
using Toeic.Domain.Entities;
using Toeic.Domain.Enums;
using Toeic.Infrastructure.Persistence;

namespace Toeic.Infrastructure.WritingAssessment;

/// <summary>
/// ASSUMPTION WARNING (see PHASE3_PART4_INTEGRATION_GUIDE.md, section
/// "Giả định entity"): WritingAssessment and AssessmentCriterionScore were
/// created in Phase 1 Part 1, before this chat thread — their exact
/// property names below are inferred from the project's own planning
/// documents and forward-references in Phase 3 Part 1/3, NOT copied from
/// real source. If your actual entities use different property names,
/// this is the ONLY file you need to edit to match them — the pipeline
/// logic (Part 2 engine, Part 3 AI service, background queue) does not
/// change.
/// </summary>
public class WritingResultService : IWritingResultService
{
    private static readonly HashSet<QuestionType> WritingQuestionTypes = new()
    {
        QuestionType.PICTURE_SENTENCE,
        QuestionType.WRITTEN_RESPONSE,
        QuestionType.OPINION_ESSAY
    };

    private readonly AppDbContext _db;
    private readonly IAiAssessmentService _aiAssessmentService;
    private readonly IWritingAssessmentQueue _queue;
    private readonly ILogger<WritingResultService> _logger;

    public WritingResultService(
        AppDbContext db,
        IAiAssessmentService aiAssessmentService,
        IWritingAssessmentQueue queue,
        ILogger<WritingResultService> logger)
    {
        _db = db;
        _aiAssessmentService = aiAssessmentService;
        _queue = queue;
        _logger = logger;
    }

    public async Task ProcessExamSessionAsync(Guid examSessionId, CancellationToken ct)
    {
        var answers = await LoadWritingAnswersAsync(examSessionId, ct);

        foreach (var answer in answers)
        {
            if (string.IsNullOrWhiteSpace(answer.AnswerText))
            {
                continue; // nothing to score — GetSectionResultAsync will report this as NotAnswered
            }

            var alreadyScored = await _db.WritingAssessments
                .AnyAsync(w => w.CandidateAnswerId == answer.Id && w.IsCurrent, ct);
            if (alreadyScored)
            {
                continue; // idempotent — a previous run (or a duplicate enqueue) already scored this
            }

            try
            {
                var aiResult = await _aiAssessmentService.AssessAsync(BuildRequest(answer), ct);
                await PersistAsync(answer, aiResult, ct);
            }
            catch (Exception ex) when (ex is AiProviderException or AiOutputValidationException or ConflictAppException)
            {
                // ConflictAppException here means "no ACTIVE RubricSet for this QuestionType" —
                // an Admin configuration gap, not a transient AI failure, but the handling is the
                // same either way: log, skip this question, leave it unscored for the next attempt.
                _logger.LogWarning(
                    ex,
                    "Could not AI-score CandidateAnswer {CandidateAnswerId} (Question {QuestionId}): {Message}",
                    answer.Id, answer.QuestionId, ex.Message);
            }
        }
    }

    public async Task<WritingSectionResultDto> GetSectionResultAsync(Guid examSessionId, CancellationToken ct)
    {
        var answers = await LoadWritingAnswersAsync(examSessionId, ct);
        var questionResults = new List<WritingQuestionResultDto>();
        var needsProcessing = false;

        foreach (var answer in answers)
        {
            var existing = await _db.WritingAssessments
                .Include(w => w.CriterionScores)
                .Where(w => w.CandidateAnswerId == answer.Id && w.IsCurrent)
                .FirstOrDefaultAsync(ct);

            if (existing is not null)
            {
                questionResults.Add(ToDto(answer, existing));
            }
            else if (string.IsNullOrWhiteSpace(answer.AnswerText))
            {
                questionResults.Add(WritingQuestionResultDto.NotAnswered(answer.QuestionId, answer.Question.QuestionType));
            }
            else
            {
                questionResults.Add(WritingQuestionResultDto.Pending(answer.QuestionId, answer.Question.QuestionType));
                needsProcessing = true;
            }
        }

        if (needsProcessing)
        {
            // Self-healing safety net (PHASE3-ISSUE-06): covers the in-memory queue losing a
            // job (e.g. app restarted between submit and worker processing it). Safe to
            // enqueue duplicates — ProcessExamSessionAsync always re-checks IsCurrent before
            // calling the AI, so a redundant job is a cheap no-op, not a duplicate score.
            await _queue.EnqueueAsync(new WritingAssessmentJob(examSessionId), ct);
        }

        var scored = questionResults.Where(q => q.Status == WritingQuestionStatus.Scored).ToList();

        return new WritingSectionResultDto
        {
            QuestionsTotal = questionResults.Count,
            QuestionsScored = scored.Count,
            OverallScore = scored.Count > 0
                ? Math.Round(scored.Average(q => q.OverallScore!.Value), 2)
                : null,
            Questions = questionResults
        };
    }

    private async Task<List<CandidateAnswer>> LoadWritingAnswersAsync(Guid examSessionId, CancellationToken ct)
        => await _db.CandidateAnswers
            .Include(a => a.Question)
            .Where(a => a.ExamSessionId == examSessionId && WritingQuestionTypes.Contains(a.Question.QuestionType))
            .ToListAsync(ct);

    private static WritingAssessmentRequest BuildRequest(CandidateAnswer answer)
    {
        var question = answer.Question;

        // ASSUMPTION: Question.RequiredWords is a comma-separated string (per section 12 of the
        // original prompt, "Required Words" is listed as a manageable Question field, but its
        // exact storage shape wasn't pinned down in any doc I've seen). Adjust the split logic
        // below if it's actually a JSON array column or a related table.
        List<string>? requiredWords = string.IsNullOrWhiteSpace(question.RequiredWords)
            ? null
            : question.RequiredWords
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

        return new WritingAssessmentRequest
        {
            QuestionType = question.QuestionType,
            StudentAnswer = answer.AnswerText!,
            PromptOrQuestionText = question.QuestionText,
            RequiredWords = requiredWords,
            // No image-caption/alt-text field exists anywhere in the schema seen so far —
            // left null on purpose (PHASE3-ISSUE-03, Part 3: PromptBuilder already degrades
            // gracefully to a lenient ImageRelevance judgment when this is null).
            ImageDescription = null
        };
    }

    //private async Task PersistAsync(CandidateAnswer answer, WritingAssessmentResult aiResult, CancellationToken ct)
    //{
    //    var assessment = new WritingAssessment
    //    {
    //        WritingAssessmentId = Guid.NewGuid(),
    //        CandidateAnswerId = answer.Id,
    //        RubricSetId = aiResult.RubricSetId,
    //        ModelName = aiResult.ModelName,
    //        PromptVersion = aiResult.PromptVersion,
    //        RawOutputJson = aiResult.RawOutputJson,
    //        OverallScore = aiResult.Scoring.OverallScore,
    //        Feedback = aiResult.Feedback,
    //        StrengthsJson = JsonSerializer.Serialize(aiResult.Strengths),
    //        WeaknessesJson = JsonSerializer.Serialize(aiResult.Weaknesses),
    //        CorrectionsJson = JsonSerializer.Serialize(aiResult.Corrections),
    //        IsCurrent = true,
    //        CreatedAt = DateTime.UtcNow
    //    };

    //    _db.WritingAssessments.Add(assessment);

    //    foreach (var breakdown in aiResult.Scoring.CriterionBreakdown)
    //    {
    //        _db.AssessmentCriterionScores.Add(new AssessmentCriterionScore
    //        {
    //            AssessmentCriterionScoreId = Guid.NewGuid(),
    //            WritingAssessmentId = assessment.WritingAssessmentId,
    //            Criterion = breakdown.Criterion,
    //            Score = breakdown.RawScore,
    //            Weight = breakdown.Weight,
    //            NormalizedScore = breakdown.NormalizedScore,
    //            WeightedContribution = breakdown.WeightedContribution
    //        });
    //    }
    private async Task PersistAsync(CandidateAnswer answer, WritingAssessmentResult aiResult, CancellationToken ct)
    {
        var assessment = new Toeic.Domain.Entities.WritingAssessment
        {
            Id = Guid.NewGuid(),
            CandidateAnswerId = answer.Id,
            RubricSetId = aiResult.RubricSetId,
            ModelName = aiResult.ModelName,
            PromptVersion = aiResult.PromptVersion,
            AnalysisJson = aiResult.RawOutputJson, // Map sang AnalysisJson
            OverallScore = aiResult.Scoring.OverallScore,
            Feedback = aiResult.Feedback,
            // Gộp Weaknesses và Corrections thành JSON lưu vào cột ErrorsJson
            ErrorsJson = JsonSerializer.Serialize(new
            {
                aiResult.Weaknesses,
                aiResult.Corrections
            }),
            IsCurrent = true
            // Bỏ CreatedAt nếu BaseEntity của bạn đã tự động xử lý
        };

        _db.WritingAssessments.Add(assessment);

        // Lấy danh sách Rubric để map RubricId bắt buộc cho AssessmentCriterionScore
        var rubrics = await _db.Rubrics
            .Where(r => r.RubricSetId == aiResult.RubricSetId)
            .ToListAsync(ct);

        foreach (var breakdown in aiResult.Scoring.CriterionBreakdown)
        {
            var rubric = rubrics.FirstOrDefault(r => r.Criterion == breakdown.Criterion);

            _db.AssessmentCriterionScores.Add(new AssessmentCriterionScore
            {
                Id = Guid.NewGuid(),
                AssessmentType = AssessmentSourceType.AI,
                WritingAssessmentId = assessment.Id,
                RubricId = rubric?.Id ?? Guid.Empty,
                CriterionCode = breakdown.Criterion,
                Score = breakdown.RawScore,
                CreatedAt = DateTime.UtcNow
            });
        }

        // Saved per-question (not batched for the whole ExamSession) so that if a LATER
        // question in the same ProcessExamSessionAsync loop fails, progress already made on
        // earlier questions is not lost and won't be redundantly re-scored on retry.
        await _db.SaveChangesAsync(ct);
    }

    //private static WritingQuestionResultDto ToDto(CandidateAnswer answer, WritingAssessment assessment)
    //{
    //    var criterionDtos = (assessment.CriterionScores ?? new List<AssessmentCriterionScore>())
    //        .Select(c => new CriterionScoreDto { Criterion = c.Criterion, Score = c.Score, Weight = c.Weight })
    //        .ToList();

    //    return new WritingQuestionResultDto
    //    {
    //        QuestionId = answer.QuestionId,
    //        QuestionType = answer.Question.QuestionType,
    //        Status = WritingQuestionStatus.Scored,
    //        OverallScore = assessment.OverallScore,
    //        Feedback = assessment.Feedback,
    //        Strengths = DeserializeList(assessment.StrengthsJson),
    //        Weaknesses = DeserializeList(assessment.WeaknessesJson),
    //        Corrections = DeserializeList(assessment.CorrectionsJson),
    //        CriterionScores = criterionDtos
    //    };
    //}

    //private static List<string> DeserializeList(string? json)
    //    => string.IsNullOrWhiteSpace(json)
    //        ? new List<string>()
    //        : JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
    private static WritingQuestionResultDto ToDto(CandidateAnswer answer, Toeic.Domain.Entities.WritingAssessment assessment)
    {
        var criterionDtos = (assessment.CriterionScores ?? new List<AssessmentCriterionScore>())
            .Select(c => new CriterionScoreDto
            {
                Criterion = c.CriterionCode,
                Score = c.Score,
                Weight = 0m // Weight không còn lưu trong DB, trả về 0 hoặc join bảng Rubric để lấy nếu cần
            })
            .ToList();

        var errors = ParseErrorsJson(assessment.ErrorsJson);

        return new WritingQuestionResultDto
        {
            QuestionId = answer.QuestionId,
            QuestionType = answer.Question.QuestionType,
            Status = WritingQuestionStatus.Scored,
            OverallScore = assessment.OverallScore,
            Feedback = assessment.Feedback,
            Strengths = new List<string>(), // Cột này không còn tồn tại trong DB
            Weaknesses = errors.Weaknesses,
            Corrections = errors.Corrections,
            CriterionScores = criterionDtos
        };
    }

    private static (List<string> Weaknesses, List<string> Corrections) ParseErrorsJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return (new List<string>(), new List<string>());
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var weaknesses = doc.RootElement.TryGetProperty("Weaknesses", out var we)
                ? JsonSerializer.Deserialize<List<string>>(we.GetRawText())
                : new List<string>();

            var corrections = doc.RootElement.TryGetProperty("Corrections", out var ce)
                ? JsonSerializer.Deserialize<List<string>>(ce.GetRawText())
                : new List<string>();

            return (weaknesses ?? new List<string>(), corrections ?? new List<string>());
        }
        catch
        {
            return (new List<string>(), new List<string>());
        }
    }
}
