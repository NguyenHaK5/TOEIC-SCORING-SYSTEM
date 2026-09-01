using Microsoft.EntityFrameworkCore;
using Toeic.Domain.Entities;

namespace Toeic.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Candidate> Candidates => Set<Candidate>();
    public DbSet<Exam> Exams => Set<Exam>();
    public DbSet<Test> Tests => Set<Test>();
    public DbSet<ExamTest> ExamTests => Set<ExamTest>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<QuestionOption> QuestionOptions => Set<QuestionOption>();
    public DbSet<ExamSession> ExamSessions => Set<ExamSession>();
    public DbSet<CandidateAnswer> CandidateAnswers => Set<CandidateAnswer>();
    public DbSet<RubricSet> RubricSets => Set<RubricSet>();
    public DbSet<Rubric> Rubrics => Set<Rubric>();
    public DbSet<Toeic.Domain.Entities.WritingAssessment> WritingAssessments => Set<Toeic.Domain.Entities.WritingAssessment>();
    public DbSet<HumanAssessment> HumanAssessments => Set<HumanAssessment>();
    public DbSet<AssessmentCriterionScore> AssessmentCriterionScores => Set<AssessmentCriterionScore>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
