using Microsoft.EntityFrameworkCore;
using Toeic.Application.Common.Dtos;
using Toeic.Application.Common.Exceptions;
using Toeic.Application.HumanRating;
using Toeic.Application.HumanRating.Dtos;
using Toeic.Domain.Entities;
using Toeic.Domain.Enums;
using Toeic.Domain.Rubrics;
using Toeic.Infrastructure.Persistence;

namespace Toeic.Infrastructure.HumanRating;

/// <summary>
/// ⚠️ FILE RỦI RO CAO NHẤT CỦA PHASE 4 PART 1 — xem PHASE4_PART1_INTEGRATION_GUIDE.md
/// mục "Giả định cần đối chiếu" trước khi build. Toàn bộ property name của
/// AssessmentCriterionScore/HumanAssessment dùng đúng shape đã CHỐT ở Phase 0 (E.2.13/E.2.14),
/// và lệnh gọi WritingScoringEngine.CalculateOverallScore là suy luận theo mô tả ở
/// phase-tracker.md (Phase 3 Part 2) — cần đối chiếu chữ ký thật.
/// </summary>
public class HumanRatingService : IHumanRatingService
{
    private readonly AppDbContext _db;

    public HumanRatingService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<WritingAnswerQueueItemDto>> GetQueueAsync(
        HumanRatingQueueFilter filter, CancellationToken ct = default)
    {
        var query = _db.CandidateAnswers
            .Include(a => a.Question)
            .Include(a => a.ExamSession)
                .ThenInclude(s => s.Candidate)
            .Where(a => a.Question.Section == Section.WRITING)
            .Where(a => a.ExamSession.Status == ExamSessionStatus.SUBMITTED)
            .Where(a => a.AnswerText != null && a.AnswerText != string.Empty)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.QuestionType) &&
            Enum.TryParse<QuestionType>(filter.QuestionType, true, out var qType))
        {
            query = query.Where(a => a.Question.QuestionType == qType);
        }

        // Tải id các CandidateAnswer đã có HumanAssessment IsFinal=true để lọc Pending/Rated.
        var finalizedAnswerIds = await _db.HumanAssessments
            .Where(h => h.IsFinal)
            .Select(h => h.CandidateAnswerId)
            .Distinct()
            .ToListAsync(ct);
        var finalizedSet = finalizedAnswerIds.ToHashSet();

        var candidateAnswers = await query
            .OrderBy(a => a.ExamSession.SubmittedAt)
            .ToListAsync(ct);

        var filtered = (filter.Status switch
        {
            RaterQueueStatusFilter.Pending => candidateAnswers.Where(a => !finalizedSet.Contains(a.Id)),
            RaterQueueStatusFilter.Rated => candidateAnswers.Where(a => finalizedSet.Contains(a.Id)),
            _ => candidateAnswers
        }).ToList();

        var totalCount = filtered.Count;
        var pageItems = filtered
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToList();

        var answerIds = pageItems.Select(a => a.Id).ToList();

        var currentAiAssessments = await _db.WritingAssessments
            .Where(w => answerIds.Contains(w.CandidateAnswerId) && w.IsCurrent)
            .ToListAsync(ct);
        var aiByAnswer = currentAiAssessments.ToDictionary(w => w.CandidateAnswerId);

        var finalHumanAssessments = await _db.HumanAssessments
            .Where(h => answerIds.Contains(h.CandidateAnswerId) && h.IsFinal)
            .ToListAsync(ct);
        var humanByAnswer = finalHumanAssessments.ToDictionary(h => h.CandidateAnswerId);

        var items = pageItems.Select(a =>
        {
            aiByAnswer.TryGetValue(a.Id, out var ai);
            humanByAnswer.TryGetValue(a.Id, out var human);

            return new WritingAnswerQueueItemDto
            {
                CandidateAnswerId = a.Id,
                ExamSessionId = a.ExamSessionId,
                CandidateId = a.ExamSession.CandidateId,
                CandidateName = a.ExamSession.Candidate?.FullName ?? string.Empty,
                QuestionId = a.QuestionId,
                QuestionType = a.Question.QuestionType.ToString(),
                QuestionNumber = a.Question.QuestionNumber,
                SubmittedAt = a.ExamSession.SubmittedAt ?? a.ExamSession.StartedAt,
                AiStatus = ai == null ? "Pending" : "Scored",
                AiOverallScore = ai?.OverallScore,
                HumanStatus = human == null ? "NotRated" : "Rated",
                HumanFinalScore = human?.OverallScore
            };
        }).ToList();

        return new PagedResult<WritingAnswerQueueItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.PageNumber,
            PageSize = filter.PageSize
        };
    }

    public async Task<WritingAnswerDetailDto> GetDetailAsync(Guid candidateAnswerId, CancellationToken ct = default)
    {
        var answer = await _db.CandidateAnswers
            .Include(a => a.Question)
            .Include(a => a.ExamSession)
                .ThenInclude(s => s.Candidate)
            .FirstOrDefaultAsync(a => a.Id == candidateAnswerId, ct)
            ?? throw new NotFoundAppException("Không tìm thấy câu trả lời.");

        if (answer.Question.Section != Section.WRITING)
        {
            throw new ValidationAppException("Chỉ có thể chấm tay câu hỏi Writing.");
        }

        var dto = new WritingAnswerDetailDto
        {
            CandidateAnswerId = answer.Id,
            ExamSessionId = answer.ExamSessionId,
            CandidateId = answer.ExamSession.CandidateId,
            CandidateName = answer.ExamSession.Candidate?.FullName ?? string.Empty,
            QuestionId = answer.QuestionId,
            QuestionType = answer.Question.QuestionType.ToString(),
            QuestionText = answer.Question.QuestionText,
            RequiredWords = answer.Question.RequiredWords,
            ImageUrl = answer.Question.ImageUrl,
            AnswerText = answer.AnswerText
        };

        var currentAi = await _db.WritingAssessments
            .Include(w => w.CriterionScores)
            .Where(w => w.CandidateAnswerId == candidateAnswerId && w.IsCurrent)
            .FirstOrDefaultAsync(ct);

        if (currentAi != null)
        {
            dto.AiAssessment = new AiAssessmentSummaryDto
            {
                WritingAssessmentId = currentAi.Id,
                ModelName = currentAi.ModelName,
                PromptVersion = currentAi.PromptVersion,
                OverallScore = currentAi.OverallScore ?? 0m,
                Feedback = currentAi.Feedback,
                CreatedAt = currentAi.CreatedAt,
                CriterionScores = currentAi.CriterionScores
                    .Where(c => c.AssessmentType == AssessmentSourceType.AI)
                    .Select(c => new CriterionScoreDto
                    {
                        Criterion = c.CriterionCode,
                        Score = c.Score,
                        Reasoning = c.Reasoning
                    }).ToList()
            };
        }

        var humanAssessments = await _db.HumanAssessments
            .Include(h => h.CriterionScores)
            .Include(h => h.Rater)
            .Where(h => h.CandidateAnswerId == candidateAnswerId)
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync(ct);

        dto.HumanAssessments = humanAssessments.Select(h => new HumanAssessmentSummaryDto
        {
            HumanAssessmentId = h.Id,
            RaterId = h.RaterId,
            RaterName = h.Rater?.FullName ?? string.Empty,
            OverallScore = h.OverallScore ?? 0m,
            Feedback = h.Feedback,
            IsFinal = h.IsFinal,
            CreatedAt = h.CreatedAt,
            CriterionScores = h.CriterionScores
                .Where(c => c.AssessmentType == AssessmentSourceType.HUMAN)
                .Select(c => new CriterionScoreDto
                {
                    Criterion = c.CriterionCode,
                    Score = c.Score,
                    Reasoning = c.Reasoning
                }).ToList()
        }).ToList();

        var activeRubricSet = await GetActiveRubricSetAsync(answer.Question.QuestionType, ct);
        if (activeRubricSet != null)
        {
            dto.ActiveRubric = new ActiveRubricSetDto
            {
                RubricSetId = activeRubricSet.Id,
                Name = activeRubricSet.Name,
                Version = activeRubricSet.Version,
                Criteria = activeRubricSet.Rubrics.Select(r => new RubricCriterionDto
                {
                    RubricId = r.Id,
                    Criterion = r.Criterion,
                    Level = r.Level,
                    MinScore = r.MinScore,
                    MaxScore = r.MaxScore,
                    Description = r.Description,
                    Weight = r.Weight
                }).ToList()
            };
        }

        return dto;
    }

    public async Task<HumanScoreResultDto> SubmitScoreAsync(
        Guid candidateAnswerId, Guid raterId, SubmitHumanScoreRequestDto request, CancellationToken ct = default)
    {
        var answer = await _db.CandidateAnswers
            .Include(a => a.Question)
            .FirstOrDefaultAsync(a => a.Id == candidateAnswerId, ct)
            ?? throw new NotFoundAppException("Không tìm thấy câu trả lời.");

        if (answer.Question.Section != Section.WRITING)
        {
            throw new ValidationAppException("Chỉ có thể chấm tay câu hỏi Writing.");
        }

        if (request.CriterionScores == null || request.CriterionScores.Count == 0)
        {
            throw new ValidationAppException("Phải nhập điểm cho ít nhất 1 tiêu chí.");
        }

        var rubricSet = await GetActiveRubricSetAsync(answer.Question.QuestionType, ct)
            ?? throw new ValidationAppException(
                $"Chưa có RubricSet ACTIVE cho loại câu hỏi {answer.Question.QuestionType}. " +
                "Cần Admin activate rubric trước khi Rater chấm (xem Phase 3 Part 1).");

        // ⚠️ ĐIỂM CẦN ĐỐI CHIẾU: chữ ký thật của WritingScoringEngine.CalculateOverallScore
        // (Phase 3 Part 2). Giả định: nhận (IEnumerable<Rubric> rubrics,
        // IDictionary<string, decimal> criterionScores, decimal targetMaxScore) và trả về
        // WritingScoringResult { OverallScore, CriterionBreakdown }. Nếu chữ ký thật khác,
        // CHỈ cần sửa khối gọi dưới đây — không ảnh hưởng phần còn lại của file.
        var criterionScoreMap = request.CriterionScores
            .ToDictionary(c => c.Criterion, c => c.Score, StringComparer.OrdinalIgnoreCase);

        WritingScoringResult scoringResult;
        try
        {
            scoringResult = WritingScoringEngine.CalculateOverallScore(
                rubricSet.Rubrics.ToList(),
                criterionScoreMap,
                100m);
        }
        catch (RubricScoringException ex)
        {
            throw new ValidationAppException($"Điểm tiêu chí không hợp lệ: {ex.Message}");
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        if (request.SetAsFinal)
        {
            var previousFinal = await _db.HumanAssessments
                .Where(h => h.CandidateAnswerId == candidateAnswerId && h.IsFinal)
                .ToListAsync(ct);
            foreach (var prev in previousFinal)
            {
                prev.IsFinal = false;
                prev.UpdatedAt = DateTime.UtcNow;
            }
        }

        var humanAssessment = new HumanAssessment
        {
            Id = Guid.NewGuid(),
            CandidateAnswerId = candidateAnswerId,
            RaterId = raterId,
            OverallScore = scoringResult.OverallScore,
            Feedback = request.Feedback,
            IsFinal = request.SetAsFinal,
            CreatedAt = DateTime.UtcNow
        };
        _db.HumanAssessments.Add(humanAssessment);

        var criterionRows = new List<CriterionScoreDto>();
        foreach (var input in request.CriterionScores)
        {
            var rubric = rubricSet.Rubrics.FirstOrDefault(r =>
                string.Equals(r.Criterion, input.Criterion, StringComparison.OrdinalIgnoreCase));
            if (rubric == null)
            {
                throw new ValidationAppException(
                    $"Tiêu chí '{input.Criterion}' không thuộc RubricSet ACTIVE hiện hành.");
            }

            _db.AssessmentCriterionScores.Add(new AssessmentCriterionScore
            {
                Id = Guid.NewGuid(),
                AssessmentType = AssessmentSourceType.HUMAN,
                HumanAssessmentId = humanAssessment.Id,
                WritingAssessmentId = null,
                RubricId = rubric.Id,
                CriterionCode = input.Criterion,
                Score = input.Score,
                Reasoning = input.Reasoning,
                CreatedAt = DateTime.UtcNow
            });

            criterionRows.Add(new CriterionScoreDto
            {
                Criterion = input.Criterion,
                Score = input.Score,
                Reasoning = input.Reasoning
            });
        }

        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return new HumanScoreResultDto
        {
            HumanAssessmentId = humanAssessment.Id,
            CandidateAnswerId = candidateAnswerId,
            OverallScore = humanAssessment.OverallScore ?? 0m,
            IsFinal = humanAssessment.IsFinal,
            CriterionScores = criterionRows,
            CreatedAt = humanAssessment.CreatedAt
        };
    }

    public async Task<PagedResult<AiHumanComparisonDto>> GetAiHumanComparisonAsync(
    AiHumanComparisonFilter filter, CancellationToken ct = default)
    {
        var query = _db.HumanAssessments
            .Include(h => h.CriterionScores)
            .Include(h => h.Rater)
            .Join(_db.CandidateAnswers, h => h.CandidateAnswerId, a => a.Id,
                (h, a) => new { Human = h, Answer = a })
            .Join(_db.Questions, x => x.Answer.QuestionId, q => q.Id,
                (x, q) => new { x.Human, x.Answer, Question = q })
            .Where(x => x.Question.Section == Section.WRITING)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.QuestionType) &&
            Enum.TryParse<QuestionType>(filter.QuestionType, true, out var qType))
        {
            query = query.Where(x => x.Question.QuestionType == qType);
        }

        var rows = await query.ToListAsync(ct);

        var answerIds = rows.Select(r => r.Answer.Id).Distinct().ToList();
        var currentAiAssessments = await _db.WritingAssessments
            .Include(w => w.CriterionScores)
            .Where(w => answerIds.Contains(w.CandidateAnswerId) && w.IsCurrent)
            .ToListAsync(ct);
        var aiByAnswer = currentAiAssessments.ToDictionary(w => w.CandidateAnswerId);

        var comparisons = new List<AiHumanComparisonDto>();
        foreach (var row in rows)
        {
            aiByAnswer.TryGetValue(row.Answer.Id, out var ai);

            if (filter.OnlyWithBothScores && ai == null)
            {
                continue;
            }

            var humanCriteria = row.Human.CriterionScores
                .Where(c => c.AssessmentType == AssessmentSourceType.HUMAN)
                .ToDictionary(c => c.CriterionCode, c => c.Score, StringComparer.OrdinalIgnoreCase);
            var aiCriteria = ai?.CriterionScores
                .Where(c => c.AssessmentType == AssessmentSourceType.AI)
                .ToDictionary(c => c.CriterionCode, c => c.Score, StringComparer.OrdinalIgnoreCase)
                ?? new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

            var allCriteria = humanCriteria.Keys.Union(aiCriteria.Keys, StringComparer.OrdinalIgnoreCase);
            var criterionComparisons = allCriteria.Select(c =>
            {
                var hasHuman = humanCriteria.TryGetValue(c, out var hScore);
                var hasAi = aiCriteria.TryGetValue(c, out var aScore);
                return new CriterionComparisonDto
                {
                    Criterion = c,
                    HumanScore = hasHuman ? hScore : null,
                    AiScore = hasAi ? aScore : null,
                    Difference = (hasHuman && hasAi) ? hScore - aScore : null
                };
            }).ToList();

            comparisons.Add(new AiHumanComparisonDto
            {
                CandidateAnswerId = row.Answer.Id,
                QuestionId = row.Question.Id,
                QuestionType = row.Question.QuestionType.ToString(),
                HumanAssessmentId = row.Human.Id,
                RaterId = row.Human.RaterId,
                RaterName = row.Human.Rater?.FullName ?? string.Empty,
                IsFinalHuman = row.Human.IsFinal,
                HumanOverallScore = row.Human.OverallScore ?? 0m,
                WritingAssessmentId = ai?.Id,
                AiOverallScore = ai?.OverallScore,
                OverallDifference = ai?.OverallScore != null
                    ? (row.Human.OverallScore ?? 0m) - ai.OverallScore.Value
                    : null,
                CriterionComparisons = criterionComparisons
            });
        }

        var totalCount = comparisons.Count;
        var pageItems = comparisons
            .OrderByDescending(c => c.OverallDifference.HasValue ? Math.Abs(c.OverallDifference.Value) : -1)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToList();

        return new PagedResult<AiHumanComparisonDto>
        {
            Items = pageItems,
            TotalCount = totalCount,
            Page = filter.PageNumber,
            PageSize = filter.PageSize
        };
    }

    private async Task<RubricSet?> GetActiveRubricSetAsync(QuestionType questionType, CancellationToken ct)
    {
        return await _db.RubricSets
            .Include(rs => rs.Rubrics)
            .Where(rs => rs.QuestionType == questionType && rs.Status == RubricSetStatus.ACTIVE)
            .FirstOrDefaultAsync(ct);
    }
}
