using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using OpenPad.Core.Models;

namespace OpenPad.Core.Engine;

/// <summary>
/// Digital distribution platform where the game was discovered.
/// </summary>
public enum GamePlatform
{
    Steam,
    EpicGamesStore,
    XboxGamePass,
    GOGGalaxy,
    Custom
}

/// <summary>
/// Metadata describing an installed game discovered on the local system.
/// </summary>
public record DiscoveredGame(
    string GameName,
    GamePlatform Platform,
    string ExecutablePath,
    string ProcessName,
    string InstallDirectory
);

/// <summary>
/// Scans digital PC gaming distribution platforms (Steam, Epic Games, GOG, Xbox Game Pass)
/// to detect installed titles and automatically generate process-linked profiles.
/// </summary>
public class GameLibraryScanner
{
    private static readonly HashSet<string> IgnoredExeNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CrashReportClient.exe",
        "UnityCrashHandler64.exe",
        "UnityCrashHandler32.exe",
        "EasyAntiCheat.exe",
        "EasyAntiCheat_Setup.exe",
        "Uninstall.exe",
        "unins000.exe",
        "Launcher.exe",
        "update.exe",
        "dxsetup.exe",
        "vcredist_x64.exe"
    };

    /// <summary>
    /// Checks whether an executable path corresponds to a primary game process rather than a helper/launcher.
    /// </summary>
    public bool IsValidGameExecutable(string exePath)
    {
        if (string.IsNullOrWhiteSpace(exePath)) return false;
        string fileName = Path.GetFileName(exePath);

        if (IgnoredExeNames.Contains(fileName)) return false;
        if (fileName.StartsWith("unins", StringComparison.OrdinalIgnoreCase)) return false;
        if (fileName.EndsWith("setup.exe", StringComparison.OrdinalIgnoreCase)) return false;

        return true;
    }

    /// <summary>
    /// Parses Steam's libraryfolders.vdf configuration to discover configured library paths.
    /// </summary>
    public List<string> ParseSteamLibraryFolders(string vdfContent)
    {
        var paths = new List<string>();
        if (string.IsNullOrWhiteSpace(vdfContent)) return paths;

        // Matches: "path" "C:\\SteamLibrary"
        var matches = Regex.Matches(vdfContent, @"""path""\s+""([^""]+)""");
        foreach (Match match in matches)
        {
            if (match.Groups.Count > 1)
            {
                paths.Add(match.Groups[1].Value.Replace(@"\\", @"\"));
            }
        }

        return paths;
    }

    /// <summary>
    /// Creates a game record if the executable is valid.
    /// </summary>
    public DiscoveredGame? RegisterGame(
        string gameName,
        GamePlatform platform,
        string executablePath,
        string installDirectory)
    {
        if (!IsValidGameExecutable(executablePath))
        {
            return null;
        }

        string processName = Path.GetFileNameWithoutExtension(executablePath);

        return new DiscoveredGame(
            GameName: gameName,
            Platform: platform,
            ExecutablePath: executablePath,
            ProcessName: processName,
            InstallDirectory: installDirectory
        );
    }

    /// <summary>
    /// Generates an OpenPad baseline MappingProfile mapped to the discovered game process.
    /// </summary>
    public MappingProfile GenerateDefaultProfile(DiscoveredGame game)
    {
        ArgumentNullException.ThrowIfNull(game);

        return new MappingProfile
        {
            Name = $"{game.GameName} Profile",
            Description = $"Auto-generated profile for {game.GameName} ({game.Platform})",
            AssociatedProcessNames = [game.ProcessName, $"{game.ProcessName}.exe"]
        };
    }
}
