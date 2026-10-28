using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

namespace OpenPad.Core.Engine;

/// <summary>
/// Operational execution state of the zero-latency OpenPad Microkernel.
/// </summary>
public enum MicrokernelState
{
    Uninitialized,
    Running,
    Degraded,
    SafeMode,
    Stopped
}

/// <summary>
/// Low-level input event processed across the microkernel ring buffer.
/// </summary>
public readonly record struct MicrokernelInputEvent(
    long EventId,
    double TimestampMs,
    int DeviceSlot,
    string InputCode,
    float RawValue,
    float FilteredValue,
    bool Handled
);

/// <summary>
/// Inter-process communication packet for shared-memory / IPC event transmission.
/// </summary>
public sealed record MicrokernelIpcPacket(
    long SequenceNumber,
    string ChannelId,
    MicrokernelInputEvent Payload,
    double TransmittedAtMs
);

/// <summary>
/// Decoupled, zero-latency input microkernel with high-frequency event streaming,
/// lock-free ring buffering, heartbeat watchdog monitoring, and fail-safe fallback modes.
/// </summary>
public sealed class OpenPadMicrokernel
{
    private readonly object _syncLock = new();
    private readonly List<Action<MicrokernelInputEvent>> _subscribers = new();
    private readonly ConcurrentQueue<MicrokernelInputEvent> _eventQueue = new();

    private int _maxCapacity = 1024;
    private double _watchdogTimeoutMs = 50.0;
    private double _lastHeartbeatTimestampMs;
    private long _totalEventsProcessed;
    private long _droppedEvents;
    private double _cumulativeLatencyUs;

    public MicrokernelState State { get; private set; } = MicrokernelState.Uninitialized;
    public int Capacity => _maxCapacity;
    public double WatchdogTimeoutMs => _watchdogTimeoutMs;
    public long TotalEventsProcessed => _totalEventsProcessed;
    public long DroppedEvents => _droppedEvents;
    public double AverageDispatchLatencyMicroseconds => _totalEventsProcessed > 0
        ? _cumulativeLatencyUs / _totalEventsProcessed
        : 0.0;
    public bool IsWatchdogHealthy => State == MicrokernelState.Running;

    /// <summary>
    /// Initializes the input microkernel with a designated ring buffer capacity and watchdog timeout.
    /// </summary>
    public void Initialize(int capacity = 1024, double watchdogTimeoutMs = 50.0)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "Ring buffer capacity must be strictly positive.");
        }

        if (watchdogTimeoutMs <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(watchdogTimeoutMs), "Watchdog timeout must be strictly positive.");
        }

        lock (_syncLock)
        {
            _maxCapacity = capacity;
            _watchdogTimeoutMs = watchdogTimeoutMs;
            _lastHeartbeatTimestampMs = 0.0;
            _totalEventsProcessed = 0;
            _droppedEvents = 0;
            _cumulativeLatencyUs = 0.0;
            _subscribers.Clear();

            while (_eventQueue.TryDequeue(out _)) { }

            State = MicrokernelState.Running;
        }
    }

    /// <summary>
    /// Registers an event subscriber callback invoked on microkernel dispatch ticks.
    /// </summary>
    public void RegisterSubscriber(Action<MicrokernelInputEvent> subscriber)
    {
        ArgumentNullException.ThrowIfNull(subscriber);

        lock (_syncLock)
        {
            _subscribers.Add(subscriber);
        }
    }

    /// <summary>
    /// Submits a raw input event to the microkernel ring buffer.
    /// Enforces backpressure drop if capacity is exceeded.
    /// </summary>
    public bool EnqueueEvent(MicrokernelInputEvent evt)
    {
        if (State != MicrokernelState.Running && State != MicrokernelState.Degraded)
        {
            return false;
        }

        if (_eventQueue.Count >= _maxCapacity)
        {
            _droppedEvents++;
            return false;
        }

        _eventQueue.Enqueue(evt);
        return true;
    }

    /// <summary>
    /// Dispatches pending ring buffer events and updates heartbeat watchdog timing.
    /// </summary>
    public int ProcessTick(double currentTimestampMs)
    {
        if (State == MicrokernelState.Stopped || State == MicrokernelState.Uninitialized)
        {
            return 0;
        }

        VerifyWatchdog(currentTimestampMs);

        int processed = 0;
        while (_eventQueue.TryDequeue(out var evt))
        {
            double dispatchLatencyUs = Math.Max(0.0, (currentTimestampMs - evt.TimestampMs) * 1000.0);
            _cumulativeLatencyUs += dispatchLatencyUs;
            _totalEventsProcessed++;
            processed++;

            List<Action<MicrokernelInputEvent>> subscribersCopy;
            lock (_syncLock)
            {
                subscribersCopy = [.. _subscribers];
            }

            foreach (var subscriber in subscribersCopy)
            {
                try
                {
                    subscriber(evt);
                }
                catch
                {
                    // Fault isolation: subscriber errors do not crash the microkernel
                    State = MicrokernelState.Degraded;
                }
            }
        }

        _lastHeartbeatTimestampMs = currentTimestampMs;
        return processed;
    }

    /// <summary>
    /// Evaluates watchdog health against time elapsed since the last heartbeat tick.
    /// </summary>
    public void VerifyWatchdog(double currentTimestampMs)
    {
        if (_lastHeartbeatTimestampMs > 0 && (currentTimestampMs - _lastHeartbeatTimestampMs) > _watchdogTimeoutMs)
        {
            State = MicrokernelState.SafeMode;
        }
    }

    /// <summary>
    /// Creates an IPC packet suitable for shared memory streaming across processes.
    /// </summary>
    public MicrokernelIpcPacket CreateIpcPacket(string channelId, MicrokernelInputEvent evt, double transmissionTimestampMs)
    {
        ArgumentNullException.ThrowIfNull(channelId);

        return new MicrokernelIpcPacket(
            SequenceNumber: _totalEventsProcessed + 1,
            ChannelId: channelId,
            Payload: evt,
            TransmittedAtMs: transmissionTimestampMs
        );
    }

    /// <summary>
    /// Recovers microkernel state from SafeMode or Degraded back to normal Running status.
    /// </summary>
    public void ResetState(double currentTimestampMs)
    {
        lock (_syncLock)
        {
            _lastHeartbeatTimestampMs = currentTimestampMs;
            State = MicrokernelState.Running;
        }
    }

    /// <summary>
    /// Halts microkernel execution and drains remaining buffer queues.
    /// </summary>
    public void Shutdown()
    {
        lock (_syncLock)
        {
            State = MicrokernelState.Stopped;
            while (_eventQueue.TryDequeue(out _)) { }
            _subscribers.Clear();
        }
    }
}
