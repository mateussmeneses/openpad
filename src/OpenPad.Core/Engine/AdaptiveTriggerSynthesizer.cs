using System;

namespace OpenPad.Core.Engine;

/// <summary>
/// Operational mode for adaptive force-feedback triggers (DualSense protocol).
/// </summary>
public enum AdaptiveTriggerMode : byte
{
    Off = 0x00,
    Feedback = 0x01,
    Weapon = 0x02,
    Vibration = 0x06,
    Bow = 0x20
}

/// <summary>
/// Configured tactile effect profile to apply to an adaptive analog trigger.
/// </summary>
public record AdaptiveTriggerEffect(
    AdaptiveTriggerMode Mode,
    byte StartPosition = 0,
    byte EndPosition = 9,
    byte Force = 5,
    byte Frequency = 0
);

/// <summary>
/// Synthesizes adaptive trigger resistance curves and command packets
/// compatible with DualSense and dynamic force-feedback controller hardware.
/// </summary>
public class AdaptiveTriggerSynthesizer
{
    public static AdaptiveTriggerEffect Off() =>
        new(AdaptiveTriggerMode.Off);

    public static AdaptiveTriggerEffect Feedback(byte startPos, byte force) =>
        new(AdaptiveTriggerMode.Feedback,
            StartPosition: Math.Clamp(startPos, (byte)0, (byte)9),
            Force: Math.Clamp(force, (byte)0, (byte)8));

    public static AdaptiveTriggerEffect Weapon(byte startPos, byte endPos, byte force) =>
        new(AdaptiveTriggerMode.Weapon,
            StartPosition: Math.Clamp(startPos, (byte)0, (byte)9),
            EndPosition: Math.Clamp(endPos, (byte)0, (byte)9),
            Force: Math.Clamp(force, (byte)0, (byte)8));

    public static AdaptiveTriggerEffect Vibration(byte startPos, byte force, byte frequency) =>
        new(AdaptiveTriggerMode.Vibration,
            StartPosition: Math.Clamp(startPos, (byte)0, (byte)9),
            Force: Math.Clamp(force, (byte)0, (byte)8),
            Frequency: frequency);

    public static AdaptiveTriggerEffect Bow(byte startPos, byte endPos, byte force) =>
        new(AdaptiveTriggerMode.Bow,
            StartPosition: Math.Clamp(startPos, (byte)0, (byte)9),
            EndPosition: Math.Clamp(endPos, (byte)0, (byte)9),
            Force: Math.Clamp(force, (byte)0, (byte)8));

    /// <summary>
    /// Builds a 10-byte raw USB/Bluetooth HID trigger command payload for the hardware controller.
    /// </summary>
    public byte[] BuildTriggerPacket(AdaptiveTriggerEffect effect)
    {
        ArgumentNullException.ThrowIfNull(effect);

        byte[] packet = new byte[10];
        packet[0] = (byte)effect.Mode;

        switch (effect.Mode)
        {
            case AdaptiveTriggerMode.Feedback:
                packet[1] = Math.Clamp(effect.StartPosition, (byte)0, (byte)9);
                packet[2] = Math.Clamp(effect.Force, (byte)0, (byte)8);
                break;

            case AdaptiveTriggerMode.Weapon:
                packet[1] = Math.Clamp(effect.StartPosition, (byte)0, (byte)9);
                packet[2] = Math.Clamp(effect.EndPosition, (byte)0, (byte)9);
                packet[3] = Math.Clamp(effect.Force, (byte)0, (byte)8);
                break;

            case AdaptiveTriggerMode.Vibration:
                packet[1] = Math.Clamp(effect.StartPosition, (byte)0, (byte)9);
                packet[2] = Math.Clamp(effect.Force, (byte)0, (byte)8);
                packet[3] = effect.Frequency;
                break;

            case AdaptiveTriggerMode.Bow:
                packet[1] = Math.Clamp(effect.StartPosition, (byte)0, (byte)9);
                packet[2] = Math.Clamp(effect.EndPosition, (byte)0, (byte)9);
                packet[3] = Math.Clamp(effect.Force, (byte)0, (byte)8);
                break;

            case AdaptiveTriggerMode.Off:
            default:
                // All zeros
                break;
        }

        return packet;
    }
}
