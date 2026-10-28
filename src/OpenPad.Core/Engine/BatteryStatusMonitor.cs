namespace OpenPad.Core.Engine;

/// <summary>
/// Power state of a connected controller.
/// </summary>
public enum GamepadPowerState
{
    Unknown = 0,
    OnBattery = 1,
    Charging = 2,
    Charged = 3,
    NoBattery = 4
}

/// <summary>
/// Severity warning level for controller battery.
/// </summary>
public enum BatteryLevelSeverity
{
    Normal = 0,
    Low = 1,
    Critical = 2
}

/// <summary>
/// Telemetry record capturing controller power metrics.
/// </summary>
public record BatteryTelemetry(
    int Slot,
    GamepadPowerState PowerState,
    int Percentage,
    BatteryLevelSeverity Severity,
    bool IsCharging
);

/// <summary>
/// Monitors wireless controller battery status, charging state, low-battery alert thresholds,
/// and inactivity timeouts for power conservation.
/// </summary>
public class BatteryStatusMonitor
{
    private readonly Dictionary<int, BatteryTelemetry> _slotTelemetry = new();
    private readonly Dictionary<int, double> _lastActivitySeconds = new();
    private readonly Dictionary<int, BatteryLevelSeverity> _lastAlertedSeverity = new();

    public int LowBatteryThresholdPercent { get; set; } = 20;
    public int CriticalBatteryThresholdPercent { get; set; } = 5;
    public double IdleSleepTimeoutMinutes { get; set; } = 10.0;

    /// <summary>
    /// Event fired when a controller transitions into Low or Critical battery state.
    /// </summary>
    public event Action<BatteryTelemetry, string>? BatteryAlertTriggered;

    /// <summary>
    /// Event fired when a controller has been idle longer than IdleSleepTimeoutMinutes.
    /// </summary>
    public event Action<int>? IdleSleepTriggered;

    /// <summary>
    /// Updates telemetry metrics for a given gamepad slot.
    /// </summary>
    public BatteryTelemetry UpdateTelemetry(int slot, GamepadPowerState state, int percentage)
    {
        int clampedPercent = Math.Clamp(percentage, 0, 100);
        bool isCharging = state == GamepadPowerState.Charging;

        BatteryLevelSeverity severity = BatteryLevelSeverity.Normal;
        if (!isCharging && state == GamepadPowerState.OnBattery)
        {
            if (clampedPercent <= CriticalBatteryThresholdPercent)
            {
                severity = BatteryLevelSeverity.Critical;
            }
            else if (clampedPercent <= LowBatteryThresholdPercent)
            {
                severity = BatteryLevelSeverity.Low;
            }
        }

        var telemetry = new BatteryTelemetry(
            Slot: slot,
            PowerState: state,
            Percentage: clampedPercent,
            Severity: severity,
            IsCharging: isCharging
        );

        _slotTelemetry[slot] = telemetry;

        // Check if transition warrants alert
        _lastAlertedSeverity.TryGetValue(slot, out var lastSeverity);
        if (severity > lastSeverity)
        {
            string message = severity == BatteryLevelSeverity.Critical
                ? $"Aviso Crítico: Bateria do controle P{slot + 1} em {clampedPercent}%! Conecte o cabo imediatamente."
                : $"Aviso: Bateria do controle P{slot + 1} baixa ({clampedPercent}%).";

            BatteryAlertTriggered?.Invoke(telemetry, message);
        }

        _lastAlertedSeverity[slot] = severity;

        return telemetry;
    }

    /// <summary>
    /// Records user interaction to reset inactivity timer.
    /// </summary>
    public void RecordInputActivity(int slot, double currentTimeSeconds)
    {
        _lastActivitySeconds[slot] = currentTimeSeconds;
    }

    /// <summary>
    /// Checks whether the controller has exceeded idle timeout.
    /// </summary>
    public bool CheckIdleTimeout(int slot, double currentTimeSeconds)
    {
        if (!_lastActivitySeconds.TryGetValue(slot, out var lastActive))
        {
            _lastActivitySeconds[slot] = currentTimeSeconds;
            return false;
        }

        double idleDuration = currentTimeSeconds - lastActive;
        double maxIdleSeconds = IdleSleepTimeoutMinutes * 60.0;

        if (idleDuration >= maxIdleSeconds)
        {
            IdleSleepTriggered?.Invoke(slot);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Returns current telemetry for a gamepad slot or null if unknown.
    /// </summary>
    public BatteryTelemetry? GetTelemetry(int slot)
    {
        return _slotTelemetry.TryGetValue(slot, out var t) ? t : null;
    }
}
