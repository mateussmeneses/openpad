using OpenPad.Core.Interfaces;
using OpenPad.Core.Models;

namespace OpenPad.Core.Testing;

/// <summary>
/// Fonte de controle controlável para testes automatizados.
/// </summary>
public class FakeControllerSource : IControllerSource
{
    private readonly List<ControllerInfo> _controllers = new();
    private bool _isRunning = false;

    public event EventHandler<ControllerInfo>? ControllerConnected;
    public event EventHandler<string>? ControllerDisconnected;
    public event EventHandler<ControllerSnapshot>? StateUpdated;

    public bool IsRunning => _isRunning;

    public IReadOnlyList<ControllerInfo> GetConnectedControllers() => _controllers.ToList();

    public void Start() => _isRunning = true;
    public void Stop() => _isRunning = false;

    public void ConnectController(string id, string name)
    {
        var info = new ControllerInfo(id, name, true);
        _controllers.RemoveAll(c => c.Id == id);
        _controllers.Add(info);
        ControllerConnected?.Invoke(this, info);
    }

    public void DisconnectController(string id)
    {
        _controllers.RemoveAll(c => c.Id == id);
        ControllerDisconnected?.Invoke(this, id);
    }

    public void EmitSnapshot(ControllerSnapshot snapshot)
    {
        StateUpdated?.Invoke(this, snapshot);
    }

    public void EmitButtonPress(string id, string name, GamepadButton button, bool isPressed)
    {
        var buttons = new Dictionary<GamepadButton, bool>();
        foreach (GamepadButton b in Enum.GetValues<GamepadButton>())
        {
            buttons[b] = false;
        }
        buttons[button] = isPressed;

        var axes = new Dictionary<GamepadAxis, float>();
        foreach (GamepadAxis a in Enum.GetValues<GamepadAxis>())
        {
            axes[a] = 0.0f;
        }

        var snapshot = new ControllerSnapshot(id, name, true, DateTime.UtcNow, buttons, axes);
        EmitSnapshot(snapshot);
    }

    public void Dispose()
    {
        Stop();
        _controllers.Clear();
    }
}
