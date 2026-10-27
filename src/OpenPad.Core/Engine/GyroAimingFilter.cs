namespace OpenPad.Core.Engine;

/// <summary>
/// Motion aiming filter converting controller gyroscope angular velocity (deg/s)
/// into smooth mouse displacement (dx, dy) with deadband and sub-pixel accumulation.
/// </summary>
public class GyroAimingFilter
{
    private float _subPixelX;
    private float _subPixelY;

    public float DeadbandDegPerSec { get; set; } = 1.5f;
    public float SensitivityScale { get; set; } = 0.05f;
    public bool InvertPitch { get; set; } = false;
    public bool InvertYaw { get; set; } = false;
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Processes gyroscope angular velocity and returns integer mouse delta (dx, dy).
    /// </summary>
    public (int dx, int dy) Process(float pitchDegPerSec, float yawDegPerSec, float deltaSeconds = 0.00833f)
    {
        if (!IsEnabled) return (0, 0);

        // Calculate angular velocity magnitude
        float magnitude = MathF.Sqrt(pitchDegPerSec * pitchDegPerSec + yawDegPerSec * yawDegPerSec);
        if (magnitude < DeadbandDegPerSec)
        {
            return (0, 0);
        }

        // Apply deadband reduction
        float reductionFactor = (magnitude - DeadbandDegPerSec) / magnitude;
        float filteredYaw = yawDegPerSec * reductionFactor;
        float filteredPitch = pitchDegPerSec * reductionFactor;

        // Apply inversion
        if (InvertYaw) filteredYaw = -filteredYaw;
        if (InvertPitch) filteredPitch = -filteredPitch;

        // Calculate continuous displacement in pixels
        float rawDx = filteredYaw * SensitivityScale * (deltaSeconds * 1000f) + _subPixelX;
        float rawDy = -filteredPitch * SensitivityScale * (deltaSeconds * 1000f) + _subPixelY; // pitch up moves mouse up (-dy in Win32)

        int intDx = (int)MathF.Round(rawDx);
        int intDy = (int)MathF.Round(rawDy);

        // Retain sub-pixel remainder for precision micro-aiming
        _subPixelX = rawDx - intDx;
        _subPixelY = rawDy - intDy;

        return (intDx, intDy);
    }

    public void Reset()
    {
        _subPixelX = 0f;
        _subPixelY = 0f;
    }
}
