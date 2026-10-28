using System;
using NUnit.Framework;
using OpenPad.Core.Engine;

namespace OpenPad.Tests;

[TestFixture]
public class StickCircularityBenchmarkTests
{
    private StickCircularityBenchmark _benchmark = null!;

    [SetUp]
    public void SetUp()
    {
        _benchmark = new StickCircularityBenchmark();
    }

    [Test]
    public void ComputeReport_PerfectCircleAndZeroRestDrift_AchievesGradeA()
    {
        // 36 samples on a perfect unit circle
        for (int i = 0; i < 36; i++)
        {
            double angle = (i / 36.0) * (2 * Math.PI);
            float x = (float)Math.Cos(angle);
            float y = (float)Math.Sin(angle);
            _benchmark.RecordOuterSample(x, y);
        }

        // Resting at center
        for (int i = 0; i < 20; i++)
        {
            _benchmark.RecordRestSample(0.01f, 0.01f);
        }

        var report = _benchmark.ComputeReport();

        Assert.That(report.CircularityErrorPercentage, Is.LessThan(1.0));
        Assert.That(report.CenterRestDrift, Is.LessThan(0.03));
        Assert.That(report.Grade, Is.EqualTo(StickHardwareGrade.GradeA_Pristine));
    }

    [Test]
    public void ComputeReport_SevereCenterDrift_AchievesGradeD()
    {
        // Resting stick is stuck at 0.35 drift
        for (int i = 0; i < 10; i++)
        {
            _benchmark.RecordRestSample(0.25f, 0.25f); // mag ~ 0.35
        }

        var report = _benchmark.ComputeReport();

        Assert.That(report.CenterRestDrift, Is.GreaterThan(0.30));
        Assert.That(report.Grade, Is.EqualTo(StickHardwareGrade.GradeD_CriticalReplacementNeeded));
    }

    [Test]
    public void Reset_ClearsAllRecordedSamples()
    {
        _benchmark.RecordOuterSample(1.0f, 0.0f);
        _benchmark.RecordRestSample(0.1f, 0.1f);

        _benchmark.Reset();

        var report = _benchmark.ComputeReport();
        Assert.That(report.OuterSampleCount, Is.EqualTo(0));
        Assert.That(report.RestSampleCount, Is.EqualTo(0));
    }
}
