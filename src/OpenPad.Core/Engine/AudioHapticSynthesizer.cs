using System;

namespace OpenPad.Core.Engine;

/// <summary>
/// Configuration for the real-time audio-to-haptic rumble synthesizer.
/// </summary>
public record AudioHapticConfig
{
    /// <summary>
    /// Master volume/gain multiplier for haptic motors [0.0 - 2.0]. Default 1.0.
    /// </summary>
    public float MasterGain { get; init; } = 1.0f;

    /// <summary>
    /// Sensitivity threshold for the heavy low-frequency motor [0.0 - 1.0].
    /// </summary>
    public float BassThreshold { get; init; } = 0.05f;

    /// <summary>
    /// Sensitivity threshold for the light high-frequency motor [0.0 - 1.0].
    /// </summary>
    public float TrebleThreshold { get; init; } = 0.03f;

    /// <summary>
    /// Attack responsiveness [0.1 - 1.0]. Higher values cause immediate snappy vibration.
    /// </summary>
    public float AttackSpeed { get; init; } = 0.65f;

    /// <summary>
    /// Decay dampening [0.01 - 0.5]. Controls how quickly motor spin-down decelerates.
    /// </summary>
    public float DecaySpeed { get; init; } = 0.20f;
}

/// <summary>
/// Converts real-time audio streams (PCM waveforms or FFT spectral bands) into dual-motor
/// rumble feedback for game controllers. Separates low-end explosions/bass (Large Motor)
/// from crisp high-end transients/clicks/footsteps (Small Motor).
/// </summary>
public class AudioHapticSynthesizer
{
    private float _largeMotorLevel;
    private float _smallMotorLevel;

    /// <summary>
    /// Resets envelope followers and silences both rumble motors.
    /// </summary>
    public void Reset()
    {
        _largeMotorLevel = 0f;
        _smallMotorLevel = 0f;
    }

    /// <summary>
    /// Processes discrete frequency bands (e.g. from an FFT audio analyzer) to generate rumble intensities.
    /// Expects at least 4 frequency bands: [SubBass, LowMids, HighMids, Treble].
    /// </summary>
    /// <param name="bands">Normalized spectral energy levels per band [0.0 .. 1.0].</param>
    /// <param name="config">Synthesizer tuning parameters.</param>
    /// <returns>Dual motor rumble intensities [0 .. 65535].</returns>
    public (ushort LargeMotor, ushort SmallMotor) ProcessSpectrum(ReadOnlySpan<float> bands, AudioHapticConfig? config = null)
    {
        config ??= new AudioHapticConfig();

        if (bands.IsEmpty)
        {
            ApplyDecay(0f, 0f, config);
            return ToUshortTuple();
        }

        float targetBass = 0f;
        float targetTreble = 0f;

        if (bands.Length >= 4)
        {
            // Low bands (Sub-bass + Bass)
            targetBass = Math.Max(bands[0], bands[1]);
            // High bands (High-mids + Treble)
            targetTreble = Math.Max(bands[2], bands[3]);
        }
        else
        {
            targetBass = bands[0];
            targetTreble = bands.Length > 1 ? bands[1] : bands[0];
        }

        // Apply threshold gating
        if (targetBass < config.BassThreshold) targetBass = 0f;
        if (targetTreble < config.TrebleThreshold) targetTreble = 0f;

        // Apply master gain
        targetBass *= config.MasterGain;
        targetTreble *= config.MasterGain;

        ApplyDecay(targetBass, targetTreble, config);

        return ToUshortTuple();
    }

    /// <summary>
    /// Processes raw PCM floating-point audio samples (-1.0 to 1.0) directly through time-domain filtering.
    /// </summary>
    /// <param name="samples">Buffer of audio samples.</param>
    /// <param name="config">Synthesizer tuning parameters.</param>
    /// <returns>Dual motor rumble intensities [0 .. 65535].</returns>
    public (ushort LargeMotor, ushort SmallMotor) ProcessPcmBuffer(ReadOnlySpan<float> samples, AudioHapticConfig? config = null)
    {
        config ??= new AudioHapticConfig();

        if (samples.IsEmpty)
        {
            ApplyDecay(0f, 0f, config);
            return ToUshortTuple();
        }

        float sumEnergy = 0f;
        float highDiffEnergy = 0f;
        float prevSample = 0f;

        for (int i = 0; i < samples.Length; i++)
        {
            float s = samples[i];
            float abs = Math.Abs(s);
            sumEnergy += abs;

            if (i > 0)
            {
                // High transient energy based on sample-to-sample difference
                highDiffEnergy += Math.Abs(s - prevSample);
            }
            prevSample = s;
        }

        float avgBass = sumEnergy / samples.Length;
        float avgTreble = highDiffEnergy / samples.Length;

        if (avgBass < config.BassThreshold) avgBass = 0f;
        if (avgTreble < config.TrebleThreshold) avgTreble = 0f;

        avgBass *= config.MasterGain;
        avgTreble *= config.MasterGain;

        ApplyDecay(avgBass, avgTreble, config);

        return ToUshortTuple();
    }

    private void ApplyDecay(float targetBass, float targetTreble, AudioHapticConfig config)
    {
        // Envelope smoothing for large motor (bass)
        if (targetBass > _largeMotorLevel)
        {
            _largeMotorLevel += (targetBass - _largeMotorLevel) * config.AttackSpeed;
        }
        else
        {
            _largeMotorLevel -= (_largeMotorLevel - targetBass) * config.DecaySpeed;
        }

        // Envelope smoothing for small motor (treble)
        if (targetTreble > _smallMotorLevel)
        {
            _smallMotorLevel += (targetTreble - _smallMotorLevel) * config.AttackSpeed;
        }
        else
        {
            _smallMotorLevel -= (_smallMotorLevel - targetTreble) * config.DecaySpeed;
        }

        _largeMotorLevel = Math.Clamp(_largeMotorLevel, 0f, 1.0f);
        _smallMotorLevel = Math.Clamp(_smallMotorLevel, 0f, 1.0f);
    }

    private (ushort LargeMotor, ushort SmallMotor) ToUshortTuple()
    {
        ushort large = (ushort)Math.Clamp((int)(_largeMotorLevel * 65535f), 0, 65535);
        ushort small = (ushort)Math.Clamp((int)(_smallMotorLevel * 65535f), 0, 65535);
        return (large, small);
    }
}
