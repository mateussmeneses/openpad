using NUnit.Framework;
using OpenPad.Core.Engine;

namespace OpenPad.Tests;

[TestFixture]
public class TriggerActuationCurveEngineTests
{
    private TriggerActuationCurveEngine _engine = null!;

    [SetUp]
    public void SetUp()
    {
        _engine = new TriggerActuationCurveEngine();
    }

    [Test]
    public void ProcessTrigger_WithinPreTravelDeadzone_ReturnsZero()
    {
        var config = new TriggerCurveConfig
        {
            PreTravelDeadzone = 0.10f
        };

        var output = _engine.ProcessTrigger(0.08f, config);
        Assert.That(output, Is.EqualTo(0f));
    }

    [Test]
    public void ProcessTrigger_InstantDigitalClick_ReachesFullOutputAtHairpin()
    {
        var config = new TriggerCurveConfig
        {
            Type = TriggerCurveType.InstantDigitalClick,
            PreTravelDeadzone = 0.02f,
            HairpinThreshold = 0.05f
        };

        // Below hairpin -> 0
        Assert.That(_engine.ProcessTrigger(0.04f, config), Is.EqualTo(0f));

        // At or above hairpin -> 1.0
        Assert.That(_engine.ProcessTrigger(0.06f, config), Is.EqualTo(1.0f));
        Assert.That(_engine.ProcessTrigger(0.80f, config), Is.EqualTo(1.0f));
    }

    [Test]
    public void ProcessTrigger_BeyondEndStopClamp_ClampsToOne()
    {
        var config = new TriggerCurveConfig
        {
            PreTravelDeadzone = 0.0f,
            EndStopClamp = 0.90f,
            Type = TriggerCurveType.Linear
        };

        var output = _engine.ProcessTrigger(0.92f, config);
        Assert.That(output, Is.EqualTo(1.0f));
    }

    [Test]
    public void ProcessTrigger_AggressiveCurve_ProvidesBoostedInitialResponse()
    {
        var configLinear = new TriggerCurveConfig { PreTravelDeadzone = 0f, EndStopClamp = 1f, Type = TriggerCurveType.Linear };
        var configAggressive = new TriggerCurveConfig { PreTravelDeadzone = 0f, EndStopClamp = 1f, Type = TriggerCurveType.Aggressive };

        float outLinear = _engine.ProcessTrigger(0.25f, configLinear);       // 0.25
        float outAggressive = _engine.ProcessTrigger(0.25f, configAggressive); // sqrt(0.25) = 0.50

        Assert.That(outAggressive, Is.GreaterThan(outLinear));
        Assert.That(outAggressive, Is.EqualTo(0.50f).Within(0.01f));
    }

    [Test]
    public void ProcessTrigger_SteppedDualZone_ScalesStagesIndependently()
    {
        var config = new TriggerCurveConfig
        {
            PreTravelDeadzone = 0f,
            EndStopClamp = 1f,
            Type = TriggerCurveType.SteppedDualZone,
            StepThreshold = 0.50f
        };

        // Halfway through stage 1 (0.25 pull) -> 0.25 output (half of 0.5)
        float stage1 = _engine.ProcessTrigger(0.25f, config);
        Assert.That(stage1, Is.EqualTo(0.25f).Within(0.01f));

        // Exactly at step (0.50 pull) -> 0.50 output
        float atStep = _engine.ProcessTrigger(0.50f, config);
        Assert.That(atStep, Is.EqualTo(0.50f).Within(0.01f));

        // Full pull (1.00 pull) -> 1.00 output
        float full = _engine.ProcessTrigger(1.00f, config);
        Assert.That(full, Is.EqualTo(1.00f).Within(0.01f));
    }
}
