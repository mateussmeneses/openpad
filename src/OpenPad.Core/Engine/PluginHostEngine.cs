using System;
using System.Collections.Generic;
using System.Linq;
using OpenPad.Core.Models;

namespace OpenPad.Core.Engine;

/// <summary>
/// Operational state and lifecycle status of a plugin.
/// </summary>
public enum PluginState
{
    Uninitialized,
    Active,
    Disabled,
    Quarantined
}

/// <summary>
/// Context provided to plugins during initialization.
/// </summary>
public record PluginContext(
    string HostVersion,
    string ConfigDirectory,
    Action<string>? LogInfo = null,
    Action<string, Exception?>? LogError = null
);

/// <summary>
/// Diagnostic statistics tracking plugin invocations and fault resilience.
/// </summary>
public class PluginTelemetry
{
    public long TotalInvocations { get; set; }
    public int ConsecutiveFailures { get; set; }
    public int TotalFailures { get; set; }
    public DateTime? LastFailureTime { get; set; }
    public string? LastErrorMessage { get; set; }
}

/// <summary>
/// Interface that all OpenPad v2.0 plugins must implement.
/// Plugins can intercept, transform, filter, or synthesize controller inputs.
/// </summary>
public interface IOpenPadPlugin
{
    /// <summary>
    /// Unique identifier for the plugin (e.g. "com.openpad.sniper-stabilizer").
    /// </summary>
    string Id { get; }

    /// <summary>
    /// User-friendly display name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Semantic version of the plugin.
    /// </summary>
    Version Version { get; }

    /// <summary>
    /// Plugin author or organization.
    /// </summary>
    string Author { get; }

    /// <summary>
    /// Execution priority order in the input pipeline. Lower numbers execute earlier.
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Whether the plugin is currently enabled for processing.
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// Initializes plugin resources and establishes host bindings.
    /// </summary>
    void Initialize(PluginContext context);

    /// <summary>
    /// Intercepts and transforms controller snapshots in real-time.
    /// Must execute within microsecond budgets (&lt; 0.1ms).
    /// </summary>
    ControllerSnapshot ProcessSnapshot(ControllerSnapshot snapshot);

    /// <summary>
    /// Cleanly releases all unmanaged memory or background handles.
    /// </summary>
    void Shutdown();
}

/// <summary>
/// Extensible plugin host engine for OpenPad v2.0.
/// Provides high-throughput input pipeline routing, dynamic load/unload,
/// and automated zero-crash sandboxing with fault quarantine.
/// </summary>
public class PluginHostEngine
{
    private readonly Dictionary<string, (IOpenPadPlugin Plugin, PluginTelemetry Telemetry)> _plugins = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _syncLock = new();

    /// <summary>
    /// Maximum consecutive exceptions tolerated before a plugin is quarantined to protect the 1000Hz gaming loop.
    /// </summary>
    public int MaxConsecutiveFailures { get; set; } = 3;

    /// <summary>
    /// Context passed to newly registered plugins.
    /// </summary>
    public PluginContext Context { get; set; } = new("2.0.0", AppContext.BaseDirectory);

    /// <summary>
    /// Registers and initializes an OpenPad plugin.
    /// </summary>
    public void RegisterPlugin(IOpenPadPlugin plugin)
    {
        ArgumentNullException.ThrowIfNull(plugin);

        lock (_syncLock)
        {
            if (_plugins.ContainsKey(plugin.Id))
            {
                throw new InvalidOperationException($"Plugin with ID '{plugin.Id}' is already registered.");
            }

            var telemetry = new PluginTelemetry();
            plugin.Initialize(Context);
            _plugins[plugin.Id] = (plugin, telemetry);
        }
    }

    /// <summary>
    /// Unregisters and shuts down a plugin by ID.
    /// </summary>
    public bool UnregisterPlugin(string pluginId)
    {
        lock (_syncLock)
        {
            if (!_plugins.TryGetValue(pluginId, out var entry))
            {
                return false;
            }

            try
            {
                entry.Plugin.Shutdown();
            }
            catch (Exception ex)
            {
                Context.LogError?.Invoke($"Error shutting down plugin {pluginId}", ex);
            }

            _plugins.Remove(pluginId);
            return true;
        }
    }

    /// <summary>
    /// Retrieves a registered plugin by ID.
    /// </summary>
    public IOpenPadPlugin? GetPlugin(string pluginId)
    {
        lock (_syncLock)
        {
            return _plugins.TryGetValue(pluginId, out var entry) ? entry.Plugin : null;
        }
    }

    /// <summary>
    /// Retrieves diagnostic telemetry for a plugin.
    /// </summary>
    public PluginTelemetry? GetTelemetry(string pluginId)
    {
        lock (_syncLock)
        {
            return _plugins.TryGetValue(pluginId, out var entry) ? entry.Telemetry : null;
        }
    }

    /// <summary>
    /// Lists all registered plugins.
    /// </summary>
    public IReadOnlyList<IOpenPadPlugin> GetRegisteredPlugins()
    {
        lock (_syncLock)
        {
            return _plugins.Values.Select(v => v.Plugin).ToList();
        }
    }

    /// <summary>
    /// Enables or disables a plugin.
    /// </summary>
    public bool SetPluginEnabled(string pluginId, bool enabled)
    {
        lock (_syncLock)
        {
            if (!_plugins.TryGetValue(pluginId, out var entry))
            {
                return false;
            }

            entry.Plugin.IsEnabled = enabled;
            if (enabled && entry.Telemetry.ConsecutiveFailures >= MaxConsecutiveFailures)
            {
                // Reset failure counter on explicit re-enable
                entry.Telemetry.ConsecutiveFailures = 0;
            }
            return true;
        }
    }

    /// <summary>
    /// Routes a controller snapshot sequentially through all enabled, non-quarantined plugins in priority order.
    /// Protects the gaming loop by catching plugin faults and quarantining repeatedly failing plugins.
    /// </summary>
    public ControllerSnapshot ProcessPipeline(ControllerSnapshot snapshot)
    {
        List<(IOpenPadPlugin Plugin, PluginTelemetry Telemetry)> activePlugins;

        lock (_syncLock)
        {
            activePlugins = _plugins.Values
                .Where(e => e.Plugin.IsEnabled && e.Telemetry.ConsecutiveFailures < MaxConsecutiveFailures)
                .OrderBy(e => e.Plugin.Priority)
                .ToList();
        }

        var current = snapshot;

        foreach (var (plugin, telemetry) in activePlugins)
        {
            try
            {
                current = plugin.ProcessSnapshot(current);
                telemetry.TotalInvocations++;
                telemetry.ConsecutiveFailures = 0;
            }
            catch (Exception ex)
            {
                telemetry.TotalFailures++;
                telemetry.ConsecutiveFailures++;
                telemetry.LastFailureTime = DateTime.UtcNow;
                telemetry.LastErrorMessage = ex.Message;

                Context.LogError?.Invoke(
                    $"Plugin '{plugin.Name}' ({plugin.Id}) faulted during input loop (consecutive: {telemetry.ConsecutiveFailures})",
                    ex);

                if (telemetry.ConsecutiveFailures >= MaxConsecutiveFailures)
                {
                    Context.LogError?.Invoke(
                        $"Plugin '{plugin.Name}' quarantined after exceeding {MaxConsecutiveFailures} consecutive failures.",
                        null);
                }
            }
        }

        return current;
    }
}
