using OpenPad.Core.Interfaces;

namespace OpenPad.Core.Engine;

public record HapticStep(ushort LowFrequency, ushort HighFrequency, uint DurationMs, uint PauseAfterMs = 0);

public record HapticPattern(string Name, IReadOnlyList<HapticStep> Steps, int LoopCount = 1)
{
    public static HapticPattern Heartbeat() => new("Heartbeat", new List<HapticStep>
    {
        new(30000, 15000, 80, PauseAfterMs: 60),
        new(45000, 25000, 120, PauseAfterMs: 300)
    });

    public static HapticPattern DoublePulse() => new("DoublePulse", new List<HapticStep>
    {
        new(35000, 35000, 70, PauseAfterMs: 50),
        new(35000, 35000, 70, PauseAfterMs: 0)
    });

    public static HapticPattern WarningThud() => new("WarningThud", new List<HapticStep>
    {
        new(50000, 40000, 200, PauseAfterMs: 0)
    });
}

/// <summary>
/// Sequenced vibration player delivering haptic feedback patterns through SDL3 gamepads.
/// </summary>
public class HapticPatternPlayer
{
    private readonly object _syncLock = new();
    private CancellationTokenSource? _currentCts;

    public bool IsPlaying
    {
        get
        {
            lock (_syncLock) return _currentCts != null && !_currentCts.IsCancellationRequested;
        }
    }

    public async Task PlayAsync(string deviceId, HapticPattern pattern, IControllerSource source, CancellationToken externalCt = default)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentNullException.ThrowIfNull(source);

        CancellationTokenSource linkedCts;
        lock (_syncLock)
        {
            Stop();
            _currentCts = new CancellationTokenSource();
            linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_currentCts.Token, externalCt);
        }

        var ct = linkedCts.Token;

        try
        {
            int loops = pattern.LoopCount;
            int counter = 0;

            while (loops < 0 || counter < loops)
            {
                ct.ThrowIfCancellationRequested();

                foreach (var step in pattern.Steps)
                {
                    ct.ThrowIfCancellationRequested();

                    source.SetRumble(deviceId, step.LowFrequency, step.HighFrequency, step.DurationMs);

                    if (step.DurationMs > 0)
                    {
                        await Task.Delay((int)step.DurationMs, ct);
                    }

                    if (step.PauseAfterMs > 0)
                    {
                        // Silence motors during pause
                        source.SetRumble(deviceId, 0, 0, 0);
                        await Task.Delay((int)step.PauseAfterMs, ct);
                    }
                }

                counter++;
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on stop / cancel
        }
        finally
        {
            // Always ensure motors are silenced when pattern ends
            try
            {
                source.SetRumble(deviceId, 0, 0, 0);
            }
            catch { }

            lock (_syncLock)
            {
                if (_currentCts == linkedCts)
                {
                    _currentCts.Dispose();
                    _currentCts = null;
                }
            }
        }
    }

    public void Stop()
    {
        lock (_syncLock)
        {
            if (_currentCts != null)
            {
                try
                {
                    _currentCts.Cancel();
                    _currentCts.Dispose();
                }
                catch (ObjectDisposedException) { }
                _currentCts = null;
            }
        }
    }
}
