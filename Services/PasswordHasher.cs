using System.Security.Cryptography;

namespace AutoMailerBackend.Services;

public static class PasswordHasher
{
    private const int Iterations = 100_000;
    private const int KeySize = 32;
    private const int SaltSize = 32;

    public static string GenerateSalt()
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        return Convert.ToBase64String(salt);
    }

    public static string Hash(string password, string salt)
    {
        var saltBytes = Convert.FromBase64String(salt);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, saltBytes, Iterations, HashAlgorithmName.SHA256, KeySize);
        return Convert.ToBase64String(hash);
    }

    public static bool Verify(string password, string salt, string hash)
    {
        return Hash(password, salt) == hash;
    }
}
