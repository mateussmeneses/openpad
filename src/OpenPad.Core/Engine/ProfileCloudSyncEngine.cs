using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using OpenPad.Core.Models;

namespace OpenPad.Core.Engine;

/// <summary>
/// Tamper-evident cryptographic package for community profile sharing.
/// </summary>
public record SignedProfileBundle(
    string ProfileJson,
    string AuthorId,
    DateTime Timestamp,
    string SignatureHex
);

/// <summary>
/// Result of verifying and importing a cryptographically signed profile.
/// </summary>
public record ProfileValidationResult(
    bool IsValid,
    string? ErrorReason,
    MappingProfile? Profile
);

/// <summary>
/// Secure cloud synchronization and cryptographic validation engine for community profiles.
/// Employs HMAC-SHA256 signatures and strict sandboxing against malicious macro injections.
/// </summary>
public class ProfileCloudSyncEngine
{
    private static readonly HashSet<string> RestrictedProcesses = new(StringComparer.OrdinalIgnoreCase)
    {
        "cmd",
        "cmd.exe",
        "powershell",
        "powershell.exe",
        "pwsh",
        "pwsh.exe",
        "taskmgr",
        "taskmgr.exe",
        "regedit",
        "regedit.exe"
    };

    /// <summary>
    /// Signs and serializes a mapping profile into a tamper-evident bundle.
    /// </summary>
    public SignedProfileBundle ExportSignedProfile(
        MappingProfile profile,
        string authorId,
        byte[] secretKey)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(secretKey);

        string json = JsonSerializer.Serialize(profile);
        string signature = ComputeHmac(json, secretKey);

        return new SignedProfileBundle(
            ProfileJson: json,
            AuthorId: authorId,
            Timestamp: DateTime.UtcNow,
            SignatureHex: signature
        );
    }

    /// <summary>
    /// Verifies cryptographic signature and security policy compliance before importing.
    /// </summary>
    public ProfileValidationResult VerifyAndImport(
        SignedProfileBundle bundle,
        byte[] secretKey)
    {
        ArgumentNullException.ThrowIfNull(bundle);
        ArgumentNullException.ThrowIfNull(secretKey);

        // 1. Cryptographic Signature Validation
        string expectedSig = ComputeHmac(bundle.ProfileJson, secretKey);
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expectedSig),
                Encoding.UTF8.GetBytes(bundle.SignatureHex)))
        {
            return new ProfileValidationResult(
                IsValid: false,
                ErrorReason: "Cryptographic signature mismatch: profile payload has been tampered with.",
                Profile: null
            );
        }

        // 2. Deserialization
        MappingProfile? profile;
        try
        {
            profile = JsonSerializer.Deserialize<MappingProfile>(bundle.ProfileJson);
        }
        catch (Exception ex)
        {
            return new ProfileValidationResult(
                IsValid: false,
                ErrorReason: $"Malformed profile JSON: {ex.Message}",
                Profile: null
            );
        }

        if (profile == null)
        {
            return new ProfileValidationResult(
                IsValid: false,
                ErrorReason: "Profile deserialization produced null.",
                Profile: null
            );
        }

        // 3. Security Policy Audit: Process Targeting Sandbox
        foreach (var proc in profile.AssociatedProcessNames)
        {
            if (RestrictedProcesses.Contains(proc))
            {
                return new ProfileValidationResult(
                    IsValid: false,
                    ErrorReason: $"Security Policy Violation: Profile attempts to bind to restricted system process '{proc}'.",
                    Profile: null
                );
            }
        }

        return new ProfileValidationResult(
            IsValid: true,
            ErrorReason: null,
            Profile: profile
        );
    }

    private static string ComputeHmac(string data, byte[] key)
    {
        using var hmac = new HMACSHA256(key);
        byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexStringLower(hash);
    }
}
