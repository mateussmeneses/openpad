using NUnit.Framework;
using OpenPad.Core.Engine;

namespace OpenPad.Tests;

[TestFixture]
public class GyroAimingFilterTests
{
    private GyroAimingFilter _filter = null!;

    [SetUp]
    public void SetUp()
    {
        _filter = new GyroAimingFilter
        {
            DeadbandDegPerSec = 2.0f,
            SensitivityScale = 0.1f
        };
    }

    [Test]
    public void Process_BelowDeadband_ReturnsZeroDisplacement()
    {
        var (dx, dy) = _filter.Process(pitchDegPerSec: 1.0f, yawDegPerSec: 1.0f);

        Assert.That(dx, Is.EqualTo(0));
        Assert.That(dy, Is.EqualTo(0));
    }

    [Test]
    public void Process_AboveDeadband_GeneratesSmoothDisplacement()
    {
        // 20 deg/s yaw -> magnitude 20 -> reduction factor (20-2)/20 = 0.9 -> filtered 18 deg/s
        var (dx, dy) = _filter.Process(pitchDegPerSec: 0f, yawDegPerSec: 20f, deltaSeconds: 0.0166f);

        Assert.That(dx, Is.GreaterThan(0));
        Assert.That(dy, Is.EqualTo(0));
    }

    [Test]
    public void Process_SubPixelAccumulation_PreservesSlowMicroMovements()
    {
        // Very small above-deadband speed that produces < 0.5 pixels per frame
        int totalDx = 0;
        for (int i = 0; i < 10; i++)
        {
            var (dx, _) = _filter.Process(pitchDegPerSec: 0f, yawDegPerSec: 2.5f, deltaSeconds: 0.0166f);
            totalDx += dx;
        }

        // Over 10 frames, accumulated sub-pixels must trigger at least 1 mouse step!
        Assert.That(totalDx, Is.GreaterThan(0));
    }

    [Test]
    public void Process_InvertAxes_ReversesSign()
    {
        _filter.InvertPitch = true;
        _filter.InvertYaw = true;

        var (dx, dy) = _filter.Process(pitchDegPerSec: 15f, yawDegPerSec: 15f, deltaSeconds: 0.0166f);

        Assert.That(dx, Is.LessThan(0));
        Assert.That(dy, Is.GreaterThan(0)); // inverted pitch moves mouse down (+dy in Win32)
    }

    [Test]
    public void Process_WhenDisabled_AlwaysReturnsZero()
    {
        _filter.IsEnabled = false;

        var (dx, dy) = _filter.Process(pitchDegPerSec: 50f, yawDegPerSec: 50f);

        Assert.That(dx, Is.EqualTo(0));
        Assert.That(dy, Is.EqualTo(0));
    }
}
