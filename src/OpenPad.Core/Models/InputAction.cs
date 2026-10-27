using System.Text.Json.Serialization;

namespace OpenPad.Core.Models;

public record InputAction
{
    [JsonPropertyName("type")]
    public ActionType Type { get; init; } = ActionType.None;

    [JsonPropertyName("key")]
    public VirtualKey? Key { get; init; }

    [JsonPropertyName("mouseButton")]
    public MouseButton? MouseButton { get; init; }

    [JsonPropertyName("isTurbo")]
    public bool IsTurbo { get; init; } = false;

    public static InputAction None() => new() { Type = ActionType.None };

    public static InputAction FromKey(VirtualKey key, bool isTurbo = false) => new()
    {
        Type = ActionType.Key,
        Key = key,
        IsTurbo = isTurbo
    };

    public static InputAction FromMouse(MouseButton button, bool isTurbo = false) => new()
    {
        Type = ActionType.MouseButton,
        MouseButton = button,
        IsTurbo = isTurbo
    };

    public override string ToString() => Type switch
    {
        ActionType.Key => IsTurbo ? $"Key: {Key} (Turbo)" : $"Key: {Key}",
        ActionType.MouseButton => IsTurbo ? $"Mouse: {MouseButton} (Turbo)" : $"Mouse: {MouseButton}",
        _ => "None"
    };
}
