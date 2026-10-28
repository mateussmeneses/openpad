using OpenPad.Core.Interfaces;
using OpenPad.Core.Models;

namespace OpenPad.Core.Testing;

public enum InputEventType
{
    KeyDown,
    KeyUp,
    MouseDown,
    MouseUp,
    MouseMove,
    ReleaseAll
}

public record RecordedInputEvent(
    InputEventType EventType,
    VirtualKey? Key,
    MouseButton? MouseButton,
    DateTime Timestamp,
    int DeltaX = 0,
    int DeltaY = 0);

/// <summary>
/// Sink gravador de eventos de entrada para validação em testes unitários.
/// </summary>
public class RecordingInputSink : IInputSink
{
    private readonly List<RecordedInputEvent> _events = new();
    private readonly HashSet<VirtualKey> _pressedKeys = new();
    private readonly HashSet<MouseButton> _pressedMouseButtons = new();
    private readonly HashSet<VirtualKey> _pendingReleaseKeys = new();
    private readonly HashSet<MouseButton> _pendingReleaseMouseButtons = new();
    private readonly object _lock = new();

    public bool SimulateFailures { get; set; } = false;

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

    public IReadOnlySet<VirtualKey> PendingReleaseKeys
    {
        get
        {
            lock (_lock) return new HashSet<VirtualKey>(_pendingReleaseKeys);
        }
    }

    public IReadOnlySet<MouseButton> PendingReleaseMouseButtons
    {
        get
        {
            lock (_lock) return new HashSet<MouseButton>(_pendingReleaseMouseButtons);
        }
    }

    public bool SendKeyDown(VirtualKey key)
    {
        lock (_lock)
        {
            _events.Add(new RecordedInputEvent(InputEventType.KeyDown, key, null, DateTime.UtcNow));
            if (SimulateFailures) return false;
            _pressedKeys.Add(key);
            return true;
        }
    }

    public bool SendKeyUp(VirtualKey key)
    {
        lock (_lock)
        {
            _events.Add(new RecordedInputEvent(InputEventType.KeyUp, key, null, DateTime.UtcNow));
            if (SimulateFailures)
            {
                _pendingReleaseKeys.Add(key);
                return false;
            }
            _pressedKeys.Remove(key);
            _pendingReleaseKeys.Remove(key);
            return true;
        }
    }

    public bool SendMouseDown(MouseButton button)
    {
        lock (_lock)
        {
            _events.Add(new RecordedInputEvent(InputEventType.MouseDown, null, button, DateTime.UtcNow));
            if (SimulateFailures) return false;
            _pressedMouseButtons.Add(button);
            return true;
        }
    }

    public bool SendMouseUp(MouseButton button)
    {
        lock (_lock)
        {
            _events.Add(new RecordedInputEvent(InputEventType.MouseUp, null, button, DateTime.UtcNow));
            if (SimulateFailures)
            {
                _pendingReleaseMouseButtons.Add(button);
                return false;
            }
            _pressedMouseButtons.Remove(button);
            _pendingReleaseMouseButtons.Remove(button);
            return true;
        }
    }

    public bool SendMouseMove(int dx, int dy)
    {
        lock (_lock)
        {
            _events.Add(new RecordedInputEvent(InputEventType.MouseMove, null, null, DateTime.UtcNow, dx, dy));
            return !SimulateFailures;
        }
    }

    public bool ReleaseAll()
    {
        lock (_lock)
        {
            _events.Add(new RecordedInputEvent(InputEventType.ReleaseAll, null, null, DateTime.UtcNow));
            if (SimulateFailures)
            {
                foreach (var k in _pressedKeys) _pendingReleaseKeys.Add(k);
                foreach (var b in _pressedMouseButtons) _pendingReleaseMouseButtons.Add(b);
                return false;
            }
            _pressedKeys.Clear();
            _pressedMouseButtons.Clear();
            _pendingReleaseKeys.Clear();
            _pendingReleaseMouseButtons.Clear();
            return true;
        }
    }

    public int RetryPendingReleases()
    {
        lock (_lock)
        {
            if (SimulateFailures) return 0;
            int count = _pendingReleaseKeys.Count + _pendingReleaseMouseButtons.Count;
            _pressedKeys.ExceptWith(_pendingReleaseKeys);
            _pressedMouseButtons.ExceptWith(_pendingReleaseMouseButtons);
            _pendingReleaseKeys.Clear();
            _pendingReleaseMouseButtons.Clear();
            return count;
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _events.Clear();
            _pressedKeys.Clear();
            _pressedMouseButtons.Clear();
            _pendingReleaseKeys.Clear();
            _pendingReleaseMouseButtons.Clear();
        }
    }

    public void Dispose()
    {
        ReleaseAll();
    }
}
