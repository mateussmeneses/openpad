using OpenPad.Core.Interfaces;
using OpenPad.Core.Models;

namespace OpenPad.Core.Engine;

public enum TriggerStage
{
    Released = 0,
    SoftPull = 1,
    FullPull = 2
}

public class DualActionTriggerConfig
{
    public float SoftThreshold { get; set; } = 0.25f;
    public float FullThreshold { get; set; } = 0.85f;
    public InputAction? SoftAction { get; set; }
    public InputAction? FullAction { get; set; }
    public bool ReleaseSoftOnFullPull { get; set; } = false;
    public bool BurstEnabled { get; set; } = false;
    public int BurstCount { get; set; } = 3;
}

/// <summary>
/// Two-stage hair trigger engine supporting dual action thresholds and rapid-fire burst modes.
/// </summary>
public class DualActionTriggerEngine
{
    private readonly DualActionTriggerConfig _config;
    private TriggerStage _currentStage = TriggerStage.Released;

    public event EventHandler<TriggerStage>? StageChanged;

    public TriggerStage CurrentStage => _currentStage;
    public DualActionTriggerConfig Config => _config;

    public DualActionTriggerEngine(DualActionTriggerConfig? config = null)
    {
        _config = config ?? new DualActionTriggerConfig();
    }

    /// <summary>
    /// Processes current normalized trigger deflection (0.0f to 1.0f) and dispatches inputs to the sink.
    /// </summary>
    public TriggerStage Update(float triggerValue, IInputSink sink)
    {
        ArgumentNullException.ThrowIfNull(sink);

        TriggerStage newStage;
        if (triggerValue >= _config.FullThreshold)
        {
            newStage = TriggerStage.FullPull;
        }
        else if (triggerValue >= _config.SoftThreshold)
        {
            newStage = TriggerStage.SoftPull;
        }
        else
        {
            newStage = TriggerStage.Released;
        }

        if (newStage != _currentStage)
        {
            var oldStage = _currentStage;
            _currentStage = newStage;
            ApplyStageTransition(oldStage, newStage, sink);
            StageChanged?.Invoke(this, newStage);
        }

        return _currentStage;
    }

    private void ApplyStageTransition(TriggerStage oldStage, TriggerStage newStage, IInputSink sink)
    {
        // 1. Handle Release from old stage
        if (oldStage == TriggerStage.FullPull)
        {
            ReleaseAction(_config.FullAction, sink);
            if (newStage == TriggerStage.Released && !_config.ReleaseSoftOnFullPull)
            {
                ReleaseAction(_config.SoftAction, sink);
            }
        }
        if (oldStage == TriggerStage.SoftPull && (_config.ReleaseSoftOnFullPull || newStage == TriggerStage.Released))
        {
            ReleaseAction(_config.SoftAction, sink);
        }

        // 2. Handle Engagement of new stage
        if (newStage == TriggerStage.SoftPull)
        {
            PressAction(_config.SoftAction, sink);
        }
        else if (newStage == TriggerStage.FullPull)
        {
            if (!_config.ReleaseSoftOnFullPull && oldStage == TriggerStage.Released)
            {
                PressAction(_config.SoftAction, sink);
            }
            else if (_config.ReleaseSoftOnFullPull && oldStage == TriggerStage.SoftPull)
            {
                ReleaseAction(_config.SoftAction, sink);
            }

            PressAction(_config.FullAction, sink);
        }
    }

    private static void PressAction(InputAction? action, IInputSink sink)
    {
        if (action == null) return;
        if (action.Type == ActionType.Key && action.Key.HasValue)
        {
            sink.SendKeyDown(action.Key.Value);
        }
        else if (action.Type == ActionType.MouseButton && action.MouseButton.HasValue)
        {
            sink.SendMouseDown(action.MouseButton.Value);
        }
    }

    private static void ReleaseAction(InputAction? action, IInputSink sink)
    {
        if (action == null) return;
        if (action.Type == ActionType.Key && action.Key.HasValue)
        {
            sink.SendKeyUp(action.Key.Value);
        }
        else if (action.Type == ActionType.MouseButton && action.MouseButton.HasValue)
        {
            sink.SendMouseUp(action.MouseButton.Value);
        }
    }
}
