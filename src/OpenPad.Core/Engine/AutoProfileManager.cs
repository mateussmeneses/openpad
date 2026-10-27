using OpenPad.Core.Models;

namespace OpenPad.Core.Engine;

/// <summary>
/// Gerenciador de alternância automática de perfis de acordo com o processo em primeiro plano.
/// Permite carregar perfis específicos para jogos (ex: doom.exe, cyberpunk2077.exe) e
/// restaurar o perfil padrão ao retornar para a área de trabalho ou navegador.
/// </summary>
public class AutoProfileManager
{
    private readonly List<MappingProfile> _profiles = new();
    private MappingProfile _defaultProfile;
    private string? _lastProcessName;

    public event EventHandler<(string ProcessName, MappingProfile Profile)>? ProfileAutoSwitched;

    public IReadOnlyList<MappingProfile> Profiles => _profiles.ToList();
    public MappingProfile DefaultProfile => _defaultProfile;
    public string? LastActiveProcess => _lastProcessName;

    public AutoProfileManager(MappingProfile defaultProfile)
    {
        _defaultProfile = defaultProfile ?? throw new ArgumentNullException(nameof(defaultProfile));
        _profiles.Add(_defaultProfile);
    }

    public void RegisterProfile(MappingProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        _profiles.RemoveAll(p => string.Equals(p.Name, profile.Name, StringComparison.OrdinalIgnoreCase));
        _profiles.Add(profile);
    }

    public void SetDefaultProfile(MappingProfile defaultProfile)
    {
        _defaultProfile = defaultProfile ?? throw new ArgumentNullException(nameof(defaultProfile));
        RegisterProfile(_defaultProfile);
    }

    /// <summary>
    /// Localiza o perfil correspondente ao nome do executável informado.
    /// Caso nenhum perfil associe o processo, retorna o perfil padrão.
    /// </summary>
    public MappingProfile ResolveProfileForProcess(string? processName)
    {
        if (string.IsNullOrWhiteSpace(processName))
        {
            return _defaultProfile;
        }

        var normalizedTarget = NormalizeProcessName(processName);

        foreach (var profile in _profiles)
        {
            if (profile.AssociatedProcessNames.Any(proc =>
                string.Equals(NormalizeProcessName(proc), normalizedTarget, StringComparison.OrdinalIgnoreCase)))
            {
                return profile;
            }
        }

        return _defaultProfile;
    }

    private static string NormalizeProcessName(string name)
    {
        var clean = Path.GetFileName(name.Trim());
        return clean.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? clean[..^4]
            : clean;
    }

    /// <summary>
    /// Avalia o processo em primeiro plano e, se necessário, aplica a troca de perfil na MappingEngine.
    /// </summary>
    public bool CheckAndApplyForegroundProcess(string? processName, MappingEngine engine)
    {
        ArgumentNullException.ThrowIfNull(engine);

        if (string.Equals(_lastProcessName, processName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        _lastProcessName = processName;
        var targetProfile = ResolveProfileForProcess(processName);

        if (!ReferenceEquals(engine.CurrentProfile, targetProfile) &&
            !string.Equals(engine.CurrentProfile.Name, targetProfile.Name, StringComparison.Ordinal))
        {
            engine.SetProfile(targetProfile);
            ProfileAutoSwitched?.Invoke(this, (processName ?? "Desconhecido", targetProfile));
            return true;
        }

        return false;
    }
}
