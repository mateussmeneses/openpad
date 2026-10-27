namespace OpenPad.Core.Engine;

public enum SecurityGuardAction
{
    Allow,
    BlockInputs,
    TriggerEmergencyDisarm
}

/// <summary>
/// Security guard preventing injection of synthetic inputs into sensitive Windows system processes,
/// credential prompts, and password managers.
/// </summary>
public class ProcessSecurityGuard
{
    private static readonly HashSet<string> DefaultBlacklist = new(StringComparer.OrdinalIgnoreCase)
    {
        "taskmgr.exe",
        "consent.exe",
        "logonui.exe",
        "lockapp.exe",
        "credentialuibroker.exe",
        "keepass.exe",
        "1password.exe",
        "bitwarden.exe"
    };

    private readonly HashSet<string> _blacklist;
    private readonly HashSet<string> _whitelist = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _syncLock = new();

    public bool AutoDisarmOnBlacklist { get; set; } = true;
    public bool IsEnabled { get; set; } = true;

    public event EventHandler<string>? BlacklistTriggered;

    public ProcessSecurityGuard(IEnumerable<string>? customBlacklist = null)
    {
        _blacklist = new HashSet<string>(DefaultBlacklist, StringComparer.OrdinalIgnoreCase);
        if (customBlacklist != null)
        {
            foreach (var item in customBlacklist)
            {
                _blacklist.Add(NormalizeProcessName(item));
            }
        }
    }

    public SecurityGuardAction EvaluateProcess(string processName)
    {
        if (!IsEnabled || string.IsNullOrWhiteSpace(processName))
        {
            return SecurityGuardAction.Allow;
        }

        string normalized = NormalizeProcessName(processName);

        lock (_syncLock)
        {
            // Whitelist takes absolute precedence
            if (_whitelist.Contains(normalized))
            {
                return SecurityGuardAction.Allow;
            }

            if (_blacklist.Contains(normalized))
            {
                BlacklistTriggered?.Invoke(this, normalized);
                return AutoDisarmOnBlacklist
                    ? SecurityGuardAction.TriggerEmergencyDisarm
                    : SecurityGuardAction.BlockInputs;
            }

            return SecurityGuardAction.Allow;
        }
    }

    public void AddBlacklist(string processName)
    {
        if (string.IsNullOrWhiteSpace(processName)) return;
        lock (_syncLock)
        {
            _blacklist.Add(NormalizeProcessName(processName));
        }
    }

    public bool RemoveBlacklist(string processName)
    {
        if (string.IsNullOrWhiteSpace(processName)) return false;
        lock (_syncLock)
        {
            return _blacklist.Remove(NormalizeProcessName(processName));
        }
    }

    public void AddWhitelist(string processName)
    {
        if (string.IsNullOrWhiteSpace(processName)) return;
        lock (_syncLock)
        {
            _whitelist.Add(NormalizeProcessName(processName));
        }
    }

    public bool RemoveWhitelist(string processName)
    {
        if (string.IsNullOrWhiteSpace(processName)) return false;
        lock (_syncLock)
        {
            return _whitelist.Remove(NormalizeProcessName(processName));
        }
    }

    public bool IsBlacklisted(string processName)
    {
        if (string.IsNullOrWhiteSpace(processName)) return false;
        lock (_syncLock)
        {
            return _blacklist.Contains(NormalizeProcessName(processName));
        }
    }

    private static string NormalizeProcessName(string name)
    {
        string fileName = Path.GetFileName(name.Trim());
        return fileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? fileName
            : $"{fileName}.exe";
    }
}
