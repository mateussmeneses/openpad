using NUnit.Framework;
using OpenPad.Core.Engine;

namespace OpenPad.Tests;

[TestFixture]
public class InputLatencyProfilerTests
{
    private InputLatencyProfiler _profiler = null!;

    [SetUp]
    public void SetUp()
    {
        _profiler = new InputLatencyProfiler(windowSize: 50);
    }

    [Test]
    public void GetDiagnostics_EmptyProfiler_ReturnsZeroHz()
    {
        var diag = _profiler.GetDiagnostics();

        Assert.That(diag.PollingRateHz, Is.EqualTo(0.0));
        Assert.That(diag.TotalSamples, Is.EqualTo(0));
    }

    [Test]
    public void RecordSample_Regular120HzFeed_CalculatesAccurateFrequency()
    {
        double timestamp = 1000.0;
        const double interval120Hz = 8.3333; // ~120 Hz

        for (int i = 0; i < 30; i++)
        {
            _profiler.RecordSample(timestamp);
            timestamp += interval120Hz;
        }

        var diag = _profiler.GetDiagnostics();

        Assert.That(diag.PollingRateHz, Is.InRange(118.0, 122.0));
        Assert.That(diag.AverageIntervalMs, Is.InRange(8.2, 8.5));
        Assert.That(diag.JitterMs, Is.LessThan(0.1)); // Very low jitter on regular feed
        Assert.That(diag.MissedTickCount, Is.EqualTo(0));
    }

    [Test]
    public void RecordSample_IrregularIntervals_DetectsMissedTicksAndJitter()
    {
        double timestamp = 1000.0;
        _profiler.RecordSample(timestamp);

        // Tick with large delay (stutter)
        timestamp += 25.0; // > 16.6ms threshold
        _profiler.RecordSample(timestamp);

        timestamp += 8.0;
        _profiler.RecordSample(timestamp);

        var diag = _profiler.GetDiagnostics();

        Assert.That(diag.MissedTickCount, Is.GreaterThanOrEqualTo(1));
        Assert.That(diag.JitterMs, Is.GreaterThan(0.0));
    }

    [Test]
    public void Reset_ClearsAllRecordedState()
    {
        _profiler.RecordSample(10.0);
        _profiler.RecordSample(20.0);

        _profiler.Reset();

        var diag = _profiler.GetDiagnostics();
        Assert.That(diag.PollingRateHz, Is.EqualTo(0.0));
        Assert.That(diag.TotalSamples, Is.EqualTo(0));
    }
}
