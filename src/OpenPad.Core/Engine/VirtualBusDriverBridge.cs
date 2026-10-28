using System;
using OpenPad.Core.Models;

namespace OpenPad.Core.Engine;

/// <summary>
/// Type of virtual target controller emulated in the operating system.
/// </summary>
public enum VirtualControllerType
{
    Xbox360,
    DualShock4
}

/// <summary>
/// Standardized virtual controller report matching OS driver specifications.
/// </summary>
public record VirtualGamepadReport(
    ushort Buttons,
    byte LeftTrigger,
    byte RightTrigger,
    short ThumbLX,
    short ThumbLY,
    short ThumbRX,
    short ThumbRY
);

/// <summary>
/// Virtual force-feedback rumble event received from the operating system / game.
/// </summary>
public record VirtualRumbleEvent(
    byte LargeMotor,
    byte SmallMotor
);

/// <summary>
/// Emulation bridge for virtual bus drivers (e.g. ViGEmBus / Virtual HID).
/// Translates OpenPad controller snapshots into system-level virtual gamepad devices
/// recognized by games without keyboard/mouse support.
/// </summary>
public class VirtualBusDriverBridge
{
    public bool IsConnected { get; private set; }
    public VirtualControllerType? ActiveType { get; private set; }
    public VirtualGamepadReport LastReport { get; private set; } = new(0, 0, 0, 0, 0, 0, 0);

    public event Action<VirtualRumbleEvent>? OnRumbleReceived;

    /// <summary>
    /// Plugs in a virtual gamepad to the virtual bus.
    /// </summary>
    public void Connect(VirtualControllerType type)
    {
        if (IsConnected)
        {
            Disconnect();
        }

        ActiveType = type;
        IsConnected = true;
    }

    /// <summary>
    /// Unplugs the virtual gamepad from the system.
    /// </summary>
    public void Disconnect()
    {
        IsConnected = false;
        ActiveType = null;
        LastReport = new(0, 0, 0, 0, 0, 0, 0);
    }

    /// <summary>
    /// Converts a normalized OpenPad ControllerSnapshot into a virtual driver report and submits it.
    /// </summary>
    public VirtualGamepadReport SubmitSnapshot(ControllerSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (!IsConnected)
        {
            throw new InvalidOperationException("Cannot submit report: Virtual gamepad is not connected.");
        }

        ushort buttons = 0;
        if (snapshot.IsButtonPressed(GamepadButton.A)) buttons |= 0x1000;
        if (snapshot.IsButtonPressed(GamepadButton.B)) buttons |= 0x2000;
        if (snapshot.IsButtonPressed(GamepadButton.X)) buttons |= 0x4000;
        if (snapshot.IsButtonPressed(GamepadButton.Y)) buttons |= 0x8000;
        if (snapshot.IsButtonPressed(GamepadButton.DpadUp)) buttons |= 0x0001;
        if (snapshot.IsButtonPressed(GamepadButton.DpadDown)) buttons |= 0x0002;
        if (snapshot.IsButtonPressed(GamepadButton.DpadLeft)) buttons |= 0x0004;
        if (snapshot.IsButtonPressed(GamepadButton.DpadRight)) buttons |= 0x0008;
        if (snapshot.IsButtonPressed(GamepadButton.Start)) buttons |= 0x0010;
        if (snapshot.IsButtonPressed(GamepadButton.Back)) buttons |= 0x0020;
        if (snapshot.IsButtonPressed(GamepadButton.LeftStick)) buttons |= 0x0040;
        if (snapshot.IsButtonPressed(GamepadButton.RightStick)) buttons |= 0x0080;
        if (snapshot.IsButtonPressed(GamepadButton.LeftShoulder)) buttons |= 0x0100;
        if (snapshot.IsButtonPressed(GamepadButton.RightShoulder)) buttons |= 0x0200;
        if (snapshot.IsButtonPressed(GamepadButton.Guide)) buttons |= 0x0400;

        byte leftTrigger = (byte)Math.Clamp((int)(snapshot.GetAxis(GamepadAxis.TriggerLeft) * 255f), 0, 255);
        byte rightTrigger = (byte)Math.Clamp((int)(snapshot.GetAxis(GamepadAxis.TriggerRight) * 255f), 0, 255);

        short thumbLX = NormalizeAxis(snapshot.GetAxis(GamepadAxis.LeftX));
        short thumbLY = NormalizeAxis(snapshot.GetAxis(GamepadAxis.LeftY));
        short thumbRX = NormalizeAxis(snapshot.GetAxis(GamepadAxis.RightX));
        short thumbRY = NormalizeAxis(snapshot.GetAxis(GamepadAxis.RightY));

        var report = new VirtualGamepadReport(
            buttons,
            leftTrigger,
            rightTrigger,
            thumbLX,
            thumbLY,
            thumbRX,
            thumbRY
        );

        LastReport = report;
        return report;
    }

    /// <summary>
    /// Simulates feedback rumble sent from the game to the virtual controller.
    /// </summary>
    public void DispatchRumbleFromGame(byte largeMotor, byte smallMotor)
    {
        if (!IsConnected) return;
        OnRumbleReceived?.Invoke(new VirtualRumbleEvent(largeMotor, smallMotor));
    }

    private static short NormalizeAxis(float val)
    {
        float clamped = Math.Clamp(val, -1f, 1f);
        if (clamped >= 0f)
        {
            return (short)Math.Round(clamped * 32767f);
        }
        return (short)Math.Round(clamped * 32768f);
    }
}
