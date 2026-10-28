namespace OpenPad.Core.Engine;

/// <summary>
/// Acceleration curve types supported by the mouse acceleration engine.
/// </summary>
public enum MouseAccelerationCurve
{
    None = 0,
    Linear = 1,
    Power = 2,
    Dynamic = 3
}

/// <summary>
/// Advanced mouse acceleration and precision snapping engine.
/// Translates analog stick deflection into responsive, natural mouse movement
/// with ballistic acceleration, sub-pixel accuracy, sniper precision mode,
/// and axial snapping for UI and shooter navigation.
/// </summary>
public class MouseAccelerationEngine
{
    private float _subPixelX;
    private float _subPixelY;
    private float _dynamicEngagedTime;

    public MouseAccelerationCurve Curve { get; set; } = MouseAccelerationCurve.Power;
    public float BaseSensitivity { get; set; } = 15.0f;
    public float PowerExponent { get; set; } = 1.8f;
    public float DynamicThreshold { get; set; } = 0.75f;
    public float DynamicRampTimeSeconds { get; set; } = 0.6f;
    public float MaxDynamicMultiplier { get; set; } = 2.5f;

    public bool IsSniperActive { get; set; } = false;
    public float SniperMultiplier { get; set; } = 0.25f;

    public bool EnableAxisSnapping { get; set; } = false;
    public float SnapThresholdAngleDegrees { get; set; } = 7.0f;

    public bool InvertX { get; set; } = false;
    public bool InvertY { get; set; } = false;

    /// <summary>
    /// Resets sub-pixel accumulators and dynamic timing.
    /// </summary>
    public void Reset()
    {
        _subPixelX = 0f;
        _subPixelY = 0f;
        _dynamicEngagedTime = 0f;
    }

    /// <summary>
    /// Processes normalized stick inputs (-1.0 to 1.0) and returns integer mouse delta (dx, dy).
    /// </summary>
    public (int dx, int dy) ComputeMouseDelta(float rawX, float rawY, float deltaTimeSeconds = 0.00833f)
    {
        float mag = MathF.Sqrt(rawX * rawX + rawY * rawY);
        if (mag < 0.0001f)
        {
            _dynamicEngagedTime = 0f;
            return (0, 0);
        }

        // Clamp magnitude to 1.0
        float clampedMag = Math.Min(1.0f, mag);
        float normX = rawX / mag * clampedMag;
        float normY = rawY / mag * clampedMag;

        // Apply Axis Snapping if enabled
        if (EnableAxisSnapping)
        {
            (normX, normY) = ApplyAxisSnapping(normX, normY, clampedMag);
        }

        // Calculate Curve Multiplier
        float speedMultiplier = 1.0f;
        switch (Curve)
        {
            case MouseAccelerationCurve.None:
                speedMultiplier = 1.0f;
                break;

            case MouseAccelerationCurve.Linear:
                speedMultiplier = clampedMag;
                break;

            case MouseAccelerationCurve.Power:
                speedMultiplier = MathF.Pow(clampedMag, PowerExponent - 1.0f);
                break;

            case MouseAccelerationCurve.Dynamic:
                if (clampedMag >= DynamicThreshold)
                {
                    _dynamicEngagedTime += deltaTimeSeconds;
                    float progress = Math.Clamp(_dynamicEngagedTime / Math.Max(0.01f, DynamicRampTimeSeconds), 0f, 1f);
                    speedMultiplier = 1.0f + progress * (MaxDynamicMultiplier - 1.0f);
                }
                else
                {
                    _dynamicEngagedTime = 0f;
                    speedMultiplier = clampedMag;
                }
                break;
        }

        // Apply Sniper slowdown if active
        if (IsSniperActive)
        {
            speedMultiplier *= SniperMultiplier;
        }

        // Apply Inversion
        if (InvertX) normX = -normX;
        if (InvertY) normY = -normY;

        // Compute continuous displacement with sub-pixel accumulation
        float continuousDx = normX * BaseSensitivity * speedMultiplier * (deltaTimeSeconds * 120.0f) + _subPixelX;
        float continuousDy = normY * BaseSensitivity * speedMultiplier * (deltaTimeSeconds * 120.0f) + _subPixelY;

        int intDx = (int)MathF.Round(continuousDx);
        int intDy = (int)MathF.Round(continuousDy);

        _subPixelX = continuousDx - intDx;
        _subPixelY = continuousDy - intDy;

        return (intDx, intDy);
    }

    private (float x, float y) ApplyAxisSnapping(float x, float y, float magnitude)
    {
        // Angle in degrees [0, 360)
        float angleRad = MathF.Atan2(y, x);
        float angleDeg = angleRad * (180.0f / MathF.PI);
        if (angleDeg < 0) angleDeg += 360.0f;

        // Check horizontal (0 deg or 180 deg)
        if (MathF.Abs(angleDeg - 0f) <= SnapThresholdAngleDegrees ||
            MathF.Abs(angleDeg - 360f) <= SnapThresholdAngleDegrees)
        {
            return (magnitude, 0f);
        }
        if (MathF.Abs(angleDeg - 180f) <= SnapThresholdAngleDegrees)
        {
            return (-magnitude, 0f);
        }

        // Check vertical (90 deg or 270 deg)
        if (MathF.Abs(angleDeg - 90f) <= SnapThresholdAngleDegrees)
        {
            return (0f, magnitude);
        }
        if (MathF.Abs(angleDeg - 270f) <= SnapThresholdAngleDegrees)
        {
            return (0f, -magnitude);
        }

        return (x, y);
    }
}
