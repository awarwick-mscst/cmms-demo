using System.Security.Cryptography;
using Konscious.Security.Cryptography;
using System.Text;

namespace LicensingServer.Web.Services.Security;

public class PasswordHashingService
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int MemorySize = 47104; // ~46 MB (OWASP recommendation)
    private const int Iterations = 2;
    private const int Parallelism = 1;

    public string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = ComputeHash(password, salt);

        return $"$argon2id$v=19$m={MemorySize},t={Iterations},p={Parallelism}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        try
        {
            var parts = hashedPassword.Split('$');
            if (parts.Length != 6 || parts[1] != "argon2id")
                return false;

            var paramParts = parts[3].Split(',');
            var m = int.Parse(paramParts[0].Split('=')[1]);
            var t = int.Parse(paramParts[1].Split('=')[1]);
            var p = int.Parse(paramParts[2].Split('=')[1]);

            var salt = Convert.FromBase64String(parts[4]);
            var expectedHash = Convert.FromBase64String(parts[5]);

            var actualHash = ComputeHash(password, salt, m, t, p);

            return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
        }
        catch
        {
            return false;
        }
    }

    private static byte[] ComputeHash(string password, byte[] salt, int memorySize = MemorySize, int iterations = Iterations, int parallelism = Parallelism)
    {
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            MemorySize = memorySize,
            Iterations = iterations,
            DegreeOfParallelism = parallelism,
        };

        return argon2.GetBytes(HashSize);
    }
}
