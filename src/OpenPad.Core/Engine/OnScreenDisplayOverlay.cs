using System;
using System.Globalization;

namespace OpenPad.Core.Engine;

/// <summary>
/// Screen positioning anchors for the on-screen display overlay.
/// </summary>
public enum OsdPosition
{
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight,
    CenterTop
}

/// <summary>
/// Severity level of the overlay notification.
/// </summary>
public enum OsdNotificationLevel
{
    Info,
    Warning,
    Critical
}

/// <summary>
/// Active transient notification rendered on the HUD.
/// </summary>
public record OsdNotification(
    string Message,
    OsdNotificationLevel Level,
    DateTime Timestamp,
    TimeSpan Duration
)
{
    public bool IsExpired(DateTime now) => now - Timestamp > Duration;
}

/// <summary>
/// Snapshot frame containing all current HUD metrics and telemetry to be drawn.
/// </summary>
public record OsdFrame(
    string ActiveProfile,
    string ActiveLayer,
    int BatteryPercent,
    bool IsCharging,
    double PollingRateHz,
    double LatencyMs,
    string? ActiveNotification,
    OsdNotificationLevel? NotificationLevel,
    OsdPosition Position
);

/// <summary>
/// Telemetry synthesizer and notification manager for in-game and desktop On-Screen Display (OSD) overlays.
/// Visualizes active profile, layer switches, battery life, input polling frequency, and latency.
/// </summary>
public class OnScreenDisplayOverlay
{
    private OsdNotification? _currentNotification;

    public OsdPosition Position { get; set; } = OsdPosition.TopRight;
    public string ActiveProfile { get; set; } = "Default";
    public string ActiveLayer { get; set; } = "Base";
    public int BatteryPercent { get; set; } = 100;
    public bool IsCharging { get; set; }
    public double PollingRateHz { get; set; } = 1000.0;
    public double LatencyMs { get; set; } = 1.0;

    /// <summary>
    /// Updates hardware telemetry and input profiling metrics.
    /// </summary>
    public void UpdateTelemetry(
        string profile,
        string layer,
        int batteryPercent,
        bool isCharging,
        double pollingRateHz,
        double latencyMs)
    {
        ActiveProfile = profile;
        ActiveLayer = layer;
        BatteryPercent = Math.Clamp(batteryPercent, 0, 100);
        IsCharging = isCharging;
        PollingRateHz = pollingRateHz;
        LatencyMs = latencyMs;
    }

    /// <summary>
    /// Displays a timed notification toast on the overlay.
    /// </summary>
    public void PostNotification(
        string message,
        OsdNotificationLevel level = OsdNotificationLevel.Info,
        TimeSpan? duration = null,
        DateTime? now = null)
    {
        var timestamp = now ?? DateTime.UtcNow;
        var displayDuration = duration ?? TimeSpan.FromSeconds(3);
        _currentNotification = new OsdNotification(message, level, timestamp, displayDuration);
    }

    /// <summary>
    /// Clears any currently active notification banner.
    /// </summary>
    public void ClearNotification()
    {
        _currentNotification = null;
    }

    /// <summary>
    /// Generates a point-in-time render snapshot, automatically pruning expired banners.
    /// </summary>
    public OsdFrame GetCurrentFrame(DateTime now)
    {
        if (_currentNotification != null && _currentNotification.IsExpired(now))
        {
            _currentNotification = null;
        }

        return new OsdFrame(
            ActiveProfile: ActiveProfile,
            ActiveLayer: ActiveLayer,
            BatteryPercent: BatteryPercent,
            IsCharging: IsCharging,
            PollingRateHz: PollingRateHz,
            LatencyMs: LatencyMs,
            ActiveNotification: _currentNotification?.Message,
            NotificationLevel: _currentNotification?.Level,
            Position: Position
        );
    }

    /// <summary>
    /// Formats a single-line text summary suitable for compact status bars or console logs.
    /// </summary>
    public string FormatCompactHud(DateTime now)
    {
        var frame = GetCurrentFrame(now);
        var chargingTag = frame.IsCharging ? " [CHG]" : "";
        var baseHud = $"[{frame.ActiveProfile} | {frame.ActiveLayer}] {frame.BatteryPercent}%{chargingTag} | {frame.PollingRateHz.ToString("F0", CultureInfo.InvariantCulture)}Hz ({frame.LatencyMs.ToString("F1", CultureInfo.InvariantCulture)}ms)";

        if (frame.ActiveNotification != null)
        {
            var levelTag = frame.NotificationLevel switch
            {
                OsdNotificationLevel.Critical => "CRITICAL",
                OsdNotificationLevel.Warning => "WARN",
                _ => "INFO"
            };
            return $"[{levelTag}: {frame.ActiveNotification}] {baseHud}";
        }

        return baseHud;
    }
}
