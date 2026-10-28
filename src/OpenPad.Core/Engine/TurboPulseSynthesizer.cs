namespace OpenPad.Core.Engine;

/// <summary>
/// Result state returned on every tick of the turbo pulse synthesizer.
/// </summary>
public record TurboPulseState(
    bool IsEngaged,
    bool JustFired,
    bool BurstCompleted,
    int PulseCount
);

/// <summary>
/// Synthesizes high-precision rapid fire / turbo pulses for accessibility,
/// shoot-'em-ups, and rapid action emulation.
/// Supports configurable repetition frequency (Hz), duty cycle, and burst limits.
/// </summary>
public class TurboPulseSynthesizer
{
    private double _phaseTime;
    private int _completedPulses;
    private bool _wasEngaged;
    private bool _burstFinished;

    public double FrequencyHz { get; set; } = 12.0;
    public double DutyCycle { get; set; } = 0.5;
    public int BurstLimit { get; set; } = 0; // 0 = continuous / infinite

    public bool IsActive => _wasEngaged;
    public int CompletedPulses => _completedPulses;

    /// <summary>
    /// Resets synthesizer state and releases active pulse.
    /// </summary>
    public void Reset()
    {
        _phaseTime = 0.0;
        _completedPulses = 0;
        _wasEngaged = false;
        _burstFinished = false;
    }

    /// <summary>
    /// Updates the pulse synthesizer given frame delta time and whether the physical trigger is held.
    /// </summary>
    public TurboPulseState Update(double deltaTimeSeconds, bool isTriggerPressed)
    {
        if (!isTriggerPressed)
        {
            Reset();
            return new TurboPulseState(IsEngaged: false, JustFired: false, BurstCompleted: false, PulseCount: 0);
        }

        if (_burstFinished)
        {
            return new TurboPulseState(IsEngaged: false, JustFired: false, BurstCompleted: true, PulseCount: _completedPulses);
        }

        double safeFreq = Math.Clamp(FrequencyHz, 0.5, 60.0);
        double safeDuty = Math.Clamp(DutyCycle, 0.05, 0.95);
        double period = 1.0 / safeFreq;
        double activeThreshold = period * safeDuty;

        _phaseTime += deltaTimeSeconds;

        bool justFired = false;

        // Check if cycle rolled over
        while (_phaseTime >= period)
        {
            _phaseTime -= period;
            _completedPulses++;

            if (BurstLimit > 0 && _completedPulses >= BurstLimit)
            {
                _burstFinished = true;
                _wasEngaged = false;
                return new TurboPulseState(IsEngaged: false, JustFired: false, BurstCompleted: true, PulseCount: _completedPulses);
            }
        }

        bool isEngaged = _phaseTime < activeThreshold;
        if (isEngaged && !_wasEngaged)
        {
            justFired = true;
        }

        _wasEngaged = isEngaged;

        return new TurboPulseState(
            IsEngaged: isEngaged,
            JustFired: justFired,
            BurstCompleted: false,
            PulseCount: _completedPulses
        );
    }
}
