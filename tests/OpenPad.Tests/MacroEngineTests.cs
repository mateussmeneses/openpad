using NUnit.Framework;
using OpenPad.Core.Engine;
using OpenPad.Core.Models;
using OpenPad.Core.Testing;

namespace OpenPad.Tests;

[TestFixture]
public class MacroEngineTests
{
    private MacroEngine _engine = null!;
    private RecordingInputSink _sink = null!;

    [SetUp]
    public void SetUp()
    {
        _engine = new MacroEngine();
        _sink = new RecordingInputSink();
    }

    [TearDown]
    public void TearDown()
    {
        _sink.Dispose();
    }

    [Test]
    public void RecordStep_AppendsStepsToMacroSequence()
    {
        _engine.StartRecording();
        Assert.That(_engine.IsRecording, Is.True);

        _engine.RecordStep(MacroStepType.KeyDown, key: VirtualKey.A);
        _engine.RecordStep(MacroStepType.KeyUp, key: VirtualKey.A);

        var macro = _engine.StopRecording("TestMacro");

        Assert.That(_engine.IsRecording, Is.False);
        Assert.That(macro.Name, Is.EqualTo("TestMacro"));
        Assert.That(macro.Steps.Count, Is.GreaterThanOrEqualTo(2));
        Assert.That(macro.Steps.Any(s => s.Type == MacroStepType.KeyDown && s.Key == VirtualKey.A), Is.True);
        Assert.That(macro.Steps.Any(s => s.Type == MacroStepType.KeyUp && s.Key == VirtualKey.A), Is.True);
    }

    [Test]
    public async Task PlayAsync_DispatchesKeysAndReleasesOnFinish()
    {
        var steps = new List<MacroStep>
        {
            new(MacroStepType.KeyDown, Key: VirtualKey.B),
            new(MacroStepType.KeyUp, Key: VirtualKey.B),
            new(MacroStepType.MouseDown, Button: MouseButton.Left),
            new(MacroStepType.MouseUp, Button: MouseButton.Left)
        };
        var macro = new MacroSequence("QuickSequence", steps, RepeatCount: 1);

        await _engine.PlayAsync(macro, _sink);

        var history = _sink.RecordedEvents;
        Assert.That(history.Any(e => e.EventType == InputEventType.KeyDown && e.Key == VirtualKey.B), Is.True);
        Assert.That(history.Any(e => e.EventType == InputEventType.KeyUp && e.Key == VirtualKey.B), Is.True);
        Assert.That(history.Any(e => e.EventType == InputEventType.MouseDown && e.MouseButton == MouseButton.Left), Is.True);
        Assert.That(history.Any(e => e.EventType == InputEventType.MouseUp && e.MouseButton == MouseButton.Left), Is.True);
        Assert.That(history.Last().EventType, Is.EqualTo(InputEventType.ReleaseAll));
    }

    [Test]
    public async Task PlayAsync_MultipleRepeats_ExecutesStepsRepeatedly()
    {
        var steps = new List<MacroStep>
        {
            new(MacroStepType.KeyDown, Key: VirtualKey.F),
            new(MacroStepType.KeyUp, Key: VirtualKey.F)
        };
        var macro = new MacroSequence("TripleRepeat", steps, RepeatCount: 3);

        await _engine.PlayAsync(macro, _sink);

        int keyDownCount = _sink.RecordedEvents.Count(e => e.EventType == InputEventType.KeyDown && e.Key == VirtualKey.F);
        Assert.That(keyDownCount, Is.EqualTo(3));
    }

    [Test]
    public async Task Stop_CancelsPlaybackImmediatelyAndReleasesInputs()
    {
        var steps = new List<MacroStep>
        {
            new(MacroStepType.KeyDown, Key: VirtualKey.W),
            new(MacroStepType.Delay, DelayMs: 1000), // Long delay
            new(MacroStepType.KeyUp, Key: VirtualKey.W)
        };
        var macro = new MacroSequence("Stoppable", steps, RepeatCount: 5);

        var playTask = _engine.PlayAsync(macro, _sink);

        await Task.Delay(50);
        _engine.Stop();

        await playTask;

        Assert.That(_engine.IsPlaying, Is.False);
        Assert.That(_sink.RecordedEvents.Last().EventType, Is.EqualTo(InputEventType.ReleaseAll));
    }
}
