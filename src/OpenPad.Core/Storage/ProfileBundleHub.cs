using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenPad.Core.Models;

namespace OpenPad.Core.Storage;

public class ProfilePackage
{
    [JsonPropertyName("formatVersion")]
    public string FormatVersion { get; set; } = "1.0";

    [JsonPropertyName("exportedAt")]
    public DateTime ExportedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("author")]
    public string Author { get; set; } = "User";

    [JsonPropertyName("profile")]
    public MappingProfile Profile { get; set; } = null!;

    [JsonPropertyName("checksumSha256")]
    public string ChecksumSha256 { get; set; } = string.Empty;
}

/// <summary>
/// Packages mapping profiles into portable bundles with SHA-256 tamper-proof verification.
/// </summary>
public static class ProfileBundleHub
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string CreatePackageJson(MappingProfile profile, string author = "User")
    {
        ArgumentNullException.ThrowIfNull(profile);

        string profileJson = JsonSerializer.Serialize(profile, JsonOptions);
        string hash = ComputeSha256(profileJson);

        var package = new ProfilePackage
        {
            FormatVersion = "1.0",
            ExportedAt = DateTime.UtcNow,
            Author = author,
            Profile = profile,
            ChecksumSha256 = hash
        };

        return JsonSerializer.Serialize(package, JsonOptions);
    }

    public static MappingProfile LoadPackageJson(string packageJson)
    {
        if (string.IsNullOrWhiteSpace(packageJson))
        {
            throw new ArgumentException("Conteúdo do pacote vazio ou inválido.", nameof(packageJson));
        }

        var package = JsonSerializer.Deserialize<ProfilePackage>(packageJson, JsonOptions)
            ?? throw new InvalidOperationException("Falha ao desserializar pacote de perfil.");

        if (package.Profile == null)
        {
            throw new InvalidOperationException("Pacote não contém um perfil válido.");
        }

        // Recompute hash on inner profile to verify integrity
        string profileJson = JsonSerializer.Serialize(package.Profile, JsonOptions);
        string computedHash = ComputeSha256(profileJson);

        if (!string.Equals(computedHash, package.ChecksumSha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new CryptographicException("Checksum SHA-256 do pacote de perfil inválido ou corrompido.");
        }

        return package.Profile;
    }

    private static string ComputeSha256(string rawData)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawData));
        return Convert.ToHexStringLower(bytes);
    }
}
