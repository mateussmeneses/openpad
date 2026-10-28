using System;
using NUnit.Framework;
using OpenPad.Core.Engine;

namespace OpenPad.Tests;

[TestFixture]
public class OnScreenDisplayOverlayTests
{
    private OnScreenDisplayOverlay _overlay = null!;

    [SetUp]
    public void SetUp()
    {
        _overlay = new OnScreenDisplayOverlay();
    }

    [Test]
    public void FormatCompactHud_RendersDefaultTelemetryCorrectly()
    {
        var now = new DateTime(2026, 10, 28, 12, 0, 0, DateTimeKind.Utc);
        _overlay.UpdateTelemetry("ApexLegends", "SniperZoom", 85, false, 1000.0, 0.8);

        var hud = _overlay.FormatCompactHud(now);

        Assert.That(hud, Does.Contain("ApexLegends"));
        Assert.That(hud, Does.Contain("SniperZoom"));
        Assert.That(hud, Does.Contain("85%"));
        Assert.That(hud, Does.Contain("1000Hz"));
        Assert.That(hud, Does.Contain("0.8ms"));
    }

    [Test]
    public void PostNotification_ShowsInFrameUntilExpired()
    {
        var start = new DateTime(2026, 10, 28, 12, 0, 0, DateTimeKind.Utc);
        _overlay.PostNotification("Profile Switched: Halo", OsdNotificationLevel.Info, TimeSpan.FromSeconds(5), start);

        // Within duration: notification present
        var midTime = start.AddSeconds(2);
        var activeFrame = _overlay.GetCurrentFrame(midTime);
        Assert.That(activeFrame.ActiveNotification, Is.EqualTo("Profile Switched: Halo"));
        Assert.That(activeFrame.NotificationLevel, Is.EqualTo(OsdNotificationLevel.Info));

        // After duration: notification expired and pruned
        var expiredTime = start.AddSeconds(6);
        var expiredFrame = _overlay.GetCurrentFrame(expiredTime);
        Assert.That(expiredFrame.ActiveNotification, Is.Null);
    }

    [Test]
    public void FormatCompactHud_WithActiveNotification_PrefixesSeverityBanner()
    {
        var now = new DateTime(2026, 10, 28, 12, 0, 0, DateTimeKind.Utc);
        _overlay.UpdateTelemetry("Cyberpunk", "Combat", 15, false, 500.0, 2.1);
        _overlay.PostNotification("Low Battery: 15%", OsdNotificationLevel.Warning, TimeSpan.FromSeconds(10), now);

        var hud = _overlay.FormatCompactHud(now);

        Assert.That(hud, Does.StartWith("[WARN: Low Battery: 15%]"));
        Assert.That(hud, Does.Contain("Cyberpunk"));
        Assert.That(hud, Does.Contain("15%"));
    }

    [Test]
    public void ClearNotification_ImmediatelyRemovesBanner()
    {
        var now = DateTime.UtcNow;
        _overlay.PostNotification("Critical Alert", OsdNotificationLevel.Critical, TimeSpan.FromMinutes(1), now);

        _overlay.ClearNotification();

        var frame = _overlay.GetCurrentFrame(now);
        Assert.That(frame.ActiveNotification, Is.Null);
    }
}
