using System;

namespace OpenPad.Core.Engine;

/// <summary>
/// Shape of the inner deadzone to filter stick drift and involuntary movements.
/// </summary>
public enum DeadzoneShape
{
    /// <summary>
    /// Circular deadzone calculated using the Euclidean vector magnitude. Recommended for FPS / twin-stick shooters.
    /// </summary>
    Radial,

    /// <summary>
    /// Independent deadzones along horizontal and vertical axes. Recommended for racing, flight simulators, or platformers.
    /// </summary>
    Axial,

    /// <summary>
    /// Chebyshev square deadzone based on max(|X|, |Y|).
    /// </summary>
    Square
}

/// <summary>
/// Desired output boundary shape for the stick deflection.
/// </summary>
public enum OutputShape
{
    /// <summary>
    /// Clamped to standard circular unit boundary [magnitude &lt;= 1.0].
    /// </summary>
    Circle,

    /// <summary>
    /// Expanded to square boundary so diagonals can reach full (1.0, 1.0), optimal for retro 8-way games.
    /// </summary>
    Square
}

/// <summary>
/// Configuration parameters for stick deadzone and shape morphing.
/// </summary>
public record DeadzoneConfig
{
    /// <summary>
    /// Inner deadzone threshold [0.0 - 0.9]. Deflections below this are discarded as drift.
    /// </summary>
    public float InnerDeadzone { get; init; } = 0.15f;

    /// <summary>
    /// Outer deadzone threshold [0.1 - 1.0]. Deflections above this reach maximum output.
    /// </summary>
    public float OuterDeadzone { get; init; } = 0.95f;

    /// <summary>
    /// Anti-deadzone boost [0.0 - 0.9]. Offsets the output immediately when leaving the inner deadzone,
    /// overcoming hardcoded in-game deadzones.
    /// </summary>
    public float AntiDeadzone { get; init; } = 0.0f;

    /// <summary>
    /// Deadzone filtering shape.
    /// </summary>
    public DeadzoneShape Shape { get; init; } = DeadzoneShape.Radial;

    /// <summary>
    /// Target output geometric shape.
    /// </summary>
    public OutputShape OutputShape { get; init; } = OutputShape.Circle;
}

/// <summary>
/// High-performance deadzone filter and geometric shape morpher for analog gamepad sticks.
/// Supports radial, axial, and square deadzones with dynamic anti-deadzone injection and square output projection.
/// </summary>
public class DeadzoneShapeMorpher
{
    /// <summary>
    /// Processes raw analog stick inputs (-1.0 to +1.0) and applies deadzone filtering,
    /// anti-deadzone compensation, and shape morphing.
    /// </summary>
    /// <param name="rawX">Raw horizontal axis deflection [-1.0 .. 1.0].</param>
    /// <param name="rawY">Raw vertical axis deflection [-1.0 .. 1.0].</param>
    /// <param name="config">Deadzone configuration parameters.</param>
    /// <returns>Processed (X, Y) stick deflection tuple [-1.0 .. 1.0].</returns>
    public (float X, float Y) ProcessStick(float rawX, float rawY, DeadzoneConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        float clampedX = Math.Clamp(rawX, -1f, 1f);
        float clampedY = Math.Clamp(rawY, -1f, 1f);

        float inner = Math.Clamp(config.InnerDeadzone, 0f, 0.95f);
        float outer = Math.Clamp(config.OuterDeadzone, inner + 0.01f, 1.0f);
        float anti = Math.Clamp(config.AntiDeadzone, 0f, 0.95f);

        float outX;
        float outY;

        switch (config.Shape)
        {
            case DeadzoneShape.Axial:
                outX = ProcessSingleAxis(clampedX, inner, outer, anti);
                outY = ProcessSingleAxis(clampedY, inner, outer, anti);
                break;

            case DeadzoneShape.Square:
                float maxAxis = Math.Max(Math.Abs(clampedX), Math.Abs(clampedY));
                if (maxAxis <= inner)
                {
                    return (0f, 0f);
                }
                float normalizedSquare = Math.Clamp((maxAxis - inner) / (outer - inner), 0f, 1f);
                float boostedSquare = anti + (1f - anti) * normalizedSquare;
                float squareRatio = boostedSquare / maxAxis;
                outX = Math.Clamp(clampedX * squareRatio, -1f, 1f);
                outY = Math.Clamp(clampedY * squareRatio, -1f, 1f);
                break;

            case DeadzoneShape.Radial:
            default:
                float magnitude = MathF.Sqrt(clampedX * clampedX + clampedY * clampedY);
                if (magnitude <= inner || magnitude <= 0.00001f)
                {
                    return (0f, 0f);
                }

                float normalizedMag = Math.Clamp((magnitude - inner) / (outer - inner), 0f, 1f);
                float boostedMag = anti + (1f - anti) * normalizedMag;

                float dirX = clampedX / magnitude;
                float dirY = clampedY / magnitude;

                if (config.OutputShape == OutputShape.Square)
                {
                    float maxDir = Math.Max(Math.Abs(dirX), Math.Abs(dirY));
                    float squareFactor = maxDir > 0.0001f ? (1f / maxDir) : 1f;
                    float scaledMag = boostedMag * squareFactor;
                    outX = Math.Clamp(dirX * scaledMag, -1f, 1f);
                    outY = Math.Clamp(dirY * scaledMag, -1f, 1f);
                }
                else
                {
                    outX = Math.Clamp(dirX * boostedMag, -1f, 1f);
                    outY = Math.Clamp(dirY * boostedMag, -1f, 1f);
                }
                break;
        }

        return (outX, outY);
    }

    private static float ProcessSingleAxis(float value, float inner, float outer, float anti)
    {
        float abs = Math.Abs(value);
        if (abs <= inner)
        {
            return 0f;
        }

        float normalized = Math.Clamp((abs - inner) / (outer - inner), 0f, 1f);
        float boosted = anti + (1f - anti) * normalized;
        return Math.Sign(value) * Math.Clamp(boosted, 0f, 1f);
    }
}
