namespace Toeic.Application.Common.Interfaces;

/// <summary>Băm/verify mật khẩu. Implementation dùng PBKDF2 (Infrastructure.Security.PasswordHasher).</summary>
public interface IPasswordHasher
{
    string Hash(string plainPassword);
    bool Verify(string plainPassword, string passwordHash);
}
