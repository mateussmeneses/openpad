using NUnit.Framework;
using OpenPad.Core.Models;
using OpenPad.Core.Storage;

namespace OpenPad.Tests;

[TestFixture]
public class JsonProfileStoreTests
{
    private JsonProfileStore _store = null!;
    private string _tempDir = null!;

    [SetUp]
    public void SetUp()
    {
        _store = new JsonProfileStore();
        _tempDir = Path.Combine(Path.GetTempPath(), "OpenPadTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    [Test]
    public void DefaultProfile_SerializesAndDeserializes_RoundTrip()
    {
        var original = MappingProfile.CreateDefault("MeuPerfil");
        original.Description = "Teste de descrição customizada";
        original.StickDeadzone = 0.22f;
        original.TriggerThreshold = 0.45f;

        var json = _store.SerializeToString(original);
        Assert.That(json, Is.Not.Null.And.Not.Empty);

        var restored = _store.LoadFromString(json);
        Assert.That(restored.Name, Is.EqualTo("MeuPerfil"));
        Assert.That(restored.Description, Is.EqualTo("Teste de descrição customizada"));
        Assert.That(restored.SchemaVersion, Is.EqualTo(1));
        Assert.That(restored.StickDeadzone, Is.EqualTo(0.22f).Within(0.001f));
        Assert.That(restored.TriggerThreshold, Is.EqualTo(0.45f).Within(0.001f));
        Assert.That(restored.ButtonBindings[GamepadButton.A].Key, Is.EqualTo(VirtualKey.Space));
        Assert.That(restored.ButtonBindings[GamepadButton.RightShoulder].MouseButton, Is.EqualTo(MouseButton.Left));
    }

    [Test]
    public void SaveToFileAndLoadFromFile_RoundTrip()
    {
        var filePath = Path.Combine(_tempDir, "profile.json");
        var original = MappingProfile.CreateDefault("ArquivoPerfil");

        _store.SaveToFile(filePath, original);
        Assert.That(File.Exists(filePath), Is.True);

        var loaded = _store.LoadFromFile(filePath);
        Assert.That(loaded.Name, Is.EqualTo("ArquivoPerfil"));
        Assert.That(loaded.ButtonBindings.Count, Is.EqualTo(original.ButtonBindings.Count));
    }

    [Test]
    public void ValidateProfile_RejectsInvalidSchemaVersion()
    {
        var profile = MappingProfile.CreateDefault();
        profile.SchemaVersion = 0;

        var isValid = _store.ValidateProfile(profile, out var errors);
        Assert.That(isValid, Is.False);
        Assert.That(errors.Any(e => e.Contains("esquema")), Is.True);
    }

    [Test]
    public void ValidateProfile_RejectsEmptyName()
    {
        var profile = MappingProfile.CreateDefault();
        profile.Name = "   ";

        var isValid = _store.ValidateProfile(profile, out var errors);
        Assert.That(isValid, Is.False);
        Assert.That(errors.Any(e => e.Contains("nome")), Is.True);
    }

    [Test]
    public void ValidateProfile_RejectsDeadzoneOutOfRange()
    {
        var profile = MappingProfile.CreateDefault();
        profile.StickDeadzone = 1.5f; // inválido, > 0.9

        var isValid = _store.ValidateProfile(profile, out var errors);
        Assert.That(isValid, Is.False);
        Assert.That(errors.Any(e => e.Contains("morta")), Is.True);
    }

    [Test]
    public void LoadFromString_ThrowsOnEmptyOrCorruptJson()
    {
        Assert.Throws<ArgumentException>(() => _store.LoadFromString(""));
        Assert.Throws<System.Text.Json.JsonException>(() => _store.LoadFromString("{ invalid json"));
    }
}
