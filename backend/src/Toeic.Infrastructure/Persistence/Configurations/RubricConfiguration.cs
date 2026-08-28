using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Toeic.Domain.Entities;

namespace Toeic.Infrastructure.Persistence.Configurations;

public class RubricSetConfiguration : IEntityTypeConfiguration<RubricSet>
{
    public void Configure(EntityTypeBuilder<RubricSet> builder)
    {
        builder.ToTable("RubricSets");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name).HasMaxLength(200).IsRequired();
        builder.Property(r => r.QuestionType).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(r => r.Version).HasMaxLength(20).IsRequired();
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(r => r.CreatedAt).IsRequired();

        builder.HasIndex(r => new { r.QuestionType, r.Version }).IsUnique();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(r => r.CreatedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class RubricConfiguration : IEntityTypeConfiguration<Rubric>
{
    public void Configure(EntityTypeBuilder<Rubric> builder)
    {
        builder.ToTable("Rubrics");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Criterion).HasMaxLength(100).IsRequired();
        builder.Property(r => r.MinScore).HasColumnType("decimal(6,2)").IsRequired();
        builder.Property(r => r.MaxScore).HasColumnType("decimal(6,2)").IsRequired();
        builder.Property(r => r.Weight).HasColumnType("decimal(5,2)").IsRequired();
        builder.Property(r => r.CreatedAt).IsRequired();

        builder.HasIndex(r => new { r.RubricSetId, r.Criterion, r.Level }).IsUnique();

        builder.HasOne(r => r.RubricSet)
            .WithMany(rs => rs.Rubrics)
            .HasForeignKey(r => r.RubricSetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(r => r.CreatedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
