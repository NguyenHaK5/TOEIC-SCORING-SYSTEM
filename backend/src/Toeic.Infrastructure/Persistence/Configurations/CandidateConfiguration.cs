using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Toeic.Domain.Entities;

namespace Toeic.Infrastructure.Persistence.Configurations;

public class CandidateConfiguration : IEntityTypeConfiguration<Candidate>
{
    public void Configure(EntityTypeBuilder<Candidate> builder)
    {
        builder.ToTable("Candidates");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.CandidateCode).HasMaxLength(50).IsRequired();
        builder.Property(c => c.FullName).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Gender).HasMaxLength(20);
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(c => c.CreatedAt).IsRequired();

        // ISSUE-08: quan hệ 1:1 với Users
        builder.HasIndex(c => c.UserId).IsUnique();
        builder.HasIndex(c => c.CandidateCode).IsUnique();
    }
}
