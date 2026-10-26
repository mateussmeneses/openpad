using System.Text.Json;
using System.Text.Json.Serialization;
using OpenPad.Core.Interfaces;
using OpenPad.Core.Models;

namespace OpenPad.Core.Storage;

public class JsonProfileStore : IProfileStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public MappingProfile LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Arquivo de perfil não encontrado: {filePath}");
        }

        var json = File.ReadAllText(filePath);
        return LoadFromString(json);
    }

    public void SaveToFile(string filePath, MappingProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        if (!ValidateProfile(profile, out var errors))
        {
            throw new InvalidOperationException($"Perfil inválido para gravação: {string.Join("; ", errors)}");
        }

        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var json = SerializeToString(profile);
        File.WriteAllText(filePath, json);
    }

    public MappingProfile LoadFromString(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("Conteúdo JSON vazio.", nameof(json));
        }

        var profile = JsonSerializer.Deserialize<MappingProfile>(json, JsonOptions)
            ?? throw new JsonException("Falha ao desserializar perfil a partir do JSON.");

        if (!ValidateProfile(profile, out var errors))
        {
            throw new InvalidOperationException($"Perfil carregado contém erros: {string.Join("; ", errors)}");
        }

        return profile;
    }

    public string SerializeToString(MappingProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        return JsonSerializer.Serialize(profile, JsonOptions);
    }

    public bool ValidateProfile(MappingProfile profile, out List<string> errors)
    {
        errors = new List<string>();

        if (profile == null)
        {
            errors.Add("Perfil não pode ser nulo.");
            return false;
        }

        if (profile.SchemaVersion <= 0)
        {
            errors.Add($"Versão de esquema inválida: {profile.SchemaVersion}. Deve ser >= 1.");
        }

        if (string.IsNullOrWhiteSpace(profile.Name))
        {
            errors.Add("O nome do perfil não pode ser vazio.");
        }

        if (profile.StickDeadzone < 0.0f || profile.StickDeadzone > 0.9f)
        {
            errors.Add("A zona morta dos analógicos deve estar entre 0.0 e 0.9.");
        }

        if (profile.TriggerThreshold < 0.05f || profile.TriggerThreshold > 0.95f)
        {
            errors.Add("O limiar dos gatilhos deve estar entre 0.05 e 0.95.");
        }

        return errors.Count == 0;
    }
}
