using System.Security.Cryptography;

namespace HR.Services;

public class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16; // 128-bit
    private const int KeySize = 32;  // 256-bit
    private const int Iterations = 100_000;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    public string HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            throw new ArgumentException("كلمة المرور لا يمكن أن تكون فارغة.", nameof(password));
        }

        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, KeySize);

        // Format: {salt}:{key} in Base64
        return $"v1${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(key)}";
    }

    public bool VerifyPassword(string? hashedPassword, string? providedPassword)
    {
        if (string.IsNullOrEmpty(hashedPassword) || string.IsNullOrEmpty(providedPassword))
        {
            return false;
        }

        // Standard PBKDF2 format
        if (hashedPassword.StartsWith("v1$"))
        {
            var parts = hashedPassword.Split('$');
            if (parts.Length != 4) return false;

            if (!int.TryParse(parts[1], out int iterations)) return false;
            byte[] salt = Convert.FromBase64String(parts[2]);
            byte[] expectedKey = Convert.FromBase64String(parts[3]);

            byte[] actualKey = Rfc2898DeriveBytes.Pbkdf2(providedPassword, salt, iterations, Algorithm, expectedKey.Length);
            return CryptographicOperations.FixedTimeEquals(actualKey, expectedKey);
        }

        // Fallback for simple legacy or plain text password matching
        if (string.Equals(hashedPassword, providedPassword, StringComparison.Ordinal))
        {
            return true;
        }

        return false;
    }
}
