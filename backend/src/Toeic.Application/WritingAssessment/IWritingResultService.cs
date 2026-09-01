using Toeic.Application.WritingAssessment.Dtos;

namespace Toeic.Application.WritingAssessment;

public interface IWritingResultService
{
    /// <summary>
    /// Does the actual AI-scoring work for every Writing CandidateAnswer in
    /// the given ExamSession that doesn't already have a current
    /// WritingAssessment. Called ONLY by WritingAssessmentBackgroundWorker
    /// (never directly from a controller — this can take many seconds).
    /// Persists WritingAssessment + AssessmentCriterionScores as it goes;
    /// a failure on one question does not stop the others (logged, left
    /// for the next attempt to pick up).
    /// </summary>
    Task ProcessExamSessionAsync(Guid examSessionId, CancellationToken ct);

    /// <summary>
    /// Read-only, fast, safe to call from GET /exam-sessions/{id}/result.
    /// Returns the current state of every Writing question (Scored /
    /// Pending / NotAnswered) without ever calling the AI itself. If it
    /// finds answered-but-unscored questions, it re-enqueues a job as a
    /// self-healing safety net (see PHASE3-ISSUE-06) — but still returns
    /// immediately without waiting for that job to run.
    /// </summary>
    Task<WritingSectionResultDto> GetSectionResultAsync(Guid examSessionId, CancellationToken ct);
}
