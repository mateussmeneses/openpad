using OpenPad.Core.Models;

namespace OpenPad.Core.Engine;

/// <summary>
/// Definition of a simultaneous multi-button chord mapping.
/// </summary>
public class ButtonChord
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "Chord";
    public HashSet<GamepadButton> Buttons { get; set; } = new();
    public InputAction OutputAction { get; set; } = InputAction.None();
}

/// <summary>
/// Engine for detecting and synthesizing simultaneous button combos (chords)
/// with microsecond tolerance windows, preventing premature single-button accidental activations.
/// </summary>
public class ChordingEngine
{
    private readonly List<ButtonChord> _chords = new();
    private readonly HashSet<GamepadButton> _currentlyPressed = new();
    private readonly Dictionary<GamepadButton, double> _pendingPressTimers = new();
    private readonly HashSet<GamepadButton> _consumedByActiveChord = new();
    private ButtonChord? _activeEngagedChord;

    public double ToleranceWindowSeconds { get; set; } = 0.045; // 45 milliseconds
    public IReadOnlyList<ButtonChord> Chords => _chords.AsReadOnly();
    public bool IsChordEngaged => _activeEngagedChord != null;
    public ButtonChord? ActiveChord => _activeEngagedChord;

    /// <summary>
    /// Event fired when a chord combination is successfully completed within tolerance.
    /// </summary>
    public event Action<ButtonChord>? ChordActivated;

    /// <summary>
    /// Event fired when an active chord is released.
    /// </summary>
    public event Action<ButtonChord>? ChordReleased;

    /// <summary>
    /// Event fired when a single button press exceeds tolerance without forming a chord,
    /// flushing it as a standard isolated input.
    /// </summary>
    public event Action<GamepadButton>? SingleButtonFlushed;

    public void RegisterChord(ButtonChord chord)
    {
        _chords.Add(chord);
    }

    public void ClearChords()
    {
        Reset();
        _chords.Clear();
    }

    /// <summary>
    /// Handles physical button state changes.
    /// </summary>
    public void ProcessButton(GamepadButton button, bool isPressed)
    {
        if (isPressed)
        {
            _currentlyPressed.Add(button);

            // Check if any chord utilizes this button
            bool isPartOfAnyChord = _chords.Any(c => c.Buttons.Contains(button));

            if (!isPartOfAnyChord)
            {
                // Not part of any chord: flush immediately
                SingleButtonFlushed?.Invoke(button);
                return;
            }

            // Start tolerance timer for potential chord completion
            _pendingPressTimers[button] = 0.0;

            // Check if all buttons of an existing chord are now pressed
            CheckForChordMatch();
        }
        else
        {
            _currentlyPressed.Remove(button);
            _pendingPressTimers.Remove(button);

            if (_activeEngagedChord != null && _activeEngagedChord.Buttons.Contains(button))
            {
                // Release active chord
                var released = _activeEngagedChord;
                _activeEngagedChord = null;
                _consumedByActiveChord.Clear();
                ChordReleased?.Invoke(released);
            }

            _consumedByActiveChord.Remove(button);
        }
    }

    /// <summary>
    /// Advances frame time, evaluating pending chord candidate buttons.
    /// </summary>
    public void Update(double deltaTimeSeconds)
    {
        if (_pendingPressTimers.Count == 0) return;

        var expiredButtons = new List<GamepadButton>();

        var keys = _pendingPressTimers.Keys.ToList();
        foreach (var btn in keys)
        {
            _pendingPressTimers[btn] += deltaTimeSeconds;
            if (_pendingPressTimers[btn] >= ToleranceWindowSeconds)
            {
                expiredButtons.Add(btn);
            }
        }

        foreach (var expiredBtn in expiredButtons)
        {
            _pendingPressTimers.Remove(expiredBtn);
            // Only flush if not already part of an engaged chord
            if (!_consumedByActiveChord.Contains(expiredBtn))
            {
                SingleButtonFlushed?.Invoke(expiredBtn);
            }
        }
    }

    private void CheckForChordMatch()
    {
        if (_activeEngagedChord != null) return;

        foreach (var chord in _chords)
        {
            if (chord.Buttons.Count > 1 && chord.Buttons.IsSubsetOf(_currentlyPressed))
            {
                // Full chord matched!
                _activeEngagedChord = chord;
                foreach (var b in chord.Buttons)
                {
                    _consumedByActiveChord.Add(b);
                    _pendingPressTimers.Remove(b);
                }

                ChordActivated?.Invoke(chord);
                return;
            }
        }
    }

    /// <summary>
    /// Resets all internal state and buffers.
    /// </summary>
    public void Reset()
    {
        if (_activeEngagedChord != null)
        {
            var chord = _activeEngagedChord;
            _activeEngagedChord = null;
            ChordReleased?.Invoke(chord);
        }

        _currentlyPressed.Clear();
        _pendingPressTimers.Clear();
        _consumedByActiveChord.Clear();
    }
}
