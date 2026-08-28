using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Toeic.Domain.Entities;

namespace Toeic.Infrastructure.Persistence.Configurations;

public class CandidateAnswerConfiguration : IEntityTypeConfiguration<CandidateAnswer>
{
    public void Configure(EntityTypeBuilder<CandidateAnswer> builder)
    {
        builder.ToTable("CandidateAnswers");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(a => a.Score).HasColumnType("decimal(6,2)");

        builder.Property(a => a.RowVersion)
            .HasColumnName("xmin")
            .IsRowVersion();

        // ISSUE-05: auto-save phải UPSERT dựa trên cặp này, không insert nhiều dòng.
        builder.HasIndex(a => new { a.ExamSessionId, a.QuestionId }).IsUnique();

        builder.HasOne(a => a.ExamSession)
            .WithMany(s => s.CandidateAnswers)
            .HasForeignKey(a => a.ExamSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Question)
            .WithMany(q => q.CandidateAnswers)
            .HasForeignKey(a => a.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.SelectedOption)
            .WithMany()
            .HasForeignKey(a => a.SelectedOptionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
