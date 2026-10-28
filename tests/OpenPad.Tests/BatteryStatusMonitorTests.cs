using NUnit.Framework;
using OpenPad.Core.Engine;

namespace OpenPad.Tests;

[TestFixture]
public class BatteryStatusMonitorTests
{
    private BatteryStatusMonitor _monitor = null!;

    [SetUp]
    public void Setup()
    {
        _monitor = new BatteryStatusMonitor();
    }

    [Test]
    public void UpdateTelemetry_NormalLevel_SeverityNormalAndNoAlert()
    {
        bool alertFired = false;
        _monitor.BatteryAlertTriggered += (_, _) => alertFired = true;

        var telemetry = _monitor.UpdateTelemetry(0, GamepadPowerState.OnBattery, 85);

        Assert.That(telemetry.Percentage, Is.EqualTo(85));
        Assert.That(telemetry.Severity, Is.EqualTo(BatteryLevelSeverity.Normal));
        Assert.That(telemetry.IsCharging, Is.False);
        Assert.That(alertFired, Is.False);
    }

    [Test]
    public void UpdateTelemetry_LowLevel_FiresLowAlert()
    {
        string? receivedMsg = null;
        _monitor.BatteryAlertTriggered += (_, msg) => receivedMsg = msg;

        var telemetry = _monitor.UpdateTelemetry(0, GamepadPowerState.OnBattery, 15);

        Assert.That(telemetry.Severity, Is.EqualTo(BatteryLevelSeverity.Low));
        Assert.That(receivedMsg, Is.Not.Null);
        Assert.That(receivedMsg, Does.Contain("baixa"));
    }

    [Test]
    public void UpdateTelemetry_CriticalLevel_FiresCriticalAlert()
    {
        string? receivedMsg = null;
        _monitor.BatteryAlertTriggered += (_, msg) => receivedMsg = msg;

        var telemetry = _monitor.UpdateTelemetry(1, GamepadPowerState.OnBattery, 4);

        Assert.That(telemetry.Severity, Is.EqualTo(BatteryLevelSeverity.Critical));
        Assert.That(receivedMsg, Is.Not.Null);
        Assert.That(receivedMsg, Does.Contain("Crítico"));
    }

    [Test]
    public void UpdateTelemetry_ChargingState_SuppressesLowBatteryAlert()
    {
        bool alertFired = false;
        _monitor.BatteryAlertTriggered += (_, _) => alertFired = true;

        var telemetry = _monitor.UpdateTelemetry(0, GamepadPowerState.Charging, 10);

        Assert.That(telemetry.IsCharging, Is.True);
        Assert.That(telemetry.Severity, Is.EqualTo(BatteryLevelSeverity.Normal));
        Assert.That(alertFired, Is.False);
    }

    [Test]
    public void CheckIdleTimeout_WhenExceedingTimeout_ReturnsTrueAndTriggersEvent()
    {
        _monitor.IdleSleepTimeoutMinutes = 5.0; // 300 seconds
        int? timedOutSlot = null;
        _monitor.IdleSleepTriggered += (slot) => timedOutSlot = slot;

        // Initialize activity at t = 1000s
        _monitor.RecordInputActivity(0, 1000.0);

        // At t = 1200s (200s idle, < 300s) -> False
        bool isIdle = _monitor.CheckIdleTimeout(0, 1200.0);
        Assert.That(isIdle, Is.False);
        Assert.That(timedOutSlot, Is.Null);

        // At t = 1350s (350s idle, > 300s) -> True
        isIdle = _monitor.CheckIdleTimeout(0, 1350.0);
        Assert.That(isIdle, Is.True);
        Assert.That(timedOutSlot, Is.EqualTo(0));
    }

    [Test]
    public void RecordInputActivity_ResetsIdleDuration()
    {
        _monitor.IdleSleepTimeoutMinutes = 5.0;
        _monitor.RecordInputActivity(0, 1000.0);

        // User interacts at t = 1250s
        _monitor.RecordInputActivity(0, 1250.0);

        // At t = 1350s (only 100s since last interaction) -> Not idle
        bool isIdle = _monitor.CheckIdleTimeout(0, 1350.0);
        Assert.That(isIdle, Is.False);
    }
}
