using NUnit.Framework;
using OpenPad.Core.Engine;
using OpenPad.Core.Models;
using OpenPad.Core.Testing;

namespace OpenPad.Tests;

[TestFixture]
public class AutoProfileManagerTests
{
    private MappingProfile _defaultProfile = null!;
    private MappingProfile _gameProfile = null!;
    private AutoProfileManager _manager = null!;
    private MappingEngine _engine = null!;
    private RecordingInputSink _sink = null!;

    [SetUp]
    public void SetUp()
    {
        _sink = new RecordingInputSink();
        _defaultProfile = MappingProfile.CreateDefault("DefaultDesktop");
        _gameProfile = new MappingProfile
        {
            Name = "DoomProfile",
            AssociatedProcessNames = new List<string> { "doom.exe", "DOOMx64.exe" }
        };

        _manager = new AutoProfileManager(_defaultProfile);
        _manager.RegisterProfile(_gameProfile);

        _engine = new MappingEngine(_sink, _defaultProfile);
    }

    [TearDown]
    public void TearDown()
    {
        _engine.Dispose();
        _sink.Dispose();
    }

    [Test]
    public void ResolveProfile_WithMatchingProcess_ReturnsGameProfile()
    {
        // Deve encontrar com case-insensitive e com/sem extensão .exe
        var p1 = _manager.ResolveProfileForProcess("doom.exe");
        Assert.That(p1.Name, Is.EqualTo("DoomProfile"));

        var p2 = _manager.ResolveProfileForProcess("DOOMX64");
        Assert.That(p2.Name, Is.EqualTo("DoomProfile"));
    }

    [Test]
    public void ResolveProfile_WithUnrelatedProcess_ReturnsDefaultProfile()
    {
        var p = _manager.ResolveProfileForProcess("chrome.exe");
        Assert.That(p.Name, Is.EqualTo("DefaultDesktop"));
    }

    [Test]
    public void CheckAndApplyForegroundProcess_SwitchesEngineProfileCorrectly()
    {
        Assert.That(_engine.CurrentProfile.Name, Is.EqualTo("DefaultDesktop"));

        // 1. Usuário abre Doom -> deve aplicar DoomProfile
        bool switchedToGame = _manager.CheckAndApplyForegroundProcess("doom.exe", _engine);
        Assert.That(switchedToGame, Is.True);
        Assert.That(_engine.CurrentProfile.Name, Is.EqualTo("DoomProfile"));

        // 2. Continua em Doom -> não deve trocar de perfil inutilmente
        bool redundantCheck = _manager.CheckAndApplyForegroundProcess("doom.exe", _engine);
        Assert.That(redundantCheck, Is.False);

        // 3. Usuário dá Alt+Tab para o navegador -> deve restaurar DefaultDesktop
        bool switchedToBrowser = _manager.CheckAndApplyForegroundProcess("msedge.exe", _engine);
        Assert.That(switchedToBrowser, Is.True);
        Assert.That(_engine.CurrentProfile.Name, Is.EqualTo("DefaultDesktop"));
    }
}
