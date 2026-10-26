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

    public static InputAction None() => new() { Type = ActionType.None };

    public static InputAction FromKey(VirtualKey key) => new()
    {
        Type = ActionType.Key,
        Key = key
    };

    public static InputAction FromMouse(MouseButton button) => new()
    {
        Type = ActionType.MouseButton,
        MouseButton = button
    };

    public override string ToString() => Type switch
    {
        ActionType.Key => $"Key: {Key}",
        ActionType.MouseButton => $"Mouse: {MouseButton}",
        _ => "None"
    };
}
