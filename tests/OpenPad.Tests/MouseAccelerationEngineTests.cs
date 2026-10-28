using NUnit.Framework;
using OpenPad.Core.Engine;

namespace OpenPad.Tests;

[TestFixture]
public class MouseAccelerationEngineTests
{
    private MouseAccelerationEngine _engine = null!;

    [SetUp]
    public void Setup()
    {
        _engine = new MouseAccelerationEngine();
    }

    [Test]
    public void ComputeMouseDelta_ZeroInput_ReturnsZero()
    {
        var (dx, dy) = _engine.ComputeMouseDelta(0f, 0f);
        Assert.That(dx, Is.EqualTo(0));
        Assert.That(dy, Is.EqualTo(0));
    }

    [Test]
    public void ComputeMouseDelta_LinearCurve_ProducesMovement()
    {
        _engine.Curve = MouseAccelerationCurve.Linear;
        _engine.BaseSensitivity = 20.0f;

        var (dx, dy) = _engine.ComputeMouseDelta(1.0f, 0.0f, deltaTimeSeconds: 0.1f);

        Assert.That(dx, Is.GreaterThan(0));
        Assert.That(dy, Is.EqualTo(0));
    }

    [Test]
    public void ComputeMouseDelta_SniperActive_ReducesSpeedSignificantly()
    {
        _engine.Curve = MouseAccelerationCurve.Linear;
        _engine.BaseSensitivity = 50.0f;
        _engine.SniperMultiplier = 0.2f;

        _engine.IsSniperActive = false;
        var (normalDx, _) = _engine.ComputeMouseDelta(1.0f, 0.0f, deltaTimeSeconds: 0.1f);

        _engine.Reset();
        _engine.IsSniperActive = true;
        var (sniperDx, _) = _engine.ComputeMouseDelta(1.0f, 0.0f, deltaTimeSeconds: 0.1f);

        Assert.That(sniperDx, Is.LessThan(normalDx));
        Assert.That(sniperDx, Is.GreaterThan(0));
    }

    [Test]
    public void ComputeMouseDelta_DynamicAcceleration_IncreasesSpeedWhenHeld()
    {
        _engine.Curve = MouseAccelerationCurve.Dynamic;
        _engine.DynamicThreshold = 0.5f;
        _engine.DynamicRampTimeSeconds = 0.5f;
        _engine.MaxDynamicMultiplier = 3.0f;
        _engine.BaseSensitivity = 20.0f;

        // First frame
        var (firstDx, _) = _engine.ComputeMouseDelta(1.0f, 0.0f, deltaTimeSeconds: 0.1f);

        // Advance time while holding full deflection
        for (int i = 0; i < 6; i++)
        {
            _engine.ComputeMouseDelta(1.0f, 0.0f, deltaTimeSeconds: 0.1f);
        }

        var (rampedDx, _) = _engine.ComputeMouseDelta(1.0f, 0.0f, deltaTimeSeconds: 0.1f);

        Assert.That(rampedDx, Is.GreaterThan(firstDx));
    }

    [Test]
    public void ComputeMouseDelta_AxisSnapping_SnapsSlightDiagonalToPureHorizontal()
    {
        _engine.EnableAxisSnapping = true;
        _engine.SnapThresholdAngleDegrees = 10.0f;
        _engine.BaseSensitivity = 30.0f;

        // Angle ~ 3 degrees (very close to pure horizontal +X)
        float x = 1.0f;
        float y = 0.05f;

        var (dx, dy) = _engine.ComputeMouseDelta(x, y, deltaTimeSeconds: 0.1f);

        Assert.That(dx, Is.GreaterThan(0));
        Assert.That(dy, Is.EqualTo(0));
    }

    [Test]
    public void ComputeMouseDelta_Inversion_FlipsDirection()
    {
        _engine.InvertX = true;
        _engine.InvertY = true;
        _engine.BaseSensitivity = 20.0f;

        var (dx, dy) = _engine.ComputeMouseDelta(1.0f, 1.0f, deltaTimeSeconds: 0.1f);

        Assert.That(dx, Is.LessThan(0));
        Assert.That(dy, Is.LessThan(0));
    }

    [Test]
    public void ComputeMouseDelta_SubPixelAccumulation_AccumulatesMicroMovement()
    {
        _engine.Curve = MouseAccelerationCurve.None;
        _engine.BaseSensitivity = 1.0f;

        // Tiny delta that would yield < 0.5 pixel on single frame
        var (dx1, _) = _engine.ComputeMouseDelta(0.1f, 0.0f, deltaTimeSeconds: 0.005f);

        // After multiple frames, the subpixel fraction should accumulate into at least 1 pixel
        int totalDx = dx1;
        for (int i = 0; i < 20; i++)
        {
            var (stepDx, _) = _engine.ComputeMouseDelta(0.1f, 0.0f, deltaTimeSeconds: 0.005f);
            totalDx += stepDx;
        }

        Assert.That(totalDx, Is.GreaterThan(0));
    }
}
