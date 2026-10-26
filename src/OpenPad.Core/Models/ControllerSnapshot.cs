namespace OpenPad.Core.Models;

public class ControllerSnapshot
{
    public string DeviceId { get; }
    public string DeviceName { get; }
    public bool IsConnected { get; }
    public DateTime Timestamp { get; }
    public IReadOnlyDictionary<GamepadButton, bool> Buttons { get; }
    public IReadOnlyDictionary<GamepadAxis, float> Axes { get; }

    public ControllerSnapshot(
        string deviceId,
        string deviceName,
        bool isConnected,
        DateTime timestamp,
        IDictionary<GamepadButton, bool> buttons,
        IDictionary<GamepadAxis, float> axes)
    {
        DeviceId = deviceId ?? string.Empty;
        DeviceName = deviceName ?? string.Empty;
        IsConnected = isConnected;
        Timestamp = timestamp;
        Buttons = new Dictionary<GamepadButton, bool>(buttons);
        Axes = new Dictionary<GamepadAxis, float>(axes);
    }

    public bool IsButtonPressed(GamepadButton button)
    {
        return Buttons.TryGetValue(button, out var pressed) && pressed;
    }

    public float GetAxis(GamepadAxis axis)
    {
        return Axes.TryGetValue(axis, out var value) ? value : 0.0f;
    }

    public static ControllerSnapshot Disconnected(string deviceId = "", string deviceName = "")
    {
        var buttons = new Dictionary<GamepadButton, bool>();
        foreach (GamepadButton b in Enum.GetValues<GamepadButton>())
        {
            buttons[b] = false;
        }

        var axes = new Dictionary<GamepadAxis, float>();
        foreach (GamepadAxis a in Enum.GetValues<GamepadAxis>())
        {
            axes[a] = 0.0f;
        }

        return new ControllerSnapshot(deviceId, deviceName, false, DateTime.UtcNow, buttons, axes);
    }
}
