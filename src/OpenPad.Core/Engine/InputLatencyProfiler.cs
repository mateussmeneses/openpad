namespace OpenPad.Core.Engine;

public record LatencyDiagnostics(
    double PollingRateHz,
    double AverageIntervalMs,
    double JitterMs,
    long TotalSamples,
    int MissedTickCount);

/// <summary>
/// Diagnostic profiler measuring gamepad polling frequency (Hz), inter-frame intervals, and timing jitter.
/// </summary>
public class InputLatencyProfiler
{
    private readonly int _windowSize;
    private readonly Queue<double> _intervalHistory = new();
    private readonly object _syncLock = new();
    private double _lastTimestampMs = -1.0;
    private long _totalSamples;
    private int _missedTickCount;

    public InputLatencyProfiler(int windowSize = 100)
    {
        _windowSize = Math.Max(10, windowSize);
    }

    public void RecordSample(double timestampMs)
    {
        lock (_syncLock)
        {
            if (_lastTimestampMs >= 0.0)
            {
                double interval = timestampMs - _lastTimestampMs;
                if (interval > 0.0)
                {
                    _intervalHistory.Enqueue(interval);
                    while (_intervalHistory.Count > _windowSize)
                    {
                        _intervalHistory.Dequeue();
                    }

                    // A missed tick is typically an interval exceeding 16.6ms (under 60Hz threshold)
                    if (interval > 16.6)
                    {
                        _missedTickCount++;
                    }
                }
            }

            _lastTimestampMs = timestampMs;
            _totalSamples++;
        }
    }

    public LatencyDiagnostics GetDiagnostics()
    {
        lock (_syncLock)
        {
            if (_intervalHistory.Count == 0)
            {
                return new LatencyDiagnostics(
                    PollingRateHz: 0.0,
                    AverageIntervalMs: 0.0,
                    JitterMs: 0.0,
                    TotalSamples: _totalSamples,
                    MissedTickCount: _missedTickCount);
            }

            double sum = 0.0;
            foreach (var val in _intervalHistory) sum += val;
            double avgInterval = sum / _intervalHistory.Count;

            // Calculate Jitter (standard deviation of intervals)
            double varianceSum = 0.0;
            foreach (var val in _intervalHistory)
            {
                double diff = val - avgInterval;
                varianceSum += diff * diff;
            }
            double jitter = Math.Sqrt(varianceSum / _intervalHistory.Count);

            double rateHz = avgInterval > 0.0 ? 1000.0 / avgInterval : 0.0;

            return new LatencyDiagnostics(
                PollingRateHz: Math.Round(rateHz, 1),
                AverageIntervalMs: Math.Round(avgInterval, 2),
                JitterMs: Math.Round(jitter, 2),
                TotalSamples: _totalSamples,
                MissedTickCount: _missedTickCount);
        }
    }

    public void Reset()
    {
        lock (_syncLock)
        {
            _intervalHistory.Clear();
            _lastTimestampMs = -1.0;
            _totalSamples = 0;
            _missedTickCount = 0;
        }
    }
}
