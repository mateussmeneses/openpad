using System;
using System.Collections.Generic;
using System.Linq;
using OpenPad.Core.Models;

namespace OpenPad.Core.Engine;

/// <summary>
/// Execution state of the input replay engine.
/// </summary>
public enum ReplayState
{
    Idle,
    Recording,
    Playing
}

/// <summary>
/// Individual captured frame in an input macro or speedrun sequence.
/// </summary>
public record ReplayFrame(
    long FrameIndex,
    double DeltaMs,
    IReadOnlyDictionary<GamepadButton, bool> Buttons,
    IReadOnlyDictionary<GamepadAxis, float> Axes
);

/// <summary>
/// Deterministic input sequence recorder and playback engine.
/// Supports frame-accurate macro execution, loop playback, playback speed scaling,
/// and instant safety abort when user physical override is detected.
/// </summary>
public class InputReplayEngine
{
    private readonly List<ReplayFrame> _frames = new();
    private int _playbackIndex;
    private long _frameCounter;

    public ReplayState State { get; private set; } = ReplayState.Idle;
    public bool IsLooping { get; private set; }
    public double PlaybackSpeed { get; private set; } = 1.0;
    public bool AbortOnUserOverride { get; set; } = true;

    public int RecordedFrameCount => _frames.Count;

    /// <summary>
    /// Begins capturing incoming gamepad snapshots.
    /// </summary>
    public void StartRecording()
    {
        _frames.Clear();
        _frameCounter = 0;
        State = ReplayState.Recording;
    }

    /// <summary>
    /// Records a single frame snapshot into the sequence.
    /// </summary>
    public void RecordSnapshot(ControllerSnapshot snapshot, double deltaMs)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (State != ReplayState.Recording)
        {
            return;
        }

        var frame = new ReplayFrame(
            FrameIndex: _frameCounter++,
            DeltaMs: deltaMs,
            Buttons: new Dictionary<GamepadButton, bool>((IDictionary<GamepadButton, bool>)snapshot.Buttons),
            Axes: new Dictionary<GamepadAxis, float>((IDictionary<GamepadAxis, float>)snapshot.Axes)
        );

        _frames.Add(frame);
    }

    /// <summary>
    /// Stops recording and finalizes the sequence.
    /// </summary>
    public void StopRecording()
    {
        if (State == ReplayState.Recording)
        {
            State = ReplayState.Idle;
        }
    }

    /// <summary>
    /// Starts deterministic playback of the recorded sequence.
    /// </summary>
    public void StartPlayback(bool loop = false, double speed = 1.0)
    {
        if (_frames.Count == 0)
        {
            return;
        }

        IsLooping = loop;
        PlaybackSpeed = Math.Clamp(speed, 0.1, 10.0);
        _playbackIndex = 0;
        State = ReplayState.Playing;
    }

    /// <summary>
    /// Retrieves the next frame in the sequence, checking for user input override.
    /// </summary>
    public ControllerSnapshot? GetNextFrame(ControllerSnapshot? userPhysicalInput = null)
    {
        if (State != ReplayState.Playing || _frames.Count == 0)
        {
            return null;
        }

        // Safety check: Detect physical player intervention
        if (AbortOnUserOverride && userPhysicalInput != null && HasActiveInput(userPhysicalInput))
        {
            StopPlayback();
            return userPhysicalInput;
        }

        var frame = _frames[_playbackIndex];
        _playbackIndex++;

        if (_playbackIndex >= _frames.Count)
        {
            if (IsLooping)
            {
                _playbackIndex = 0;
            }
            else
            {
                StopPlayback();
            }
        }

        return new ControllerSnapshot(
            deviceId: "replay_virtual",
            deviceName: "OpenPad Replay Stream",
            isConnected: true,
            timestamp: DateTime.UtcNow,
            buttons: (IDictionary<GamepadButton, bool>)frame.Buttons,
            axes: (IDictionary<GamepadAxis, float>)frame.Axes
        );
    }

    /// <summary>
    /// Aborts or finishes active playback.
    /// </summary>
    public void StopPlayback()
    {
        State = ReplayState.Idle;
        _playbackIndex = 0;
    }

    private static bool HasActiveInput(ControllerSnapshot snapshot)
    {
        if (snapshot.Buttons.Values.Any(v => v))
        {
            return true;
        }

        foreach (var axis in snapshot.Axes.Values)
        {
            if (Math.Abs(axis) > 0.25f)
            {
                return true;
            }
        }

        return false;
    }
}
