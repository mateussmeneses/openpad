using OpenPad.Core.Models;

namespace OpenPad.Core.Interfaces;

public interface IProfileStore
{
    MappingProfile LoadFromFile(string filePath);
    void SaveToFile(string filePath, MappingProfile profile);
    MappingProfile LoadFromString(string json);
    string SerializeToString(MappingProfile profile);
    bool ValidateProfile(MappingProfile profile, out List<string> errors);
}
