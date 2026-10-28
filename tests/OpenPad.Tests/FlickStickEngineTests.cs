using NUnit.Framework;
using OpenPad.Core.Engine;

namespace OpenPad.Tests;

[TestFixture]
public class FlickStickEngineTests
{
    private FlickStickEngine _engine = null!;

    [SetUp]
    public void Setup()
    {
        _engine = new FlickStickEngine();
    }

    [Test]
    public void ComputeTurnDelta_InsideDeadzone_ReturnsZero()
    {
        int dx = _engine.ComputeTurnDelta(0.05f, 0.05f, deltaTimeSeconds: 0.016f);
        Assert.That(dx, Is.EqualTo(0));
        Assert.That(_engine.IsEngaged, Is.False);
    }

    [Test]
    public void ComputeTurnDelta_FlickRight_GeneratesPositiveTurn()
    {
        _engine.PixelsPerFull360 = 3600.0f;
        _engine.SmoothTimeSeconds = 0.05f;

        // Flick pure right (+X=1.0, Y=0.0) -> Angle = +90 degrees (+PI/2 rad)
        int totalDx = 0;
        for (int i = 0; i < 10; i++)
        {
            totalDx += _engine.ComputeTurnDelta(1.0f, 0.0f, deltaTimeSeconds: 0.016f);
        }

        Assert.That(totalDx, Is.GreaterThan(0));
        // 90 degrees out of 360 degrees should be ~900 pixels
        Assert.That(totalDx, Is.InRange(800, 1000));
    }

    [Test]
    public void ComputeTurnDelta_FlickLeft_GeneratesNegativeTurn()
    {
        _engine.PixelsPerFull360 = 3600.0f;
        _engine.SmoothTimeSeconds = 0.05f;

        // Flick pure left (-X=1.0, Y=0.0) -> Angle = -90 degrees (-PI/2 rad)
        int totalDx = 0;
        for (int i = 0; i < 10; i++)
        {
            totalDx += _engine.ComputeTurnDelta(-1.0f, 0.0f, deltaTimeSeconds: 0.016f);
        }

        Assert.That(totalDx, Is.LessThan(0));
        Assert.That(totalDx, Is.InRange(-1000, -800));
    }

    [Test]
    public void ComputeTurnDelta_InvertTurn_FlipsSign()
    {
        _engine.PixelsPerFull360 = 3600.0f;
        _engine.SmoothTimeSeconds = 0.05f;
        _engine.InvertTurn = true;

        // Flick pure right with inverted turn should generate negative dx
        int totalDx = 0;
        for (int i = 0; i < 10; i++)
        {
            totalDx += _engine.ComputeTurnDelta(1.0f, 0.0f, deltaTimeSeconds: 0.016f);
        }

        Assert.That(totalDx, Is.LessThan(0));
    }

    [Test]
    public void Reset_ClearsPendingRotationAndNeutralizesState()
    {
        _engine.ComputeTurnDelta(1.0f, 0.0f, deltaTimeSeconds: 0.001f);
        Assert.That(_engine.IsEngaged, Is.True);

        _engine.Reset();
        Assert.That(_engine.IsEngaged, Is.False);

        int postResetDx = _engine.ComputeTurnDelta(0f, 0f, deltaTimeSeconds: 0.016f);
        Assert.That(postResetDx, Is.EqualTo(0));
    }
}
