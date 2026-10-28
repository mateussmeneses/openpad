using System.IO;
using NUnit.Framework;
using OpenPad.Core.Engine;

namespace OpenPad.Tests;

[TestFixture]
public class GameLibraryScannerTests
{
    private GameLibraryScanner _scanner = null!;

    [SetUp]
    public void SetUp()
    {
        _scanner = new GameLibraryScanner();
    }

    [Test]
    public void IsValidGameExecutable_FiltersHelperAndCrashReporters()
    {
        Assert.That(_scanner.IsValidGameExecutable(@"C:\Games\Doom\DoomEternal.exe"), Is.True);
        Assert.That(_scanner.IsValidGameExecutable(@"C:\Games\Doom\CrashReportClient.exe"), Is.False);
        Assert.That(_scanner.IsValidGameExecutable(@"C:\Games\Doom\EasyAntiCheat.exe"), Is.False);
        Assert.That(_scanner.IsValidGameExecutable(@"C:\Games\Doom\unins000.exe"), Is.False);
        Assert.That(_scanner.IsValidGameExecutable(@"C:\Games\Doom\dxsetup.exe"), Is.False);
    }

    [Test]
    public void ParseSteamLibraryFolders_ExtractsConfiguredPaths()
    {
        string mockVdf = @"
""libraryfolders""
{
    ""0""
    {
        ""path"" ""C:\\Program Files (x86)\\Steam""
        ""label"" """"
    }
    ""1""
    {
        ""path"" ""D:\\SteamLibrary""
        ""label"" ""Games SSD""
    }
}";

        var paths = _scanner.ParseSteamLibraryFolders(mockVdf);

        Assert.That(paths.Count, Is.EqualTo(2));
        Assert.That(paths[0], Is.EqualTo(@"C:\Program Files (x86)\Steam"));
        Assert.That(paths[1], Is.EqualTo(@"D:\SteamLibrary"));
    }

    [Test]
    public void RegisterGame_WhenValidExecutable_ReturnsDiscoveredGame()
    {
        var game = _scanner.RegisterGame(
            gameName: "Cyberpunk 2077",
            platform: GamePlatform.GOGGalaxy,
            executablePath: @"D:\Games\Cyberpunk 2077\bin\x64\Cyberpunk2077.exe",
            installDirectory: @"D:\Games\Cyberpunk 2077"
        );

        Assert.That(game, Is.Not.Null);
        Assert.That(game!.ProcessName, Is.EqualTo("Cyberpunk2077"));
        Assert.That(game.Platform, Is.EqualTo(GamePlatform.GOGGalaxy));
    }

    [Test]
    public void GenerateDefaultProfile_CreatesMappingProfileLinkedToProcess()
    {
        var game = new DiscoveredGame(
            GameName: "Hades II",
            Platform: GamePlatform.Steam,
            ExecutablePath: @"C:\Steam\steamapps\common\Hades II\Ship\Hades2.exe",
            ProcessName: "Hades2",
            InstallDirectory: @"C:\Steam\steamapps\common\Hades II"
        );

        var profile = _scanner.GenerateDefaultProfile(game);

        Assert.That(profile.Name, Is.EqualTo("Hades II Profile"));
        Assert.That(profile.AssociatedProcessNames, Contains.Item("Hades2"));
        Assert.That(profile.AssociatedProcessNames, Contains.Item("Hades2.exe"));
    }
}
