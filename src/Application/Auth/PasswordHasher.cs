using System.Security.Cryptography;
using System.Text;

namespace Application.Auth;

/// <summary>
/// Genera y verifica hashes PBKDF2 (SHA-256) con salt aleatorio por usuario.
/// Formato del hash almacenado: <c>PBKDF2$iteraciones$saltBase64$hashBase64</c>.
/// </summary>
public static class PasswordHasher
{
    private const int DefaultIterations = 100_000;
    private const int SaltSizeBytes = 16;
    private const int HashSizeBytes = 32;

    /// <summary>
    /// Genera un hash PBKDF2 para el password dado, listo para guardar en configuración.
    /// </summary>
    public static string Hash(string password)
    {
        ArgumentNullException.ThrowIfNull(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
        var hash = Derive(password, salt, DefaultIterations);

        return Pack(salt, hash, DefaultIterations);
    }

    /// <summary>
    /// Verifica un password contra un hash almacenado.
    /// </summary>
    public static bool Verify(string password, string storedHash)
    {
        ArgumentNullException.ThrowIfNull(password);
        ArgumentNullException.ThrowIfNull(storedHash);

        if (!TryUnpack(storedHash, out var salt, out var expectedHash, out var iterations))
        {
            return false;
        }

        var actualHash = Derive(password, salt, iterations);
        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }

    private static byte[] Derive(string password, byte[] salt, int iterations) =>
        Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            HashSizeBytes
        );

    private static string Pack(byte[] salt, byte[] hash, int iterations) =>
        $"PBKDF2${iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";

    private static bool TryUnpack(
        string storedHash,
        out byte[] salt,
        out byte[] hash,
        out int iterations
    )
    {
        salt = [];
        hash = [];
        iterations = 0;

        var parts = storedHash.Split('$');
        if (parts.Length != 4 || parts[0] != "PBKDF2")
        {
            return false;
        }

        if (!int.TryParse(parts[1], out iterations) || iterations < 1)
        {
            return false;
        }

        try
        {
            salt = Convert.FromBase64String(parts[2]);
            hash = Convert.FromBase64String(parts[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        return salt.Length > 0 && hash.Length > 0;
    }
}
