using System;
using System.Collections.Generic;
using NUnit.Framework;
using OpenPad.Core.Engine;
using OpenPad.Core.Models;

namespace OpenPad.Tests;

[TestFixture]
public class VirtualBusDriverBridgeTests
{
    private VirtualBusDriverBridge _bridge = null!;

    [SetUp]
    public void SetUp()
    {
        _bridge = new VirtualBusDriverBridge();
    }

    [Test]
    public void Connect_SetsConnectedStateAndActiveType()
    {
        Assert.That(_bridge.IsConnected, Is.False);

        _bridge.Connect(VirtualControllerType.Xbox360);

        Assert.That(_bridge.IsConnected, Is.True);
        Assert.That(_bridge.ActiveType, Is.EqualTo(VirtualControllerType.Xbox360));
    }

    [Test]
    public void Disconnect_ResetsStateAndReport()
    {
        _bridge.Connect(VirtualControllerType.DualShock4);
        _bridge.Disconnect();

        Assert.That(_bridge.IsConnected, Is.False);
        Assert.That(_bridge.ActiveType, Is.Null);
    }

    [Test]
    public void SubmitSnapshot_NormalizesAxesAndTriggersCorrectly()
    {
        _bridge.Connect(VirtualControllerType.Xbox360);

        var buttons = new Dictionary<GamepadButton, bool>
        {
            [GamepadButton.A] = true,
            [GamepadButton.RightShoulder] = true
        };

        var axes = new Dictionary<GamepadAxis, float>
        {
            [GamepadAxis.TriggerLeft] = 1.0f,
            [GamepadAxis.TriggerRight] = 0.5f,
            [GamepadAxis.LeftX] = 1.0f,
            [GamepadAxis.LeftY] = -1.0f,
            [GamepadAxis.RightX] = 0.0f
        };

        var snapshot = new ControllerSnapshot("dev1", "Pad", true, DateTime.UtcNow, buttons, axes);

        var report = _bridge.SubmitSnapshot(snapshot);

        // Buttons
        Assert.That(report.Buttons & 0x1000, Is.Not.EqualTo(0)); // A button
        Assert.That(report.Buttons & 0x0200, Is.Not.EqualTo(0)); // Right Shoulder

        // Triggers (0-255)
        Assert.That(report.LeftTrigger, Is.EqualTo(255));
        Assert.That(report.RightTrigger, Is.InRange(127, 128));

        // Sticks (-32768 to 32767)
        Assert.That(report.ThumbLX, Is.EqualTo(32767));
        Assert.That(report.ThumbLY, Is.EqualTo(-32768));
        Assert.That(report.ThumbRX, Is.EqualTo(0));
    }

    [Test]
    public void DispatchRumbleFromGame_FiresRumbleReceivedEvent()
    {
        _bridge.Connect(VirtualControllerType.Xbox360);
        VirtualRumbleEvent? received = null;
        _bridge.OnRumbleReceived += evt => received = evt;

        _bridge.DispatchRumbleFromGame(200, 100);

        Assert.That(received, Is.Not.Null);
        Assert.That(received!.LargeMotor, Is.EqualTo(200));
        Assert.That(received.SmallMotor, Is.EqualTo(100));
    }

    [Test]
    public void SubmitSnapshot_WhenDisconnected_ThrowsInvalidOperationException()
    {
        var snapshot = new ControllerSnapshot(
            "dev", "name", true, DateTime.UtcNow,
            new Dictionary<GamepadButton, bool>(),
            new Dictionary<GamepadAxis, float>()
        );

        Assert.Throws<InvalidOperationException>(() => _bridge.SubmitSnapshot(snapshot));
    }
}
