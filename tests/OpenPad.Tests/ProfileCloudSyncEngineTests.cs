using System.Text;
using NUnit.Framework;
using OpenPad.Core.Engine;
using OpenPad.Core.Models;

namespace OpenPad.Tests;

[TestFixture]
public class ProfileCloudSyncEngineTests
{
    private ProfileCloudSyncEngine _engine = null!;
    private readonly byte[] _key = Encoding.UTF8.GetBytes("SuperSecretOpenPadSignatureKey2026!");

    [SetUp]
    public void SetUp()
    {
        _engine = new ProfileCloudSyncEngine();
    }

    [Test]
    public void ExportSignedProfile_GeneratesTamperEvidentBundle()
    {
        var profile = new MappingProfile
        {
            Name = "Apex Legends",
            AssociatedProcessNames = ["r5apex", "r5apex.exe"]
        };

        var bundle = _engine.ExportSignedProfile(profile, "author_123", _key);

        Assert.That(bundle.AuthorId, Is.EqualTo("author_123"));
        Assert.That(bundle.SignatureHex, Has.Length.EqualTo(64)); // SHA256 hex is 64 chars

        var result = _engine.VerifyAndImport(bundle, _key);
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Profile!.Name, Is.EqualTo("Apex Legends"));
    }

    [Test]
    public void VerifyAndImport_WhenPayloadIsTampered_RejectsImport()
    {
        var profile = new MappingProfile { Name = "Elden Ring" };
        var bundle = _engine.ExportSignedProfile(profile, "author_123", _key);

        // Maliciously tamper with the JSON string
        var tamperedBundle = bundle with { ProfileJson = bundle.ProfileJson.Replace("Elden Ring", "Malicious Profile") };

        var result = _engine.VerifyAndImport(tamperedBundle, _key);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorReason, Does.Contain("Cryptographic signature mismatch"));
        Assert.That(result.Profile, Is.Null);
    }

    [Test]
    public void VerifyAndImport_WhenTargetingSystemProcesses_RejectsBySecurityPolicy()
    {
        var maliciousProfile = new MappingProfile
        {
            Name = "System Hijacker",
            AssociatedProcessNames = ["cmd.exe", "powershell.exe"]
        };

        var bundle = _engine.ExportSignedProfile(maliciousProfile, "author_bad", _key);

        var result = _engine.VerifyAndImport(bundle, _key);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorReason, Does.Contain("Security Policy Violation"));
        Assert.That(result.Profile, Is.Null);
    }
}
