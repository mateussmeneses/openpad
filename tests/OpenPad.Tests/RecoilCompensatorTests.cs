using NUnit.Framework;
using OpenPad.Core.Engine;

namespace OpenPad.Tests;

[TestFixture]
public class RecoilCompensatorTests
{
    private RecoilCompensator _compensator = null!;

    [SetUp]
    public void SetUp()
    {
        // Deterministic seed for reproducible tests
        _compensator = new RecoilCompensator(seed: 42);
    }

    [Test]
    public void ProcessTick_WhenNotShooting_ReturnsZero()
    {
        var (dx, dy) = _compensator.ProcessTick(isShooting: false, elapsedShootingMs: 500.0);

        Assert.That(dx, Is.EqualTo(0f));
        Assert.That(dy, Is.EqualTo(0f));
    }

    [Test]
    public void ProcessTick_BeforeRampUpDelay_ReturnsZero()
    {
        var config = new RecoilPatternConfig
        {
            RampUpDelayMs = 100.0,
            VerticalStrength = 5.0f
        };

        // At 50ms, ramp up has not elapsed
        var (dx, dy) = _compensator.ProcessTick(isShooting: true, elapsedShootingMs: 50.0, config);

        Assert.That(dx, Is.EqualTo(0f));
        Assert.That(dy, Is.EqualTo(0f));
    }

    [Test]
    public void ProcessTick_AfterRampUpDelay_ProducesVerticalPull()
    {
        var config = new RecoilPatternConfig
        {
            RampUpDelayMs = 50.0,
            VerticalStrength = 6.0f,
            HorizontalStrength = 0.0f,
            HumanizationJitter = 0.0f // Exact calculation
        };

        var (dx, dy) = _compensator.ProcessTick(isShooting: true, elapsedShootingMs: 150.0, config);

        Assert.That(dx, Is.EqualTo(0f));
        Assert.That(dy, Is.EqualTo(6.0f));
    }

    [Test]
    public void ProcessTick_WithHorizontalDrift_CompensatesBothAxes()
    {
        var config = new RecoilPatternConfig
        {
            RampUpDelayMs = 20.0,
            VerticalStrength = 4.0f,
            HorizontalStrength = -2.5f,
            HumanizationJitter = 0.0f
        };

        var (dx, dy) = _compensator.ProcessTick(isShooting: true, elapsedShootingMs: 100.0, config);

        Assert.That(dx, Is.EqualTo(-2.5f));
        Assert.That(dy, Is.EqualTo(4.0f));
    }

    [Test]
    public void ProcessTick_WithHumanizationJitter_VariesWithinBounds()
    {
        var config = new RecoilPatternConfig
        {
            RampUpDelayMs = 0.0,
            VerticalStrength = 10.0f,
            HumanizationJitter = 0.15f // ±15% variation: [8.5 .. 11.5]
        };

        var (dx, dy) = _compensator.ProcessTick(isShooting: true, elapsedShootingMs: 50.0, config);

        Assert.That(dy, Is.InRange(8.5f, 11.5f));
    }

    [Test]
    public void ProcessTick_ClampsToMaxDeltaPerTick()
    {
        var config = new RecoilPatternConfig
        {
            RampUpDelayMs = 0.0,
            VerticalStrength = 50.0f, // Exceeds clamp
            MaxDeltaPerTick = 15.0f,
            HumanizationJitter = 0.0f
        };

        var (dx, dy) = _compensator.ProcessTick(isShooting: true, elapsedShootingMs: 100.0, config);

        Assert.That(dy, Is.EqualTo(15.0f));
    }
}
