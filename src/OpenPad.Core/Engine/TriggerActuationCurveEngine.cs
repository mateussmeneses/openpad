using System;

namespace OpenPad.Core.Engine;

/// <summary>
/// Mathematical response profile applied to analog triggers.
/// </summary>
public enum TriggerCurveType
{
    Linear,
    InstantDigitalClick,
    Aggressive,
    Smooth,
    SteppedDualZone
}

/// <summary>
/// Tuning parameters for analog trigger deadzones and response curve morphing.
/// </summary>
public record TriggerCurveConfig
{
    public TriggerCurveType Type { get; init; } = TriggerCurveType.Linear;

    /// <summary>
    /// Initial trigger deflection ignored as pre-travel slack [0.0 - 0.4].
    /// </summary>
    public float PreTravelDeadzone { get; init; } = 0.05f;

    /// <summary>
    /// Trigger pull point that reaches 100% output before physical bottom-out [0.6 - 1.0].
    /// </summary>
    public float EndStopClamp { get; init; } = 0.95f;

    /// <summary>
    /// Actuation sensitivity threshold for esports hairpin instant digital clicks [0.01 - 0.3].
    /// </summary>
    public float HairpinThreshold { get; init; } = 0.08f;

    /// <summary>
    /// Deflection split point for stepped dual-zone triggers [0.2 - 0.8].
    /// </summary>
    public float StepThreshold { get; init; } = 0.65f;
}

/// <summary>
/// Custom trigger actuation curve and response processor.
/// Morphs analog triggers into esports hairpin hair-triggers, parabolic response curves,
/// or stepped dual-zone stages.
/// </summary>
public class TriggerActuationCurveEngine
{
    /// <summary>
    /// Processes a raw analog trigger pull [0.0 .. 1.0] and applies deadzone filtering and response morphing.
    /// </summary>
    public float ProcessTrigger(float rawPull, TriggerCurveConfig? config = null)
    {
        config ??= new TriggerCurveConfig();

        float clamped = Math.Clamp(rawPull, 0f, 1f);

        // Pre-travel deadzone
        if (clamped <= config.PreTravelDeadzone)
        {
            return 0f;
        }

        // Esports Hairpin instant digital click
        if (config.Type == TriggerCurveType.InstantDigitalClick)
        {
            return clamped >= config.HairpinThreshold ? 1.0f : 0.0f;
        }

        // Rescale within active travel range [PreTravel .. EndStop]
        float pre = Math.Clamp(config.PreTravelDeadzone, 0f, 0.4f);
        float end = Math.Clamp(config.EndStopClamp, pre + 0.05f, 1.0f);
        float normalized = Math.Clamp((clamped - pre) / (end - pre), 0f, 1f);

        float output = config.Type switch
        {
            TriggerCurveType.Aggressive => MathF.Sqrt(normalized),
            TriggerCurveType.Smooth => normalized * normalized * (3f - 2f * normalized),
            TriggerCurveType.SteppedDualZone => ProcessSteppedDualZone(normalized, config.StepThreshold),
            TriggerCurveType.Linear => normalized,
            _ => normalized
        };

        return Math.Clamp(output, 0f, 1f);
    }

    private static float ProcessSteppedDualZone(float normalized, float stepThreshold)
    {
        float step = Math.Clamp(stepThreshold, 0.2f, 0.8f);
        if (normalized < step)
        {
            return 0.5f * (normalized / step);
        }
        return 0.5f + 0.5f * ((normalized - step) / (1f - step));
    }
}
