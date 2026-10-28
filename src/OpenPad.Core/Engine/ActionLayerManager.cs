using OpenPad.Core.Models;

namespace OpenPad.Core.Engine;

/// <summary>
/// Mode of layer activation.
/// </summary>
public enum LayerActivationMode
{
    Hold = 0,
    Toggle = 1
}

/// <summary>
/// Represents a sub-layer that overrides button mappings while active.
/// </summary>
public class ActionLayer
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "Layer";
    public GamepadButton TriggerButton { get; set; }
    public LayerActivationMode Mode { get; set; } = LayerActivationMode.Hold;
    public Dictionary<GamepadButton, InputAction> OverrideBindings { get; set; } = new();
}

/// <summary>
/// Manages dynamic action layers and shift modifiers.
/// Allows expanding physical controller buttons into multiple sets of contextual commands.
/// Ensures atomic fallback to base profile and clean key release when switching layers.
/// </summary>
public class ActionLayerManager
{
    private readonly List<ActionLayer> _layers = new();
    private readonly HashSet<GamepadButton> _activeEngagedButtons = new();
    private string? _activeLayerId;

    public IReadOnlyList<ActionLayer> Layers => _layers.AsReadOnly();
    public string? ActiveLayerId => _activeLayerId;
    public bool IsLayerActive => !string.IsNullOrEmpty(_activeLayerId);

    /// <summary>
    /// Event raised when the active layer changes, providing previous and current layer ID.
    /// </summary>
    public event Action<string?, string?>? LayerChanged;

    /// <summary>
    /// Adds an action layer.
    /// </summary>
    public void AddLayer(ActionLayer layer)
    {
        _layers.Add(layer);
    }

    /// <summary>
    /// Removes an action layer by its ID.
    /// </summary>
    public bool RemoveLayer(string layerId)
    {
        if (_activeLayerId == layerId)
        {
            SwitchLayer(null);
        }
        return _layers.RemoveAll(l => l.Id == layerId) > 0;
    }

    /// <summary>
    /// Clears all layers.
    /// </summary>
    public void ClearLayers()
    {
        SwitchLayer(null);
        _layers.Clear();
    }

    /// <summary>
    /// Processes a button state update and returns whether the button was consumed
    /// as a layer activation trigger.
    /// </summary>
    public bool ProcessTriggerEvent(GamepadButton button, bool isPressed)
    {
        var matchingLayer = _layers.FirstOrDefault(l => l.TriggerButton == button);
        if (matchingLayer == null)
        {
            return false;
        }

        if (matchingLayer.Mode == LayerActivationMode.Hold)
        {
            if (isPressed)
            {
                SwitchLayer(matchingLayer.Id);
            }
            else if (_activeLayerId == matchingLayer.Id)
            {
                SwitchLayer(null);
            }
            return true;
        }
        else if (matchingLayer.Mode == LayerActivationMode.Toggle)
        {
            if (isPressed)
            {
                // Toggle state on button press down
                if (_activeLayerId == matchingLayer.Id)
                {
                    SwitchLayer(null);
                }
                else
                {
                    SwitchLayer(matchingLayer.Id);
                }
            }
            return true;
        }

        return false;
    }

    /// <summary>
    /// Resolves the effective button action, prioritizing active layer overrides
    /// before falling back to the base profile mapping.
    /// </summary>
    public InputAction? ResolveAction(GamepadButton button, InputAction? baseAction)
    {
        if (string.IsNullOrEmpty(_activeLayerId))
        {
            return baseAction;
        }

        var activeLayer = _layers.FirstOrDefault(l => l.Id == _activeLayerId);
        if (activeLayer != null && activeLayer.OverrideBindings.TryGetValue(button, out var overrideAction))
        {
            return overrideAction;
        }

        return baseAction;
    }

    /// <summary>
    /// Switches the active layer and cleans up state.
    /// </summary>
    public void SwitchLayer(string? newLayerId)
    {
        if (_activeLayerId == newLayerId) return;

        string? previousLayerId = _activeLayerId;
        _activeLayerId = newLayerId;
        _activeEngagedButtons.Clear();

        LayerChanged?.Invoke(previousLayerId, newLayerId);
    }

    /// <summary>
    /// Resets all layers to base profile.
    /// </summary>
    public void Reset()
    {
        SwitchLayer(null);
    }
}
