using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Toeic.Domain.Entities;

namespace Toeic.Infrastructure.Persistence.Configurations;

public class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        builder.ToTable("Questions");
        builder.HasKey(q => q.Id);

        builder.Property(q => q.Section).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(q => q.QuestionType).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(q => q.QuestionText).IsRequired();
        builder.Property(q => q.Points).HasColumnType("decimal(6,2)");
        builder.Property(q => q.CreatedAt).IsRequired();

        builder.HasIndex(q => new { q.TestId, q.Section, q.QuestionNumber });

        builder.HasOne(q => q.Test)
            .WithMany(t => t.Questions)
            .HasForeignKey(q => q.TestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(q => q.CreatedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Ghi chú (Phase 0): validation "Section=LISTENING => AudioUrl bắt buộc" enforce ở
        // Application/FluentValidation, KHÔNG dùng CHECK constraint (tránh vỡ khi mở rộng enum).
    }
}

public class QuestionOptionConfiguration : IEntityTypeConfiguration<QuestionOption>
{
    public void Configure(EntityTypeBuilder<QuestionOption> builder)
    {
        builder.ToTable("QuestionOptions");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.OptionLabel).HasMaxLength(1).IsRequired();
        builder.Property(o => o.OptionText).IsRequired();
        builder.Property(o => o.IsCorrect).HasDefaultValue(false);
        builder.Property(o => o.CreatedAt).IsRequired();

        builder.HasIndex(o => new { o.QuestionId, o.OptionLabel }).IsUnique();

        builder.HasOne(o => o.Question)
            .WithMany(q => q.Options)
            .HasForeignKey(o => o.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
