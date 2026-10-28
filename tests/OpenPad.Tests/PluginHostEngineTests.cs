using System;
using System.Collections.Generic;
using NUnit.Framework;
using OpenPad.Core.Engine;
using OpenPad.Core.Models;

namespace OpenPad.Tests;

[TestFixture]
public class PluginHostEngineTests
{
    private PluginHostEngine _host = null!;

    private class TestMockPlugin : IOpenPadPlugin
    {
        public string Id { get; init; } = "mock.test";
        public string Name { get; init; } = "Mock Test Plugin";
        public Version Version { get; init; } = new(1, 0, 0);
        public string Author { get; init; } = "OpenPad Team";
        public int Priority { get; set; } = 100;
        public bool IsEnabled { get; set; } = true;

        public bool WasInitialized { get; private set; }
        public bool WasShutdown { get; private set; }
        public bool ShouldThrow { get; set; }

        public void Initialize(PluginContext context) => WasInitialized = true;
        public void Shutdown() => WasShutdown = true;

        public ControllerSnapshot ProcessSnapshot(ControllerSnapshot snapshot)
        {
            if (ShouldThrow)
            {
                throw new InvalidOperationException("Simulated plugin crash!");
            }

            // Invert Left Trigger value for verification
            var newAxes = new Dictionary<GamepadAxis, float>(snapshot.Axes)
            {
                [GamepadAxis.TriggerLeft] = 0.99f
            };

            return new ControllerSnapshot(
                snapshot.DeviceId,
                snapshot.DeviceName,
                snapshot.IsConnected,
                snapshot.Timestamp,
                (IDictionary<GamepadButton, bool>)snapshot.Buttons,
                newAxes
            );
        }
    }

    [SetUp]
    public void SetUp()
    {
        _host = new PluginHostEngine();
    }

    [Test]
    public void RegisterPlugin_InitializesPluginAndTracksInList()
    {
        var plugin = new TestMockPlugin();
        _host.RegisterPlugin(plugin);

        Assert.That(plugin.WasInitialized, Is.True);
        Assert.That(_host.GetRegisteredPlugins().Count, Is.EqualTo(1));
        Assert.That(_host.GetPlugin(plugin.Id), Is.SameAs(plugin));
    }

    [Test]
    public void ProcessPipeline_ExecutesPluginTransformation()
    {
        var plugin = new TestMockPlugin();
        _host.RegisterPlugin(plugin);

        var initial = new ControllerSnapshot(
            "dev1", "Pad", true, DateTime.UtcNow,
            new Dictionary<GamepadButton, bool>(),
            new Dictionary<GamepadAxis, float> { [GamepadAxis.TriggerLeft] = 0.0f }
        );

        var result = _host.ProcessPipeline(initial);

        Assert.That(result.GetAxis(GamepadAxis.TriggerLeft), Is.EqualTo(0.99f).Within(0.001f));
        var telemetry = _host.GetTelemetry(plugin.Id);
        Assert.That(telemetry!.TotalInvocations, Is.EqualTo(1));
    }

    [Test]
    public void ProcessPipeline_WhenPluginThrows_QuarantinesWithoutCrashingHost()
    {
        var faultyPlugin = new TestMockPlugin { Id = "faulty.plugin", ShouldThrow = true };
        _host.RegisterPlugin(faultyPlugin);
        _host.MaxConsecutiveFailures = 2;

        var initial = new ControllerSnapshot(
            "dev1", "Pad", true, DateTime.UtcNow,
            new Dictionary<GamepadButton, bool>(),
            new Dictionary<GamepadAxis, float>()
        );

        // Run 1: exception caught
        Assert.DoesNotThrow(() => _host.ProcessPipeline(initial));
        var telemetry = _host.GetTelemetry(faultyPlugin.Id);
        Assert.That(telemetry!.ConsecutiveFailures, Is.EqualTo(1));

        // Run 2: hits threshold (2) -> quarantined
        Assert.DoesNotThrow(() => _host.ProcessPipeline(initial));
        Assert.That(telemetry.ConsecutiveFailures, Is.EqualTo(2));

        // Run 3: plugin is quarantined, skipped entirely
        Assert.DoesNotThrow(() => _host.ProcessPipeline(initial));
        Assert.That(telemetry.TotalFailures, Is.EqualTo(2)); // No new failures because it was bypassed
    }

    [Test]
    public void UnregisterPlugin_InvokesShutdownCleanly()
    {
        var plugin = new TestMockPlugin();
        _host.RegisterPlugin(plugin);

        var removed = _host.UnregisterPlugin(plugin.Id);

        Assert.That(removed, Is.True);
        Assert.That(plugin.WasShutdown, Is.True);
        Assert.That(_host.GetRegisteredPlugins().Count, Is.EqualTo(0));
    }
}
