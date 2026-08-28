using System.Security.Cryptography;
using Toeic.Application.Common.Interfaces;

namespace Toeic.Infrastructure.Security;

/// <summary>
/// PBKDF2-HMAC-SHA256, 100_000 vòng lặp, salt 128-bit ngẫu nhiên riêng cho mỗi mật khẩu.
/// Dùng API sẵn có của .NET (System.Security.Cryptography.Rfc2898DeriveBytes) — không cần
/// thêm NuGet package ngoài, giảm rủi ro phụ thuộc.
/// Định dạng lưu: "{iterations}.{saltBase64}.{hashBase64}" để có thể tăng iterations sau này
/// mà vẫn verify được các hash cũ.
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    private const int SaltSizeBytes = 16;   // 128-bit
    private const int HashSizeBytes = 32;   // 256-bit
    private const int Iterations = 100_000;

    public string Hash(string plainPassword)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password: plainPassword,
            salt: salt,
            iterations: Iterations,
            hashAlgorithm: HashAlgorithmName.SHA256,
            outputLength: HashSizeBytes);

        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string plainPassword, string passwordHash)
    {
        var parts = passwordHash.Split('.', 3);
        if (parts.Length != 3) return false;

        if (!int.TryParse(parts[0], out var iterations)) return false;

        byte[] salt, expectedHash;
        try
        {
            salt = Convert.FromBase64String(parts[1]);
            expectedHash = Convert.FromBase64String(parts[2]);
        }
        catch (FormatException)
        {
            return false;
        }

        var actualHash = Rfc2898DeriveBytes.Pbkdf2(
            password: plainPassword,
            salt: salt,
            iterations: iterations,
            hashAlgorithm: HashAlgorithmName.SHA256,
            outputLength: expectedHash.Length);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}
