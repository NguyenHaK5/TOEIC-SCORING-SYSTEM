namespace Toeic.Domain.Enums;

public enum UserRole
{
    ADMIN,
    RATER,
    CANDIDATE
}

public enum UserStatus
{
    ACTIVE,
    LOCKED,
    INACTIVE
}

public enum CandidateStatus
{
    ACTIVE,
    INACTIVE,
    DISQUALIFIED
}

public enum ExamStatus
{
    DRAFT,
    SCHEDULED,
    ONGOING,
    COMPLETED,
    CANCELLED
}

public enum TestStatus
{
    DRAFT,
    PUBLISHED,
    ARCHIVED
}

public enum Section
{
    LISTENING,
    READING,
    WRITING
}

public enum QuestionType
{
    MULTIPLE_CHOICE,
    PICTURE_SENTENCE,
    WRITTEN_RESPONSE,
    OPINION_ESSAY
}

public enum ExamSessionStatus
{
    NOT_STARTED,
    IN_PROGRESS,
    SUBMITTED,
    ABSENT,
    CANCELLED
}

public enum AnswerStatus
{
    ANSWERED,
    SKIPPED,
    NOT_ANSWERED
}

public enum RubricSetStatus
{
    DRAFT,
    ACTIVE,
    ARCHIVED
}

/// <summary>
/// Nguồn của một bản ghi điểm theo tiêu chí trong AssessmentCriterionScores.
/// AI  -> gắn với WritingAssessmentId
/// HUMAN -> gắn với HumanAssessmentId
/// </summary>
public enum AssessmentSourceType
{
    AI,
    HUMAN
}
