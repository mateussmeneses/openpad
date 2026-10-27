using System.Text.Json.Serialization;

namespace OpenPad.Core.Models;

public enum StickMouseMode
{
    None = 0,
    RightStick = 1,
    LeftStick = 2
}

public enum StickCurveType
{
    Quadratic = 0,
    Linear = 1,
    Aggressive = 2,
    Smooth = 3
}

public class MappingProfile
{
    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; set; } = 1;

    [JsonPropertyName("name")]
    public string Name { get; set; } = "Default";

    [JsonPropertyName("description")]
    public string? Description { get; set; } = "Default standard gamepad mapping";

    [JsonPropertyName("associatedProcesses")]
    public List<string> AssociatedProcessNames { get; set; } = new();

    [JsonPropertyName("hapticFeedbackEnabled")]
    public bool HapticFeedbackEnabled { get; set; } = true;

    [JsonPropertyName("stickCurveType")]
    public StickCurveType StickCurveType { get; set; } = StickCurveType.Quadratic;

    [JsonPropertyName("buttonBindings")]
    public Dictionary<GamepadButton, InputAction> ButtonBindings { get; set; } = new();

    [JsonPropertyName("shiftModifier")]
    public GamepadButton? ShiftModifier { get; set; } = null;

    [JsonPropertyName("shiftBindings")]
    public Dictionary<GamepadButton, InputAction> ShiftBindings { get; set; } = new();

    [JsonPropertyName("triggerBindings")]
    public Dictionary<GamepadAxis, InputAction> TriggerBindings { get; set; } = new();

    [JsonPropertyName("stickDeadzone")]
    public float StickDeadzone { get; set; } = 0.15f;

    [JsonPropertyName("triggerThreshold")]
    public float TriggerThreshold { get; set; } = 0.30f;

    [JsonPropertyName("stickMouseMode")]
    public StickMouseMode StickMouseMode { get; set; } = StickMouseMode.RightStick;

    [JsonPropertyName("mouseSensitivity")]
    public float MouseSensitivity { get; set; } = 15.0f;

    public static MappingProfile CreateDefault(string name = "Padrão")
    {
        var profile = new MappingProfile
        {
            SchemaVersion = 1,
            Name = name,
            Description = "Mapeamento padrão para jogos de teclado e mouse",
            StickDeadzone = 0.15f,
            TriggerThreshold = 0.30f,
            StickMouseMode = StickMouseMode.RightStick,
            MouseSensitivity = 15.0f
        };

        // Standard default bindings
        profile.ButtonBindings[GamepadButton.A] = InputAction.FromKey(VirtualKey.Space);           // Jump
        profile.ButtonBindings[GamepadButton.B] = InputAction.FromKey(VirtualKey.C);               // Crouch / Slide
        profile.ButtonBindings[GamepadButton.X] = InputAction.FromKey(VirtualKey.R);               // Reload / Interact
        profile.ButtonBindings[GamepadButton.Y] = InputAction.FromKey(VirtualKey.E);               // Use
        profile.ButtonBindings[GamepadButton.LeftShoulder] = InputAction.FromKey(VirtualKey.LeftShift); // Sprint
        profile.ButtonBindings[GamepadButton.RightShoulder] = InputAction.FromMouse(MouseButton.Left); // Click
        profile.ButtonBindings[GamepadButton.Start] = InputAction.FromKey(VirtualKey.Escape);     // Pause / Menu
        profile.ButtonBindings[GamepadButton.Back] = InputAction.FromKey(VirtualKey.Tab);          // Scoreboard / Map
        profile.ButtonBindings[GamepadButton.DpadUp] = InputAction.FromKey(VirtualKey.D1);        // Weapon 1
        profile.ButtonBindings[GamepadButton.DpadDown] = InputAction.FromKey(VirtualKey.D2);      // Weapon 2
        profile.ButtonBindings[GamepadButton.DpadLeft] = InputAction.FromKey(VirtualKey.Q);       // Quick item
        profile.ButtonBindings[GamepadButton.DpadRight] = InputAction.FromKey(VirtualKey.F);      // Flashlight / Ability
        profile.ButtonBindings[GamepadButton.LeftStick] = InputAction.FromKey(VirtualKey.LeftControl); // Duck / Sprint
        profile.ButtonBindings[GamepadButton.RightStick] = InputAction.FromMouse(MouseButton.Right); // Aim / Secondary

        // Standard triggers: RT -> Mouse Left Click, LT -> Mouse Right Click
        profile.TriggerBindings[GamepadAxis.TriggerRight] = InputAction.FromMouse(MouseButton.Left);
        profile.TriggerBindings[GamepadAxis.TriggerLeft] = InputAction.FromMouse(MouseButton.Right);

        return profile;
    }
}
