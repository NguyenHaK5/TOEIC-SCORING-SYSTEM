using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Toeic.Domain.Entities;

namespace Toeic.Infrastructure.Persistence.Configurations;

public class ExamConfiguration : IEntityTypeConfiguration<Exam>
{
    public void Configure(EntityTypeBuilder<Exam> builder)
    {
        builder.ToTable("Exams");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.ExamCode).HasMaxLength(50).IsRequired();
        builder.Property(e => e.ExamName).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();

        builder.HasIndex(e => e.ExamCode).IsUnique();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.CreatedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class TestConfiguration : IEntityTypeConfiguration<Test>
{
    public void Configure(EntityTypeBuilder<Test> builder)
    {
        builder.ToTable("Tests");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.TestCode).HasMaxLength(50).IsRequired();
        builder.Property(t => t.TestName).HasMaxLength(200).IsRequired();
        builder.Property(t => t.Version).HasMaxLength(20).IsRequired();
        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(t => t.CreatedAt).IsRequired();

        builder.HasIndex(t => t.TestCode).IsUnique();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(t => t.CreatedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

/// <summary>ISSUE-01 — bảng N:N gán Test cho Exam, composite PK.</summary>
public class ExamTestConfiguration : IEntityTypeConfiguration<ExamTest>
{
    public void Configure(EntityTypeBuilder<ExamTest> builder)
    {
        builder.ToTable("ExamTests");
        builder.HasKey(et => new { et.ExamId, et.TestId });

        builder.Property(et => et.IsActive).HasDefaultValue(true);
        builder.Property(et => et.CreatedAt).IsRequired();

        builder.HasOne(et => et.Exam)
            .WithMany(e => e.ExamTests)
            .HasForeignKey(et => et.ExamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(et => et.Test)
            .WithMany(t => t.ExamTests)
            .HasForeignKey(et => et.TestId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
