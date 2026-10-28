namespace OpenPad.Core.Engine;

/// <summary>
/// Implements the Flick-Stick aiming algorithm.
/// Snapping the right stick in any direction instantly begins a rapid, smooth horizontal camera turn
/// to face that exact orientation (360-degree coverage), while subsequent sweeps allow continuous rotation.
/// Vertical aiming is intentionally decoupled and delegated to gyro or alternate controls.
/// </summary>
public class FlickStickEngine
{
    private enum FlickState
    {
        Neutral,
        Flicking,
        Rotating
    }

    private FlickState _state = FlickState.Neutral;
    private float _lastAngleRad;
    private float _remainingTurnRad;
    private float _subPixelRemainder;

    public float FlickThreshold { get; set; } = 0.75f;
    public float InnerDeadzone { get; set; } = 0.20f;
    public float SmoothTimeSeconds { get; set; } = 0.08f;
    public float PixelsPerFull360 { get; set; } = 3600.0f;
    public bool InvertTurn { get; set; } = false;

    public bool IsEngaged => _state != FlickState.Neutral;

    /// <summary>
    /// Resets the engine state to neutral.
    /// </summary>
    public void Reset()
    {
        _state = FlickState.Neutral;
        _lastAngleRad = 0f;
        _remainingTurnRad = 0f;
        _subPixelRemainder = 0f;
    }

    /// <summary>
    /// Computes horizontal mouse delta (dx) based on right stick (X, Y) deflection.
    /// </summary>
    public int ComputeTurnDelta(float rawX, float rawY, float deltaTimeSeconds = 0.00833f)
    {
        float magnitude = MathF.Sqrt(rawX * rawX + rawY * rawY);

        // When stick is near center, return to neutral
        if (magnitude < InnerDeadzone)
        {
            _state = FlickState.Neutral;
            return ProcessSmoothing(deltaTimeSeconds);
        }

        float currentAngleRad = MathF.Atan2(rawX, -rawY); // 0 at Up (forward), +PI/2 at Right

        if (_state == FlickState.Neutral)
        {
            if (magnitude >= FlickThreshold)
            {
                // First flick stroke: initiate rotation to face the exact angle
                _state = FlickState.Flicking;
                _remainingTurnRad += NormalizeAngle(currentAngleRad);
                _lastAngleRad = currentAngleRad;
            }
        }
        else
        {
            // Already engaged: continuous rotation tracks stick sweep
            _state = FlickState.Rotating;
            float deltaAngle = NormalizeAngle(currentAngleRad - _lastAngleRad);
            _remainingTurnRad += deltaAngle;
            _lastAngleRad = currentAngleRad;
        }

        return ProcessSmoothing(deltaTimeSeconds);
    }

    private int ProcessSmoothing(float deltaTimeSeconds)
    {
        if (MathF.Abs(_remainingTurnRad) < 0.0001f)
        {
            _remainingTurnRad = 0f;
            return 0;
        }

        float safeSmoothTime = Math.Max(0.01f, SmoothTimeSeconds);
        float stepFraction = Math.Min(1.0f, deltaTimeSeconds / safeSmoothTime);
        float radTurnThisFrame = _remainingTurnRad * stepFraction;
        _remainingTurnRad -= radTurnThisFrame;

        // Convert radians to pixels: (rad / (2 * PI)) * PixelsPerFull360
        float pixels = (radTurnThisFrame / (2.0f * MathF.PI)) * PixelsPerFull360 + _subPixelRemainder;
        if (InvertTurn) pixels = -pixels;

        int intDx = (int)MathF.Round(pixels);
        _subPixelRemainder = pixels - intDx;

        return intDx;
    }

    private static float NormalizeAngle(float angleRad)
    {
        while (angleRad > MathF.PI) angleRad -= 2.0f * MathF.PI;
        while (angleRad < -MathF.PI) angleRad += 2.0f * MathF.PI;
        return angleRad;
    }
}
