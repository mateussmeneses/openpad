using NUnit.Framework;
using OpenPad.Core.Engine;

namespace OpenPad.Tests;

[TestFixture]
public class OpenPadMicrokernelTests
{
    private OpenPadMicrokernel _microkernel = null!;

    [SetUp]
    public void SetUp()
    {
        _microkernel = new OpenPadMicrokernel();
    }

    [TearDown]
    public void TearDown()
    {
        _microkernel.Shutdown();
    }

    [Test]
    public void Initialize_SetsRunningStateAndProperties()
    {
        _microkernel.Initialize(capacity: 512, watchdogTimeoutMs: 25.0);

        Assert.That(_microkernel.State, Is.EqualTo(MicrokernelState.Running));
        Assert.That(_microkernel.Capacity, Is.EqualTo(512));
        Assert.That(_microkernel.WatchdogTimeoutMs, Is.EqualTo(25.0));
        Assert.That(_microkernel.IsWatchdogHealthy, Is.True);
        Assert.That(_microkernel.TotalEventsProcessed, Is.EqualTo(0));
        Assert.That(_microkernel.DroppedEvents, Is.EqualTo(0));
    }

    [Test]
    public void Initialize_InvalidArguments_ThrowsException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _microkernel.Initialize(capacity: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => _microkernel.Initialize(watchdogTimeoutMs: -1.0));
    }

    [Test]
    public void EnqueueAndProcessTick_DispatchesEventsToSubscribers()
    {
        _microkernel.Initialize(capacity: 10, watchdogTimeoutMs: 50.0);

        var received = new List<MicrokernelInputEvent>();
        _microkernel.RegisterSubscriber(received.Add);

        var evt1 = new MicrokernelInputEvent(1, 100.0, 0, "ButtonA", 1.0f, 1.0f, true);
        var evt2 = new MicrokernelInputEvent(2, 101.0, 0, "LeftStickX", 0.5f, 0.48f, true);

        Assert.That(_microkernel.EnqueueEvent(evt1), Is.True);
        Assert.That(_microkernel.EnqueueEvent(evt2), Is.True);

        int processed = _microkernel.ProcessTick(105.0);

        Assert.That(processed, Is.EqualTo(2));
        Assert.That(_microkernel.TotalEventsProcessed, Is.EqualTo(2));
        Assert.That(received.Count, Is.EqualTo(2));
        Assert.That(received[0].InputCode, Is.EqualTo("ButtonA"));
        Assert.That(received[1].InputCode, Is.EqualTo("LeftStickX"));
        Assert.That(_microkernel.AverageDispatchLatencyMicroseconds, Is.GreaterThan(0));
    }

    [Test]
    public void Enqueue_WhenBufferFull_DropsEventsAndIncrementsCounter()
    {
        _microkernel.Initialize(capacity: 2, watchdogTimeoutMs: 50.0);

        var evt1 = new MicrokernelInputEvent(1, 10.0, 0, "Btn1", 1f, 1f, true);
        var evt2 = new MicrokernelInputEvent(2, 11.0, 0, "Btn2", 1f, 1f, true);
        var evt3 = new MicrokernelInputEvent(3, 12.0, 0, "Btn3", 1f, 1f, true);

        Assert.That(_microkernel.EnqueueEvent(evt1), Is.True);
        Assert.That(_microkernel.EnqueueEvent(evt2), Is.True);
        Assert.That(_microkernel.EnqueueEvent(evt3), Is.False);

        Assert.That(_microkernel.DroppedEvents, Is.EqualTo(1));
    }

    [Test]
    public void Watchdog_TriggersSafeModeWhenTickExceedsTimeout()
    {
        _microkernel.Initialize(capacity: 64, watchdogTimeoutMs: 30.0);

        _microkernel.ProcessTick(100.0);
        Assert.That(_microkernel.State, Is.EqualTo(MicrokernelState.Running));

        // Advance timestamp beyond 30ms watchdog window
        _microkernel.ProcessTick(135.0);
        Assert.That(_microkernel.State, Is.EqualTo(MicrokernelState.SafeMode));
        Assert.That(_microkernel.IsWatchdogHealthy, Is.False);

        // Reset state recovers to Running
        _microkernel.ResetState(135.0);
        Assert.That(_microkernel.State, Is.EqualTo(MicrokernelState.Running));
    }

    [Test]
    public void FaultIsolation_SubscriberExceptionTransitionsToDegraded()
    {
        _microkernel.Initialize(capacity: 10, watchdogTimeoutMs: 100.0);

        _microkernel.RegisterSubscriber(_ => throw new InvalidOperationException("Plugin crashed"));
        _microkernel.EnqueueEvent(new MicrokernelInputEvent(1, 10.0, 0, "Trigger", 1.0f, 1.0f, true));

        _microkernel.ProcessTick(12.0);

        Assert.That(_microkernel.State, Is.EqualTo(MicrokernelState.Degraded));
        Assert.That(_microkernel.TotalEventsProcessed, Is.EqualTo(1));
    }

    [Test]
    public void CreateIpcPacket_ProducesFormattedTransmission()
    {
        _microkernel.Initialize();
        var evt = new MicrokernelInputEvent(42, 50.0, 1, "RightTrigger", 0.85f, 0.85f, true);

        var packet = _microkernel.CreateIpcPacket("ipc://openpad-ring0", evt, 51.0);

        Assert.That(packet.SequenceNumber, Is.EqualTo(1));
        Assert.That(packet.ChannelId, Is.EqualTo("ipc://openpad-ring0"));
        Assert.That(packet.Payload.InputCode, Is.EqualTo("RightTrigger"));
        Assert.That(packet.TransmittedAtMs, Is.EqualTo(51.0));
    }

    [Test]
    public void Shutdown_DrainsBufferAndStopsMicrokernel()
    {
        _microkernel.Initialize(capacity: 10);
        _microkernel.EnqueueEvent(new MicrokernelInputEvent(1, 10.0, 0, "Btn", 1f, 1f, true));

        _microkernel.Shutdown();

        Assert.That(_microkernel.State, Is.EqualTo(MicrokernelState.Stopped));
        Assert.That(_microkernel.EnqueueEvent(new MicrokernelInputEvent(2, 11.0, 0, "Btn", 1f, 1f, true)), Is.False);
    }
}
