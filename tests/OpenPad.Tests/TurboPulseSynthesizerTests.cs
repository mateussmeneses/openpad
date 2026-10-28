using NUnit.Framework;
using OpenPad.Core.Engine;

namespace OpenPad.Tests;

[TestFixture]
public class TurboPulseSynthesizerTests
{
    private TurboPulseSynthesizer _synthesizer = null!;

    [SetUp]
    public void Setup()
    {
        _synthesizer = new TurboPulseSynthesizer();
    }

    [Test]
    public void Update_WhenTriggerNotPressed_ReturnsDisengaged()
    {
        var state = _synthesizer.Update(0.016, isTriggerPressed: false);

        Assert.That(state.IsEngaged, Is.False);
        Assert.That(state.JustFired, Is.False);
        Assert.That(state.BurstCompleted, Is.False);
    }

    [Test]
    public void Update_FirstFramePressed_EngagesAndFires()
    {
        _synthesizer.FrequencyHz = 10.0; // Period = 0.1s, active for 0.05s
        _synthesizer.DutyCycle = 0.5;

        var state = _synthesizer.Update(0.01, isTriggerPressed: true);

        Assert.That(state.IsEngaged, Is.True);
        Assert.That(state.JustFired, Is.True);
        Assert.That(state.BurstCompleted, Is.False);
    }

    [Test]
    public void Update_AlternatesThroughActiveAndRestPhases()
    {
        _synthesizer.FrequencyHz = 10.0; // Period = 100ms: 50ms active, 50ms released
        _synthesizer.DutyCycle = 0.5;

        // Frame 1: 20ms in -> active
        var s1 = _synthesizer.Update(0.02, isTriggerPressed: true);
        Assert.That(s1.IsEngaged, Is.True);

        // Frame 2: 40ms in -> active
        var s2 = _synthesizer.Update(0.02, isTriggerPressed: true);
        Assert.That(s2.IsEngaged, Is.True);

        // Frame 3: 70ms in -> released
        var s3 = _synthesizer.Update(0.03, isTriggerPressed: true);
        Assert.That(s3.IsEngaged, Is.False);

        // Frame 4: 110ms in (cycle wrapped) -> active again
        var s4 = _synthesizer.Update(0.04, isTriggerPressed: true);
        Assert.That(s4.IsEngaged, Is.True);
        Assert.That(s4.JustFired, Is.True);
    }

    [Test]
    public void Update_BurstLimit_StopsFiringAfterExactCount()
    {
        _synthesizer.FrequencyHz = 20.0; // Period = 50ms
        _synthesizer.DutyCycle = 0.5;
        _synthesizer.BurstLimit = 3; // Exactly 3 pulses

        int firedCount = 0;
        bool burstEnded = false;

        // Simulate 200ms in 10ms steps
        for (int i = 0; i < 20; i++)
        {
            var state = _synthesizer.Update(0.01, isTriggerPressed: true);
            if (state.JustFired) firedCount++;
            if (state.BurstCompleted) burstEnded = true;
        }

        Assert.That(firedCount, Is.EqualTo(3));
        Assert.That(burstEnded, Is.True);
        Assert.That(_synthesizer.CompletedPulses, Is.GreaterThanOrEqualTo(3));
    }

    [Test]
    public void Update_ReleasingTrigger_ImmediatelyResets()
    {
        _synthesizer.FrequencyHz = 10.0;
        _synthesizer.Update(0.02, isTriggerPressed: true);
        Assert.That(_synthesizer.IsActive, Is.True);

        var releaseState = _synthesizer.Update(0.01, isTriggerPressed: false);
        Assert.That(releaseState.IsEngaged, Is.False);
        Assert.That(_synthesizer.IsActive, Is.False);
    }
}
