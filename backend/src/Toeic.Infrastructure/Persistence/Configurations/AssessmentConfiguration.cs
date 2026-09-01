using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Toeic.Domain.Entities;

namespace Toeic.Infrastructure.Persistence.Configurations;

/// <summary>
/// ISSUE-02: quan hệ 1:N với CandidateAnswer (chấm lại tạo dòng mới, KHÔNG update dòng cũ).
/// Partial unique index đảm bảo chỉ có đúng 1 bản IsCurrent = TRUE cho mỗi CandidateAnswer.
/// </summary>
public class WritingAssessmentConfiguration : IEntityTypeConfiguration<Toeic.Domain.Entities.WritingAssessment>
{
    public void Configure(EntityTypeBuilder<Toeic.Domain.Entities.WritingAssessment> builder)
    {
        builder.ToTable("WritingAssessments");
        builder.HasKey(w => w.Id);

        builder.Property(w => w.OverallScore).HasColumnType("decimal(6,2)");
        builder.Property(w => w.ErrorsJson).HasColumnType("jsonb");
        builder.Property(w => w.AnalysisJson).HasColumnType("jsonb");
        builder.Property(w => w.ModelName).HasMaxLength(100);
        builder.Property(w => w.PromptVersion).HasMaxLength(50);
        builder.Property(w => w.IsCurrent).HasDefaultValue(true);
        builder.Property(w => w.CreatedAt).IsRequired();

        builder.HasOne(w => w.CandidateAnswer)
            .WithMany(a => a.WritingAssessments)
            .HasForeignKey(w => w.CandidateAnswerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(w => w.RubricSet)
            .WithMany(rs => rs.WritingAssessments)
            .HasForeignKey(w => w.RubricSetId)
            .OnDelete(DeleteBehavior.Restrict);

        // ISSUE-02: luôn đúng 1 bản "hiện hành" cho mỗi câu trả lời.
        builder.HasIndex(w => w.CandidateAnswerId)
            .HasDatabaseName("IX_WritingAssessments_Current_CandidateAnswerId")
            .IsUnique()
            .HasFilter("\"IsCurrent\" = TRUE");
    }
}

/// <summary>ISSUE-04: partial unique index đảm bảo chỉ có đúng 1 bản IsFinal = TRUE cho mỗi CandidateAnswer.</summary>
public class HumanAssessmentConfiguration : IEntityTypeConfiguration<HumanAssessment>
{
    public void Configure(EntityTypeBuilder<HumanAssessment> builder)
    {
        builder.ToTable("HumanAssessments");
        builder.HasKey(h => h.Id);

        builder.Property(h => h.OverallScore).HasColumnType("decimal(6,2)");
        builder.Property(h => h.IsFinal).HasDefaultValue(false);
        builder.Property(h => h.CreatedAt).IsRequired();

        builder.HasOne(h => h.CandidateAnswer)
            .WithMany(a => a.HumanAssessments)
            .HasForeignKey(h => h.CandidateAnswerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(h => h.Rater)
            .WithMany(u => u.HumanAssessments)
            .HasForeignKey(h => h.RaterId)
            .OnDelete(DeleteBehavior.Restrict);

        // ISSUE-04: điểm "chính thức" — tối đa 1 bản cho mỗi câu trả lời.
        builder.HasIndex(h => h.CandidateAnswerId)
            .HasDatabaseName("IX_HumanAssessments_Final_CandidateAnswerId")
            .IsUnique()
            .HasFilter("\"IsFinal\" = TRUE");
    }
}
