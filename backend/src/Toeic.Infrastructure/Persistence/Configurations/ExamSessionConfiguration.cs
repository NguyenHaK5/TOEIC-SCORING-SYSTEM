using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Toeic.Domain.Entities;
using Toeic.Domain.Enums;

namespace Toeic.Infrastructure.Persistence.Configurations;

public class ExamSessionConfiguration : IEntityTypeConfiguration<ExamSession>
{
    public void Configure(EntityTypeBuilder<ExamSession> builder)
    {
        builder.ToTable("ExamSessions");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(s => s.StartedAt).IsRequired();
        builder.Property(s => s.ExpiresAt).IsRequired(); // ISSUE-07: chốt 1 lần tại StartedAt
        builder.Property(s => s.ListeningScore).HasColumnType("decimal(6,2)");
        builder.Property(s => s.ReadingScore).HasColumnType("decimal(6,2)");
        builder.Property(s => s.WritingScore).HasColumnType("decimal(6,2)");
        builder.Property(s => s.TotalScore).HasColumnType("decimal(6,2)");
        builder.Property(s => s.CreatedAt).IsRequired();

        // Ghi chú bổ sung Phase 0: optimistic concurrency dùng cột hệ thống xmin của PostgreSQL.
        builder.Property(s => s.RowVersion)
            .HasColumnName("xmin")
            .IsRowVersion();

        builder.HasOne(s => s.Exam)
            .WithMany(e => e.ExamSessions)
            .HasForeignKey(s => s.ExamId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Candidate)
            .WithMany(c => c.ExamSessions)
            .HasForeignKey(s => s.CandidateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Test)
            .WithMany(t => t.ExamSessions)
            .HasForeignKey(s => s.TestId)
            .OnDelete(DeleteBehavior.Restrict);

        // ISSUE-06: chặn nhiều phiên NOT_STARTED/IN_PROGRESS song song cho cùng 1 (Exam, Candidate).
        builder.HasIndex(s => new { s.ExamId, s.CandidateId })
            .HasDatabaseName("IX_ExamSessions_Active_ExamId_CandidateId")
            .IsUnique()
            .HasFilter($"\"Status\" IN ('{ExamSessionStatus.NOT_STARTED}', '{ExamSessionStatus.IN_PROGRESS}')");
    }
}
