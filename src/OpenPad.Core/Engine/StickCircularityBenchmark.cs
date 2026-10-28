using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenPad.Core.Engine;

/// <summary>
/// Diagnostic hardware health rating based on stick circularity and drift metrics.
/// </summary>
public enum StickHardwareGrade
{
    GradeA_Pristine,
    GradeB_Good,
    GradeC_ModerateDrift,
    GradeD_CriticalReplacementNeeded
}

/// <summary>
/// Hardware diagnostic assessment report for an analog gamepad stick.
/// </summary>
public record StickBenchmarkResult(
    double CircularityErrorPercentage,
    double MaxCircularityDeviation,
    double CenterRestDrift,
    double RestJitterStdDev,
    StickHardwareGrade Grade,
    int OuterSampleCount,
    int RestSampleCount
);

/// <summary>
/// Hardware diagnostic benchmark suite for measuring analog stick circularity error,
/// rotational boundary clipping, center resting drift, and potentiometer electronic noise.
/// </summary>
public class StickCircularityBenchmark
{
    private readonly List<(float X, float Y)> _outerSamples = new();
    private readonly List<(float X, float Y)> _restSamples = new();

    /// <summary>
    /// Resets all recorded benchmark telemetry.
    /// </summary>
    public void Reset()
    {
        _outerSamples.Clear();
        _restSamples.Clear();
    }

    /// <summary>
    /// Records a stick deflection sample along the outer physical boundary.
    /// </summary>
    public void RecordOuterSample(float x, float y)
    {
        float mag = MathF.Sqrt(x * x + y * y);
        if (mag >= 0.60f)
        {
            _outerSamples.Add((x, y));
        }
    }

    /// <summary>
    /// Records an unactuated resting stick sample to measure drift and electronic jitter.
    /// </summary>
    public void RecordRestSample(float x, float y)
    {
        _restSamples.Add((x, y));
    }

    /// <summary>
    /// Computes circularity error, maximum outlier deviation, center resting drift, and overall grade.
    /// </summary>
    public StickBenchmarkResult ComputeReport()
    {
        double circularityError = 0.0;
        double maxDeviation = 0.0;

        if (_outerSamples.Count > 0)
        {
            double sumError = 0.0;
            foreach (var (x, y) in _outerSamples)
            {
                double radius = Math.Sqrt(x * x + y * y);
                double dev = Math.Abs(radius - 1.0);
                sumError += dev;
                if (dev > maxDeviation)
                {
                    maxDeviation = dev;
                }
            }
            circularityError = (sumError / _outerSamples.Count) * 100.0;
        }

        double centerDrift = 0.0;
        double jitterStdDev = 0.0;

        if (_restSamples.Count > 0)
        {
            var magnitudes = _restSamples.Select(s => Math.Sqrt(s.X * s.X + s.Y * s.Y)).ToList();
            centerDrift = magnitudes.Average();

            if (magnitudes.Count > 1)
            {
                double variance = magnitudes.Select(m => (m - centerDrift) * (m - centerDrift)).Average();
                jitterStdDev = Math.Sqrt(variance);
            }
        }

        StickHardwareGrade grade;
        if (centerDrift <= 0.05 && circularityError <= 6.0)
        {
            grade = StickHardwareGrade.GradeA_Pristine;
        }
        else if (centerDrift <= 0.12 && circularityError <= 14.0)
        {
            grade = StickHardwareGrade.GradeB_Good;
        }
        else if (centerDrift <= 0.22)
        {
            grade = StickHardwareGrade.GradeC_ModerateDrift;
        }
        else
        {
            grade = StickHardwareGrade.GradeD_CriticalReplacementNeeded;
        }

        return new StickBenchmarkResult(
            CircularityErrorPercentage: Math.Round(circularityError, 2),
            MaxCircularityDeviation: Math.Round(maxDeviation, 3),
            CenterRestDrift: Math.Round(centerDrift, 3),
            RestJitterStdDev: Math.Round(jitterStdDev, 4),
            Grade: grade,
            OuterSampleCount: _outerSamples.Count,
            RestSampleCount: _restSamples.Count
        );
    }
}
