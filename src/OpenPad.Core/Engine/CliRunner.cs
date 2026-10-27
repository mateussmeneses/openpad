using OpenPad.Core.Models;

namespace OpenPad.Core.Engine;

public class OpenPadCliOptions
{
    public string? ProfilePath { get; set; }
    public bool ArmImmediately { get; set; }
    public bool StartInTray { get; set; }
    public StickCurveType? CurveOverride { get; set; }
    public bool ShowHelp { get; set; }
    public bool ShowVersion { get; set; }
    public bool ListProfiles { get; set; }
}

/// <summary>
/// Command-line interface parser and headless runner for OpenPad automation and shortcuts.
/// </summary>
public static class CliRunner
{
    public const string CurrentVersion = "1.0.0";

    public static OpenPadCliOptions Parse(string[] args)
    {
        var options = new OpenPadCliOptions();
        if (args == null || args.Length == 0) return options;

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i].Trim();

            if (arg.Equals("--help", StringComparison.OrdinalIgnoreCase) || arg.Equals("-h", StringComparison.OrdinalIgnoreCase))
            {
                options.ShowHelp = true;
            }
            else if (arg.Equals("--version", StringComparison.OrdinalIgnoreCase) || arg.Equals("-v", StringComparison.OrdinalIgnoreCase))
            {
                options.ShowVersion = true;
            }
            else if (arg.Equals("--arm", StringComparison.OrdinalIgnoreCase))
            {
                options.ArmImmediately = true;
            }
            else if (arg.Equals("--tray", StringComparison.OrdinalIgnoreCase) || arg.Equals("-t", StringComparison.OrdinalIgnoreCase))
            {
                options.StartInTray = true;
            }
            else if (arg.Equals("--list-profiles", StringComparison.OrdinalIgnoreCase))
            {
                options.ListProfiles = true;
            }
            else if (arg.Equals("--profile", StringComparison.OrdinalIgnoreCase) || arg.Equals("-p", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 < args.Length && !args[i + 1].StartsWith('-'))
                {
                    options.ProfilePath = args[++i].Trim();
                }
            }
            else if (arg.Equals("--curve", StringComparison.OrdinalIgnoreCase) || arg.Equals("-c", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 < args.Length && !args[i + 1].StartsWith('-'))
                {
                    string curveStr = args[++i].Trim();
                    if (Enum.TryParse<StickCurveType>(curveStr, ignoreCase: true, out var curve))
                    {
                        options.CurveOverride = curve;
                    }
                }
            }
        }

        return options;
    }

    public static string GetHelpText()
    {
        return
            $"OpenPad v{CurrentVersion} - Open-source Gamepad-to-Keyboard/Mouse Remapper\n\n" +
            "Uso: openpad [opções]\n\n" +
            "Opções:\n" +
            "  -p, --profile <caminho|nome>  Carrega automaticamente o perfil especificado\n" +
            "      --arm                     Ativa o motor de mapeamento imediatamente ao iniciar\n" +
            "  -t, --tray                    Inicia minimizado na bandeja do sistema (System Tray)\n" +
            "  -c, --curve <tipo>            Sobrescreve a curva do analógico (Linear, Quadratic, Aggressive, Smooth)\n" +
            "      --list-profiles           Lista os perfis disponíveis na pasta de configurações\n" +
            "  -v, --version                 Exibe a versão do OpenPad\n" +
            "  -h, --help                    Exibe esta mensagem de ajuda\n";
    }
}
