using OpenPad.Core.Interfaces;
using OpenPad.Core.Models;

namespace OpenPad.Core.Testing;

public enum InputEventType
{
    KeyDown,
    KeyUp,
    MouseDown,
    MouseUp,
    ReleaseAll
}

public record RecordedInputEvent(InputEventType EventType, VirtualKey? Key, MouseButton? MouseButton, DateTime Timestamp);

/// <summary>
/// Sink gravador de eventos de entrada para validação em testes unitários.
/// </summary>
public class RecordingInputSink : IInputSink
{
    private readonly List<RecordedInputEvent> _events = new();
    private readonly HashSet<VirtualKey> _pressedKeys = new();
    private readonly HashSet<MouseButton> _pressedMouseButtons = new();
    private readonly object _lock = new();

    public IReadOnlyList<RecordedInputEvent> RecordedEvents
    {
        get
        {
            lock (_lock) return _events.ToList();
        }
    }

    public IReadOnlySet<VirtualKey> CurrentlyPressedKeys
    {
        get
        {
            lock (_lock) return new HashSet<VirtualKey>(_pressedKeys);
        }
    }

    public IReadOnlySet<MouseButton> CurrentlyPressedMouseButtons
    {
        get
        {
            lock (_lock) return new HashSet<MouseButton>(_pressedMouseButtons);
        }
    }

    public void SendKeyDown(VirtualKey key)
    {
        lock (_lock)
        {
            _events.Add(new RecordedInputEvent(InputEventType.KeyDown, key, null, DateTime.UtcNow));
            _pressedKeys.Add(key);
        }
    }

    public void SendKeyUp(VirtualKey key)
    {
        lock (_lock)
        {
            _events.Add(new RecordedInputEvent(InputEventType.KeyUp, key, null, DateTime.UtcNow));
            _pressedKeys.Remove(key);
        }
    }

    public void SendMouseDown(MouseButton button)
    {
        lock (_lock)
        {
            _events.Add(new RecordedInputEvent(InputEventType.MouseDown, null, button, DateTime.UtcNow));
            _pressedMouseButtons.Add(button);
        }
    }

    public void SendMouseUp(MouseButton button)
    {
        lock (_lock)
        {
            _events.Add(new RecordedInputEvent(InputEventType.MouseUp, null, button, DateTime.UtcNow));
            _pressedMouseButtons.Remove(button);
        }
    }

    public void ReleaseAll()
    {
        lock (_lock)
        {
            _events.Add(new RecordedInputEvent(InputEventType.ReleaseAll, null, null, DateTime.UtcNow));
            _pressedKeys.Clear();
            _pressedMouseButtons.Clear();
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _events.Clear();
            _pressedKeys.Clear();
            _pressedMouseButtons.Clear();
        }
    }

    public void Dispose()
    {
        ReleaseAll();
    }
}
