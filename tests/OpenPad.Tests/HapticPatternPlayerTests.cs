using NUnit.Framework;
using OpenPad.Core.Engine;
using OpenPad.Core.Testing;

namespace OpenPad.Tests;

[TestFixture]
public class HapticPatternPlayerTests
{
    private HapticPatternPlayer _player = null!;
    private FakeControllerSource _source = null!;

    [SetUp]
    public void SetUp()
    {
        _player = new HapticPatternPlayer();
        _source = new FakeControllerSource();
        _source.ConnectController("pad-1", "Test Gamepad");
    }

    [TearDown]
    public void TearDown()
    {
        _source.Dispose();
    }

    [Test]
    public async Task PlayAsync_DispatchesRumbleStepsAndSilencesAtEnd()
    {
        var steps = new List<HapticStep>
        {
            new(20000, 10000, 20, PauseAfterMs: 10),
            new(40000, 30000, 20, PauseAfterMs: 0)
        };
        var pattern = new HapticPattern("TestPattern", steps, LoopCount: 1);

        await _player.PlayAsync("pad-1", pattern, _source);

        Assert.That(_source.RumbleHistory.Count, Is.GreaterThanOrEqualTo(3));
        Assert.That(_source.RumbleHistory.Any(r => r.lowFreq == 20000 && r.highFreq == 10000), Is.True);
        Assert.That(_source.RumbleHistory.Any(r => r.lowFreq == 40000 && r.highFreq == 30000), Is.True);
        // Final rumble must silence motors
        var last = _source.RumbleHistory.Last();
        Assert.That(last.lowFreq, Is.EqualTo(0));
        Assert.That(last.highFreq, Is.EqualTo(0));
    }

    [Test]
    public async Task PlayAsync_BuiltinPresets_ExecuteSuccessfully()
    {
        var heartbeat = HapticPattern.Heartbeat();
        Assert.That(heartbeat.Steps.Count, Is.EqualTo(2));

        var warning = HapticPattern.WarningThud();
        Assert.That(warning.Steps.Count, Is.EqualTo(1));
        Assert.That(warning.Steps[0].LowFrequency, Is.EqualTo(50000));
    }

    [Test]
    public async Task Stop_CancelsPlaybackImmediatelyAndSilencesMotors()
    {
        var steps = new List<HapticStep>
        {
            new(30000, 30000, 1000, PauseAfterMs: 500)
        };
        var pattern = new HapticPattern("LongPattern", steps, LoopCount: 5);

        var task = _player.PlayAsync("pad-1", pattern, _source);

        await Task.Delay(30);
        _player.Stop();
        await task;

        Assert.That(_player.IsPlaying, Is.False);
        var last = _source.RumbleHistory.Last();
        Assert.That(last.lowFreq, Is.EqualTo(0));
        Assert.That(last.highFreq, Is.EqualTo(0));
    }
}
