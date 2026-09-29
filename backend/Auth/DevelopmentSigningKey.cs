using System.Security.Cryptography;

namespace TicketApi.Auth;

/// <summary>The token signing key used when no <c>Auth:SigningKey</c> is configured: generated
/// once and kept in a gitignored file beside the database, so tokens outlive a restart without
/// anyone choosing a secret -- ADR-0002 section 7, the generated development signing key.</summary>
public static class DevelopmentSigningKey
{
    public static string LoadOrCreate(string path)
    {
        if (File.Exists(path))
            return File.ReadAllText(path).Trim();

        var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path, key);
        if (!OperatingSystem.IsWindows())
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);

        return key;
    }
}
