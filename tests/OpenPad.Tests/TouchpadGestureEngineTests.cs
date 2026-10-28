using System;
using NUnit.Framework;
using OpenPad.Core.Engine;

namespace OpenPad.Tests;

[TestFixture]
public class TouchpadGestureEngineTests
{
    private TouchpadGestureEngine _engine = null!;

    [SetUp]
    public void SetUp()
    {
        _engine = new TouchpadGestureEngine();
    }

    [Test]
    public void ProcessFrame_QuickDownAndUpWithoutDrift_DetectsSingleTap()
    {
        var t0 = new DateTime(2026, 10, 28, 12, 0, 0, DateTimeKind.Utc);
        var inactive = new TouchPoint(0f, 0f, false);

        // Touch 0 down at (0.5, 0.5)
        _engine.ProcessFrame(new TouchPoint(0.5f, 0.5f, true), inactive, t0);

        // Released 100ms later at (0.51, 0.51)
        var t1 = t0.AddMilliseconds(100);
        var gesture = _engine.ProcessFrame(new TouchPoint(0.51f, 0.51f, false), inactive, t1);

        Assert.That(gesture, Is.EqualTo(TouchGestureType.SingleTap));
    }

    [Test]
    public void ProcessFrame_HorizontalMovementBeyondThreshold_DetectsSwipeRight()
    {
        var t0 = new DateTime(2026, 10, 28, 12, 0, 0, DateTimeKind.Utc);
        var inactive = new TouchPoint(0f, 0f, false);

        // Start at left (0.2, 0.5)
        _engine.ProcessFrame(new TouchPoint(0.2f, 0.5f, true), inactive, t0);

        // Intermediate move to right (0.6, 0.5)
        var t1 = t0.AddMilliseconds(150);
        _engine.ProcessFrame(new TouchPoint(0.6f, 0.5f, true), inactive, t1);

        // Release at (0.6, 0.5)
        var t2 = t0.AddMilliseconds(200);
        var gesture = _engine.ProcessFrame(new TouchPoint(0.6f, 0.5f, false), inactive, t2);

        Assert.That(gesture, Is.EqualTo(TouchGestureType.SwipeRight));
    }

    [Test]
    public void ProcessFrame_VerticalMovementBeyondThreshold_DetectsSwipeUp()
    {
        var t0 = new DateTime(2026, 10, 28, 12, 0, 0, DateTimeKind.Utc);
        var inactive = new TouchPoint(0f, 0f, false);

        // Start at bottom (0.5, 0.8)
        _engine.ProcessFrame(new TouchPoint(0.5f, 0.8f, true), inactive, t0);

        // Move to top (0.5, 0.2) -> dy = -0.6
        var t1 = t0.AddMilliseconds(150);
        _engine.ProcessFrame(new TouchPoint(0.5f, 0.2f, true), inactive, t1);

        // Release at top
        var t2 = t0.AddMilliseconds(200);
        var gesture = _engine.ProcessFrame(new TouchPoint(0.5f, 0.2f, false), inactive, t2);

        Assert.That(gesture, Is.EqualTo(TouchGestureType.SwipeUp));
    }

    [Test]
    public void ProcessFrame_TwoFingersReleasedTogether_DetectsTwoFingerTap()
    {
        var t0 = new DateTime(2026, 10, 28, 12, 0, 0, DateTimeKind.Utc);

        // Both down
        _engine.ProcessFrame(
            new TouchPoint(0.3f, 0.5f, true),
            new TouchPoint(0.7f, 0.5f, true),
            t0
        );

        // Both released 120ms later
        var t1 = t0.AddMilliseconds(120);
        var gesture = _engine.ProcessFrame(
            new TouchPoint(0.3f, 0.5f, false),
            new TouchPoint(0.7f, 0.5f, false),
            t1
        );

        Assert.That(gesture, Is.EqualTo(TouchGestureType.TwoFingerTap));
    }

    [Test]
    public void ProcessFrame_DistanceSpreading_DetectsPinchOut()
    {
        var t0 = new DateTime(2026, 10, 28, 12, 0, 0, DateTimeKind.Utc);

        // Initial touches close together (dist = 0.1)
        _engine.ProcessFrame(
            new TouchPoint(0.45f, 0.5f, true),
            new TouchPoint(0.55f, 0.5f, true),
            t0
        );

        // Fingers spread apart (dist = 0.4, delta = +0.3)
        var t1 = t0.AddMilliseconds(50);
        var gesture = _engine.ProcessFrame(
            new TouchPoint(0.30f, 0.5f, true),
            new TouchPoint(0.70f, 0.5f, true),
            t1
        );

        Assert.That(gesture, Is.EqualTo(TouchGestureType.PinchOut));
    }
}
