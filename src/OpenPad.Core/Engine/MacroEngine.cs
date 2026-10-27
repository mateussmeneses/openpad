using System.Diagnostics;
using OpenPad.Core.Interfaces;
using OpenPad.Core.Models;

namespace OpenPad.Core.Engine;

public enum MacroStepType
{
    KeyDown,
    KeyUp,
    MouseDown,
    MouseUp,
    Delay
}

public record MacroStep(MacroStepType Type, VirtualKey? Key = null, MouseButton? Button = null, int DelayMs = 0);

public record MacroSequence(string Name, IReadOnlyList<MacroStep> Steps, int RepeatCount = 1);

/// <summary>
/// Execution and recording engine for timed gamepad-to-keyboard/mouse macros.
/// Ensures thread safety and atomic cancellation on emergency disarm.
/// </summary>
public class MacroEngine
{
    private readonly object _syncLock = new();
    private CancellationTokenSource? _currentExecutionCts;
    private readonly List<MacroStep> _recordedSteps = new();
    private readonly Stopwatch _recordStopwatch = new();
    private bool _isRecording;

    public bool IsRecording
    {
        get
        {
            lock (_syncLock) return _isRecording;
        }
    }

    public bool IsPlaying
    {
        get
        {
            lock (_syncLock) return _currentExecutionCts != null && !_currentExecutionCts.IsCancellationRequested;
        }
    }

    public void StartRecording()
    {
        lock (_syncLock)
        {
            _recordedSteps.Clear();
            _isRecording = true;
            _recordStopwatch.Restart();
        }
    }

    public void RecordStep(MacroStepType type, VirtualKey? key = null, MouseButton? button = null)
    {
        lock (_syncLock)
        {
            if (!_isRecording) return;

            long elapsedMs = _recordStopwatch.ElapsedMilliseconds;
            if (elapsedMs > 0 && _recordedSteps.Count > 0)
            {
                _recordedSteps.Add(new MacroStep(MacroStepType.Delay, DelayMs: (int)elapsedMs));
            }
            _recordStopwatch.Restart();

            _recordedSteps.Add(new MacroStep(type, Key: key, Button: button));
        }
    }

    public MacroSequence StopRecording(string macroName = "RecordedMacro")
    {
        lock (_syncLock)
        {
            _isRecording = false;
            _recordStopwatch.Stop();
            return new MacroSequence(macroName, _recordedSteps.ToList(), RepeatCount: 1);
        }
    }

    public async Task PlayAsync(MacroSequence macro, IInputSink sink, CancellationToken externalCt = default)
    {
        ArgumentNullException.ThrowIfNull(macro);
        ArgumentNullException.ThrowIfNull(sink);

        CancellationTokenSource linkedCts;
        lock (_syncLock)
        {
            Stop();
            _currentExecutionCts = new CancellationTokenSource();
            linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_currentExecutionCts.Token, externalCt);
        }

        var ct = linkedCts.Token;

        try
        {
            int iterations = macro.RepeatCount;
            int counter = 0;

            while (iterations < 0 || counter < iterations)
            {
                ct.ThrowIfCancellationRequested();

                foreach (var step in macro.Steps)
                {
                    ct.ThrowIfCancellationRequested();

                    switch (step.Type)
                    {
                        case MacroStepType.KeyDown when step.Key.HasValue:
                            sink.SendKeyDown(step.Key.Value);
                            break;
                        case MacroStepType.KeyUp when step.Key.HasValue:
                            sink.SendKeyUp(step.Key.Value);
                            break;
                        case MacroStepType.MouseDown when step.Button.HasValue:
                            sink.SendMouseDown(step.Button.Value);
                            break;
                        case MacroStepType.MouseUp when step.Button.HasValue:
                            sink.SendMouseUp(step.Button.Value);
                            break;
                        case MacroStepType.Delay when step.DelayMs > 0:
                            await Task.Delay(step.DelayMs, ct);
                            break;
                    }
                }

                counter++;
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on stop / emergency disarm
        }
        finally
        {
            sink.ReleaseAll();
            lock (_syncLock)
            {
                if (_currentExecutionCts == linkedCts)
                {
                    _currentExecutionCts.Dispose();
                    _currentExecutionCts = null;
                }
            }
        }
    }

    public void Stop()
    {
        lock (_syncLock)
        {
            if (_currentExecutionCts != null)
            {
                try
                {
                    _currentExecutionCts.Cancel();
                    _currentExecutionCts.Dispose();
                }
                catch (ObjectDisposedException) { }
                _currentExecutionCts = null;
            }
        }
    }
}
