using System;

namespace OpenPad.Core.Engine;

/// <summary>
/// Configuration parameters for weapon recoil mitigation.
/// </summary>
public record RecoilPatternConfig
{
    /// <summary>
    /// Vertical counter-pull magnitude (downward pull to counter muzzle climb).
    /// </summary>
    public float VerticalStrength { get; init; } = 3.5f;

    /// <summary>
    /// Horizontal counter-pull magnitude (left/right compensation).
    /// </summary>
    public float HorizontalStrength { get; init; } = 0.0f;

    /// <summary>
    /// Delay before recoil compensation engages (in milliseconds), allowing single-tap precision.
    /// </summary>
    public double RampUpDelayMs { get; init; } = 50.0;

    /// <summary>
    /// Randomized humanization jitter [0.0 - 0.5] to simulate organic human hand drift.
    /// </summary>
    public float HumanizationJitter { get; init; } = 0.10f;

    /// <summary>
    /// Maximum allowed delta offset per tick to prevent abrupt camera jerks.
    /// </summary>
    public float MaxDeltaPerTick { get; init; } = 20.0f;
}

/// <summary>
/// Dynamic recoil mitigation and anti-spread compensator for competitive shooters.
/// Injects controlled counter-offsets during sustained fire with micro-jitter humanization.
/// </summary>
public class RecoilCompensator
{
    private readonly Random _random;

    public RecoilCompensator(int? seed = null)
    {
        _random = seed.HasValue ? new Random(seed.Value) : Random.Shared;
    }

    /// <summary>
    /// Computes mouse/camera deflection offsets to mitigate weapon recoil.
    /// </summary>
    /// <param name="isShooting">Whether the firing button/trigger is currently held.</param>
    /// <param name="elapsedShootingMs">Duration in milliseconds of the current continuous burst.</param>
    /// <param name="config">Weapon recoil configuration parameters.</param>
    /// <returns>Relative (DeltaX, DeltaY) displacement to apply to the aiming cursor.</returns>
    public (float DeltaX, float DeltaY) ProcessTick(
        bool isShooting,
        double elapsedShootingMs,
        RecoilPatternConfig? config = null)
    {
        if (!isShooting || elapsedShootingMs <= 0.0)
        {
            return (0f, 0f);
        }

        config ??= new RecoilPatternConfig();

        if (elapsedShootingMs < config.RampUpDelayMs)
        {
            return (0f, 0f);
        }

        // Calculate organic jitter multiplier
        float jitterFactor = 1.0f;
        if (config.HumanizationJitter > 0.001f)
        {
            float jitterSpread = Math.Clamp(config.HumanizationJitter, 0f, 0.5f);
            float randomOffset = (float)(_random.NextDouble() * 2.0 - 1.0) * jitterSpread;
            jitterFactor += randomOffset;
        }

        float outY = Math.Clamp(config.VerticalStrength * jitterFactor, -config.MaxDeltaPerTick, config.MaxDeltaPerTick);
        float outX = Math.Clamp(config.HorizontalStrength * jitterFactor, -config.MaxDeltaPerTick, config.MaxDeltaPerTick);

        return (outX, outY);
    }
}
