using System;
using NUnit.Framework;
using OpenPad.Core.Engine;

namespace OpenPad.Tests;

[TestFixture]
public class DeadzoneShapeMorpherTests
{
    private DeadzoneShapeMorpher _morpher = null!;

    [SetUp]
    public void SetUp()
    {
        _morpher = new DeadzoneShapeMorpher();
    }

    [Test]
    public void ProcessStick_WithinRadialDeadzone_ReturnsZero()
    {
        var config = new DeadzoneConfig
        {
            InnerDeadzone = 0.2f,
            OuterDeadzone = 0.9f,
            Shape = DeadzoneShape.Radial
        };

        var (x, y) = _morpher.ProcessStick(0.1f, 0.1f, config);

        Assert.That(x, Is.EqualTo(0f));
        Assert.That(y, Is.EqualTo(0f));
    }

    [Test]
    public void ProcessStick_BeyondOuterDeadzone_ReachesFullMagnitude()
    {
        var config = new DeadzoneConfig
        {
            InnerDeadzone = 0.1f,
            OuterDeadzone = 0.9f,
            Shape = DeadzoneShape.Radial,
            OutputShape = OutputShape.Circle
        };

        var (x, y) = _morpher.ProcessStick(0.95f, 0f, config);

        Assert.That(x, Is.EqualTo(1.0f).Within(0.001f));
        Assert.That(y, Is.EqualTo(0f).Within(0.001f));
    }

    [Test]
    public void ProcessStick_WithAntiDeadzone_JumpsToBoostThresholdImmediately()
    {
        var config = new DeadzoneConfig
        {
            InnerDeadzone = 0.1f,
            OuterDeadzone = 0.9f,
            AntiDeadzone = 0.25f,
            Shape = DeadzoneShape.Radial
        };

        var (x, y) = _morpher.ProcessStick(0.101f, 0f, config);

        Assert.That(x, Is.GreaterThanOrEqualTo(0.25f));
        Assert.That(y, Is.EqualTo(0f));
    }

    [Test]
    public void ProcessStick_AxialDeadzone_FiltersAxesIndependently()
    {
        var config = new DeadzoneConfig
        {
            InnerDeadzone = 0.2f,
            OuterDeadzone = 0.9f,
            Shape = DeadzoneShape.Axial
        };

        var (x, y) = _morpher.ProcessStick(0.05f, 0.55f, config);

        Assert.That(x, Is.EqualTo(0f));
        Assert.That(y, Is.GreaterThan(0.4f));
    }

    [Test]
    public void ProcessStick_SquareOutput_ExpandsDiagonalsToFullExtent()
    {
        var config = new DeadzoneConfig
        {
            InnerDeadzone = 0.0f,
            OuterDeadzone = 1.0f,
            Shape = DeadzoneShape.Radial,
            OutputShape = OutputShape.Square
        };

        float invSqrt2 = 1.0f / MathF.Sqrt(2f);
        var (x, y) = _morpher.ProcessStick(invSqrt2, invSqrt2, config);

        Assert.That(x, Is.EqualTo(1.0f).Within(0.01f));
        Assert.That(y, Is.EqualTo(1.0f).Within(0.01f));
    }

    [Test]
    public void ProcessStick_SquareDeadzone_DiscardsCornersInsideSquare()
    {
        var config = new DeadzoneConfig
        {
            InnerDeadzone = 0.3f,
            OuterDeadzone = 0.9f,
            Shape = DeadzoneShape.Square
        };

        var (x, y) = _morpher.ProcessStick(0.25f, 0.25f, config);

        Assert.That(x, Is.EqualTo(0f));
        Assert.That(y, Is.EqualTo(0f));
    }
}
