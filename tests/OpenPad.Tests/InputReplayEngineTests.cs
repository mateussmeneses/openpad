using System;
using System.Collections.Generic;
using NUnit.Framework;
using OpenPad.Core.Engine;
using OpenPad.Core.Models;

namespace OpenPad.Tests;

[TestFixture]
public class InputReplayEngineTests
{
    private InputReplayEngine _engine = null!;

    [SetUp]
    public void SetUp()
    {
        _engine = new InputReplayEngine();
    }

    [Test]
    public void RecordSnapshot_CapturesSequenceAndReportsFrameCount()
    {
        _engine.StartRecording();

        var snap1 = new ControllerSnapshot(
            "dev", "pad", true, DateTime.UtcNow,
            new Dictionary<GamepadButton, bool> { [GamepadButton.A] = true },
            new Dictionary<GamepadAxis, float> { [GamepadAxis.LeftX] = 0.5f }
        );

        var snap2 = new ControllerSnapshot(
            "dev", "pad", true, DateTime.UtcNow,
            new Dictionary<GamepadButton, bool> { [GamepadButton.A] = false, [GamepadButton.B] = true },
            new Dictionary<GamepadAxis, float> { [GamepadAxis.LeftX] = 1.0f }
        );

        _engine.RecordSnapshot(snap1, 16.6);
        _engine.RecordSnapshot(snap2, 16.6);
        _engine.StopRecording();

        Assert.That(_engine.State, Is.EqualTo(ReplayState.Idle));
        Assert.That(_engine.RecordedFrameCount, Is.EqualTo(2));
    }

    [Test]
    public void StartPlayback_ReplaysFramesInOrder()
    {
        _engine.StartRecording();
        var snap1 = new ControllerSnapshot(
            "dev", "pad", true, DateTime.UtcNow,
            new Dictionary<GamepadButton, bool> { [GamepadButton.A] = true },
            new Dictionary<GamepadAxis, float>()
        );
        var snap2 = new ControllerSnapshot(
            "dev", "pad", true, DateTime.UtcNow,
            new Dictionary<GamepadButton, bool> { [GamepadButton.X] = true },
            new Dictionary<GamepadAxis, float>()
        );
        _engine.RecordSnapshot(snap1, 10.0);
        _engine.RecordSnapshot(snap2, 10.0);
        _engine.StopRecording();

        _engine.StartPlayback(loop: false);

        var f1 = _engine.GetNextFrame();
        Assert.That(f1, Is.Not.Null);
        Assert.That(f1!.IsButtonPressed(GamepadButton.A), Is.True);

        var f2 = _engine.GetNextFrame();
        Assert.That(f2, Is.Not.Null);
        Assert.That(f2!.IsButtonPressed(GamepadButton.X), Is.True);

        // Sequence exhausted
        var f3 = _engine.GetNextFrame();
        Assert.That(f3, Is.Null);
        Assert.That(_engine.State, Is.EqualTo(ReplayState.Idle));
    }

    [Test]
    public void StartPlayback_WithLoop_RepeatsContinuously()
    {
        _engine.StartRecording();
        var snap = new ControllerSnapshot(
            "dev", "pad", true, DateTime.UtcNow,
            new Dictionary<GamepadButton, bool> { [GamepadButton.Y] = true },
            new Dictionary<GamepadAxis, float>()
        );
        _engine.RecordSnapshot(snap, 10.0);
        _engine.StopRecording();

        _engine.StartPlayback(loop: true);

        var f1 = _engine.GetNextFrame();
        var f2 = _engine.GetNextFrame();

        Assert.That(f1, Is.Not.Null);
        Assert.That(f2, Is.Not.Null);
        Assert.That(_engine.State, Is.EqualTo(ReplayState.Playing));
    }

    [Test]
    public void GetNextFrame_WhenUserIntervenes_AbortsPlayback()
    {
        _engine.StartRecording();
        var recordedSnap = new ControllerSnapshot(
            "dev", "pad", true, DateTime.UtcNow,
            new Dictionary<GamepadButton, bool>(),
            new Dictionary<GamepadAxis, float>()
        );
        _engine.RecordSnapshot(recordedSnap, 10.0);
        _engine.StopRecording();

        _engine.StartPlayback();

        // Simulate physical player pressing RightTrigger
        var physicalOverride = new ControllerSnapshot(
            "dev", "pad", true, DateTime.UtcNow,
            new Dictionary<GamepadButton, bool>(),
            new Dictionary<GamepadAxis, float> { [GamepadAxis.TriggerRight] = 0.8f }
        );

        var result = _engine.GetNextFrame(physicalOverride);

        Assert.That(result, Is.SameAs(physicalOverride));
        Assert.That(_engine.State, Is.EqualTo(ReplayState.Idle)); // Aborted
    }
}
