using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Toeic.Domain.Entities;

namespace Toeic.Infrastructure.Persistence.Configurations;

/// <summary>
/// Lưu HASH của refresh token (không bao giờ raw). TokenHash UNIQUE để tra cứu O(1) khi refresh/logout.
/// Không xoá vật lý khi revoke — giữ lại để audit + phát hiện reuse (token bị đánh cắp dùng lại
/// sau khi đã revoke).
/// </summary>
public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.TokenHash).HasMaxLength(128).IsRequired();
        builder.Property(r => r.CreatedByIp).HasMaxLength(64);
        builder.Property(r => r.ReplacedByTokenHash).HasMaxLength(128);
        builder.Property(r => r.CreatedAt).IsRequired();
        builder.Property(r => r.ExpiresAt).IsRequired();

        builder.HasIndex(r => r.TokenHash).IsUnique();
        builder.HasIndex(r => new { r.UserId, r.RevokedAt, r.ExpiresAt });

        builder.HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
