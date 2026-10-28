using NUnit.Framework;
using OpenPad.Core.Engine;
using OpenPad.Core.Models;

namespace OpenPad.Tests;

[TestFixture]
public class ChordingEngineTests
{
    private ChordingEngine _engine = null!;
    private ButtonChord _abChord = null!;

    [SetUp]
    public void Setup()
    {
        _engine = new ChordingEngine();
        _engine.ToleranceWindowSeconds = 0.040; // 40ms

        _abChord = new ButtonChord
        {
            Id = "chord_ab",
            Name = "A + B Chord",
            Buttons = new HashSet<GamepadButton> { GamepadButton.A, GamepadButton.B },
            OutputAction = InputAction.FromKey(VirtualKey.Space)
        };

        _engine.RegisterChord(_abChord);
    }

    [Test]
    public void ProcessButton_UnrelatedButton_FlushesImmediately()
    {
        GamepadButton? flushedButton = null;
        _engine.SingleButtonFlushed += (btn) => flushedButton = btn;

        _engine.ProcessButton(GamepadButton.X, isPressed: true);

        Assert.That(flushedButton, Is.EqualTo(GamepadButton.X));
        Assert.That(_engine.IsChordEngaged, Is.False);
    }

    [Test]
    public void ProcessButton_ChordPressedSimultaneously_FiresChordWithoutSingleFlushes()
    {
        ButtonChord? activatedChord = null;
        var flushedButtons = new List<GamepadButton>();

        _engine.ChordActivated += (c) => activatedChord = c;
        _engine.SingleButtonFlushed += (b) => flushedButtons.Add(b);

        _engine.ProcessButton(GamepadButton.A, isPressed: true);
        _engine.ProcessButton(GamepadButton.B, isPressed: true);

        Assert.That(activatedChord, Is.Not.Null);
        Assert.That(activatedChord?.Id, Is.EqualTo("chord_ab"));
        Assert.That(flushedButtons, Is.Empty);
        Assert.That(_engine.IsChordEngaged, Is.True);
    }

    [Test]
    public void ProcessButton_ChordPressedWithinToleranceWindow_CompletesChordSuccessfully()
    {
        ButtonChord? activatedChord = null;
        _engine.ChordActivated += (c) => activatedChord = c;

        // Press A at t=0
        _engine.ProcessButton(GamepadButton.A, isPressed: true);
        Assert.That(activatedChord, Is.Null);

        // Advance 20ms (< 40ms tolerance)
        _engine.Update(0.020);
        Assert.That(activatedChord, Is.Null);

        // Press B at t=20ms
        _engine.ProcessButton(GamepadButton.B, isPressed: true);

        Assert.That(activatedChord, Is.Not.Null);
        Assert.That(activatedChord?.Id, Is.EqualTo("chord_ab"));
    }

    [Test]
    public void Update_SingleChordButtonPressedAndWindowExpires_FlushesSingleButton()
    {
        var flushedButtons = new List<GamepadButton>();
        _engine.SingleButtonFlushed += (b) => flushedButtons.Add(b);

        // Press A at t=0
        _engine.ProcessButton(GamepadButton.A, isPressed: true);
        Assert.That(flushedButtons, Is.Empty);

        // Advance 50ms (> 40ms tolerance)
        _engine.Update(0.050);

        Assert.That(flushedButtons, Has.Count.EqualTo(1));
        Assert.That(flushedButtons[0], Is.EqualTo(GamepadButton.A));
        Assert.That(_engine.IsChordEngaged, Is.False);
    }

    [Test]
    public void ProcessButton_ReleaseChordButton_FiresChordReleased()
    {
        ButtonChord? releasedChord = null;
        _engine.ChordReleased += (c) => releasedChord = c;

        _engine.ProcessButton(GamepadButton.A, isPressed: true);
        _engine.ProcessButton(GamepadButton.B, isPressed: true);
        Assert.That(_engine.IsChordEngaged, Is.True);

        // Release A
        _engine.ProcessButton(GamepadButton.A, isPressed: false);

        Assert.That(releasedChord, Is.Not.Null);
        Assert.That(releasedChord?.Id, Is.EqualTo("chord_ab"));
        Assert.That(_engine.IsChordEngaged, Is.False);
    }

    [Test]
    public void Reset_ClearsAllPendingAndActiveChords()
    {
        _engine.ProcessButton(GamepadButton.A, isPressed: true);
        _engine.ProcessButton(GamepadButton.B, isPressed: true);
        Assert.That(_engine.IsChordEngaged, Is.True);

        _engine.Reset();
        Assert.That(_engine.IsChordEngaged, Is.False);
    }
}
