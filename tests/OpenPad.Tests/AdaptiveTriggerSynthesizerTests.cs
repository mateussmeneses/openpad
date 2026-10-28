using NUnit.Framework;
using OpenPad.Core.Engine;

namespace OpenPad.Tests;

[TestFixture]
public class AdaptiveTriggerSynthesizerTests
{
    private AdaptiveTriggerSynthesizer _synthesizer = null!;

    [SetUp]
    public void SetUp()
    {
        _synthesizer = new AdaptiveTriggerSynthesizer();
    }

    [Test]
    public void BuildTriggerPacket_OffMode_ReturnsZeroedPayload()
    {
        var effect = AdaptiveTriggerSynthesizer.Off();
        var packet = _synthesizer.BuildTriggerPacket(effect);

        Assert.That(packet.Length, Is.EqualTo(10));
        Assert.That(packet[0], Is.EqualTo(0x00));
        Assert.That(packet[1], Is.EqualTo(0));
    }

    [Test]
    public void BuildTriggerPacket_FeedbackMode_EncodesPositionAndForce()
    {
        var effect = AdaptiveTriggerSynthesizer.Feedback(startPos: 3, force: 7);
        var packet = _synthesizer.BuildTriggerPacket(effect);

        Assert.That(packet[0], Is.EqualTo((byte)AdaptiveTriggerMode.Feedback));
        Assert.That(packet[1], Is.EqualTo(3));
        Assert.That(packet[2], Is.EqualTo(7));
    }

    [Test]
    public void BuildTriggerPacket_WeaponMode_EncodesBreakThresholds()
    {
        var effect = AdaptiveTriggerSynthesizer.Weapon(startPos: 2, endPos: 6, force: 8);
        var packet = _synthesizer.BuildTriggerPacket(effect);

        Assert.That(packet[0], Is.EqualTo((byte)AdaptiveTriggerMode.Weapon));
        Assert.That(packet[1], Is.EqualTo(2));
        Assert.That(packet[2], Is.EqualTo(6));
        Assert.That(packet[3], Is.EqualTo(8));
    }

    [Test]
    public void BuildTriggerPacket_VibrationMode_EncodesFrequency()
    {
        var effect = AdaptiveTriggerSynthesizer.Vibration(startPos: 1, force: 5, frequency: 180);
        var packet = _synthesizer.BuildTriggerPacket(effect);

        Assert.That(packet[0], Is.EqualTo((byte)AdaptiveTriggerMode.Vibration));
        Assert.That(packet[1], Is.EqualTo(1));
        Assert.That(packet[2], Is.EqualTo(5));
        Assert.That(packet[3], Is.EqualTo(180));
    }

    [Test]
    public void BuildTriggerPacket_BowMode_EncodesTensionRamp()
    {
        var effect = AdaptiveTriggerSynthesizer.Bow(startPos: 1, endPos: 8, force: 6);
        var packet = _synthesizer.BuildTriggerPacket(effect);

        Assert.That(packet[0], Is.EqualTo((byte)AdaptiveTriggerMode.Bow));
        Assert.That(packet[1], Is.EqualTo(1));
        Assert.That(packet[2], Is.EqualTo(8));
        Assert.That(packet[3], Is.EqualTo(6));
    }
}
