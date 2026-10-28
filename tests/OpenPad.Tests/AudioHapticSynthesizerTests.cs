using System;
using NUnit.Framework;
using OpenPad.Core.Engine;

namespace OpenPad.Tests;

[TestFixture]
public class AudioHapticSynthesizerTests
{
    private AudioHapticSynthesizer _synth = null!;

    [SetUp]
    public void SetUp()
    {
        _synth = new AudioHapticSynthesizer();
    }

    [Test]
    public void ProcessSpectrum_SilentBuffer_ProducesZeroRumble()
    {
        float[] silentBands = [0f, 0f, 0f, 0f];
        var (large, small) = _synth.ProcessSpectrum(silentBands);

        Assert.That(large, Is.EqualTo(0));
        Assert.That(small, Is.EqualTo(0));
    }

    [Test]
    public void ProcessSpectrum_HeavyBass_DrivesLargeMotorPrimarily()
    {
        // Bass high (0.8), Treble zero (0.0)
        float[] bassHeavyBands = [0.8f, 0.7f, 0.0f, 0.0f];
        var config = new AudioHapticConfig
        {
            AttackSpeed = 1.0f // Instant attack for deterministic test
        };

        var (large, small) = _synth.ProcessSpectrum(bassHeavyBands, config);

        Assert.That(large, Is.GreaterThan(40000));
        Assert.That(small, Is.EqualTo(0));
    }

    [Test]
    public void ProcessSpectrum_HighTreble_DrivesSmallMotorPrimarily()
    {
        // Bass zero, Treble high (0.9)
        float[] trebleHeavyBands = [0.0f, 0.0f, 0.9f, 0.85f];
        var config = new AudioHapticConfig
        {
            AttackSpeed = 1.0f
        };

        var (large, small) = _synth.ProcessSpectrum(trebleHeavyBands, config);

        Assert.That(small, Is.GreaterThan(45000));
        Assert.That(large, Is.EqualTo(0));
    }

    [Test]
    public void ProcessPcmBuffer_SquareWaveHighFrequency_DrivesSmallMotor()
    {
        // High frequency alternating waveform creates rapid delta differences
        float[] pcm = [1.0f, -1.0f, 1.0f, -1.0f, 1.0f, -1.0f, 1.0f, -1.0f];
        var config = new AudioHapticConfig
        {
            AttackSpeed = 1.0f
        };

        var (large, small) = _synth.ProcessPcmBuffer(pcm, config);

        Assert.That(large, Is.GreaterThan(0));
        Assert.That(small, Is.GreaterThan(0));
    }

    [Test]
    public void Reset_ClearsEnvelopeFollowers()
    {
        float[] loudBands = [1.0f, 1.0f, 1.0f, 1.0f];
        _synth.ProcessSpectrum(loudBands);

        _synth.Reset();

        var (large, small) = _synth.ProcessSpectrum(ReadOnlySpan<float>.Empty);
        Assert.That(large, Is.EqualTo(0));
        Assert.That(small, Is.EqualTo(0));
    }
}
