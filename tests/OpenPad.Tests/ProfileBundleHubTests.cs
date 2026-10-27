using System.Security.Cryptography;
using NUnit.Framework;
using OpenPad.Core.Models;
using OpenPad.Core.Storage;

namespace OpenPad.Tests;

[TestFixture]
public class ProfileBundleHubTests
{
    private MappingProfile _sampleProfile = null!;

    [SetUp]
    public void SetUp()
    {
        _sampleProfile = new MappingProfile
        {
            Name = "FPS Competitive",
            Description = "Optimized for fast-paced shooters",
            StickCurveType = StickCurveType.Aggressive,
            StickDeadzone = 0.08f,
            AssociatedProcessNames = new List<string> { "Doom.exe", "Quake.exe" }
        };
        _sampleProfile.ButtonBindings[GamepadButton.A] = InputAction.FromKey(VirtualKey.Space);
        _sampleProfile.ButtonBindings[GamepadButton.RightShoulder] = InputAction.FromMouse(MouseButton.Left);
    }

    [Test]
    public void CreatePackageJson_GeneratesValidPackageWithSha256Checksum()
    {
        string packageJson = ProfileBundleHub.CreatePackageJson(_sampleProfile, author: "ProGamer");

        Assert.That(packageJson, Does.Contain("\"formatVersion\": \"1.0\""));
        Assert.That(packageJson, Does.Contain("\"author\": \"ProGamer\""));
        Assert.That(packageJson, Does.Contain("\"checksumSha256\":"));
        Assert.That(packageJson, Does.Contain("\"name\": \"FPS Competitive\""));
    }

    [Test]
    public void LoadPackageJson_ValidPackage_RestoresIdenticalProfile()
    {
        string packageJson = ProfileBundleHub.CreatePackageJson(_sampleProfile, author: "Alice");

        var restored = ProfileBundleHub.LoadPackageJson(packageJson);

        Assert.That(restored.Name, Is.EqualTo("FPS Competitive"));
        Assert.That(restored.StickCurveType, Is.EqualTo(StickCurveType.Aggressive));
        Assert.That(restored.StickDeadzone, Is.EqualTo(0.08f));
        Assert.That(restored.AssociatedProcessNames, Contains.Item("Doom.exe"));
        Assert.That(restored.ButtonBindings[GamepadButton.A].Key, Is.EqualTo(VirtualKey.Space));
    }

    [Test]
    public void LoadPackageJson_TamperedContent_ThrowsCryptographicException()
    {
        string packageJson = ProfileBundleHub.CreatePackageJson(_sampleProfile);

        // Tamper with content by changing the name without recomputing hash
        string tampered = packageJson.Replace("FPS Competitive", "Hacked Profile");

        Assert.Throws<CryptographicException>(() => ProfileBundleHub.LoadPackageJson(tampered));
    }

    [Test]
    public void LoadPackageJson_EmptyPayload_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => ProfileBundleHub.LoadPackageJson(""));
    }
}
