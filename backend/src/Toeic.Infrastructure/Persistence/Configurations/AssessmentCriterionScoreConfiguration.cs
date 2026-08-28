using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Toeic.Domain.Entities;

namespace Toeic.Infrastructure.Persistence.Configurations;

/// <summary>
/// ISSUE-03: bảng điểm theo tiêu chí dùng chung AI/Human. Cho phép so sánh AI vs Human theo
/// từng tiêu chí (kể cả Image Relevance của Q1–5) bằng 1 JOIN đơn giản qua RubricId.
/// </summary>
public class AssessmentCriterionScoreConfiguration : IEntityTypeConfiguration<AssessmentCriterionScore>
{
    public void Configure(EntityTypeBuilder<AssessmentCriterionScore> builder)
    {
        builder.ToTable("AssessmentCriterionScores", tb =>
            tb.HasCheckConstraint(
                "CK_AssessmentCriterionScores_ExactlyOneParent",
                "\"WritingAssessmentId\" IS NOT NULL OR \"HumanAssessmentId\" IS NOT NULL"));

        builder.HasKey(c => c.Id);

        builder.Property(c => c.AssessmentType).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(c => c.CriterionCode).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Score).HasColumnType("decimal(6,2)").IsRequired();
        builder.Property(c => c.CreatedAt).IsRequired();

        builder.HasIndex(c => new { c.WritingAssessmentId, c.RubricId });
        builder.HasIndex(c => new { c.HumanAssessmentId, c.RubricId });

        builder.HasOne(c => c.WritingAssessment)
            .WithMany(w => w.CriterionScores)
            .HasForeignKey(c => c.WritingAssessmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.HumanAssessment)
            .WithMany(h => h.CriterionScores)
            .HasForeignKey(c => c.HumanAssessmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.Rubric)
            .WithMany(r => r.CriterionScores)
            .HasForeignKey(c => c.RubricId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
