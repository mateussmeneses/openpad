using OpenPad.Core.Interfaces;
using OpenPad.Core.Models;

namespace OpenPad.Core.Engine;

/// <summary>
/// Motor de remapeamento desacoplado de UI e hardware.
/// Converte eventos semânticos do controle em comandos do InputSink.
/// Garante que o sistema inicie desarmado e que nenhum input permaneça
/// pressionado ao desarmar, desconectar, trocar perfil ou encerrar.
/// Suporta botões, gatilhos analógicos e movimentação de mouse por stick analógico.
/// </summary>
public class MappingEngine : IDisposable
{
    private readonly IInputSink _inputSink;
    private readonly object _syncLock = new();
    private bool _isArmed = false;
    private MappingProfile _profile;
    private string? _activeDeviceId;
    private bool _disposed = false;

    private readonly Dictionary<GamepadButton, bool> _previousButtonStates = new();
    private readonly Dictionary<GamepadAxis, bool> _previousTriggerStates = new();
    private readonly HashSet<VirtualKey> _activeVirtualKeys = new();
    private readonly HashSet<MouseButton> _activeMouseButtons = new();

    public event EventHandler<bool>? ArmedStateChanged;
    public event EventHandler<MappingProfile>? ProfileChanged;
    public event EventHandler<string>? DiagnosticsLogged;

    public bool IsArmed
    {
        get
        {
            lock (_syncLock) return _isArmed;
        }
    }

    public MappingProfile CurrentProfile
    {
        get
        {
            lock (_syncLock) return _profile;
        }
    }

    public string? ActiveDeviceId
    {
        get
        {
            lock (_syncLock) return _activeDeviceId;
        }
    }

    public IReadOnlySet<VirtualKey> ActiveVirtualKeys
    {
        get
        {
            lock (_syncLock) return new HashSet<VirtualKey>(_activeVirtualKeys);
        }
    }

    public IReadOnlySet<MouseButton> ActiveMouseButtons
    {
        get
        {
            lock (_syncLock) return new HashSet<MouseButton>(_activeMouseButtons);
        }
    }

    public MappingEngine(IInputSink inputSink, MappingProfile? initialProfile = null)
    {
        _inputSink = inputSink ?? throw new ArgumentNullException(nameof(inputSink));
        _profile = initialProfile ?? MappingProfile.CreateDefault();
    }

    public void Arm()
    {
        lock (_syncLock)
        {
            if (_isArmed) return;
            _isArmed = true;
            LogDiagnostics("Motor de remapeamento ATIVADO (ARMED).");
        }
        ArmedStateChanged?.Invoke(this, true);
    }

    public void Disarm()
    {
        lock (_syncLock)
        {
            if (!_isArmed)
            {
                ReleaseHeldInputsInternal();
                return;
            }
            _isArmed = false;
            ReleaseHeldInputsInternal();
            LogDiagnostics("Motor de remapeamento DESARMADO (DISARMED). Todos os inputs liberados.");
        }
        ArmedStateChanged?.Invoke(this, false);
    }

    public void ToggleArmed()
    {
        if (IsArmed) Disarm();
        else Arm();
    }

    public void SetProfile(MappingProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        lock (_syncLock)
        {
            ReleaseHeldInputsInternal();
            _profile = profile;
            LogDiagnostics($"Perfil alterado para: {profile.Name}. Inputs liberados.");
        }
        ProfileChanged?.Invoke(this, profile);
    }

    public void HandleDeviceDisconnected(string deviceId)
    {
        lock (_syncLock)
        {
            if (_activeDeviceId == deviceId || string.IsNullOrEmpty(_activeDeviceId))
            {
                ReleaseHeldInputsInternal();
                _activeDeviceId = null;
                LogDiagnostics($"Controle desconectado: {deviceId}. Inputs liberados por segurança.");
            }
        }
    }

    public void ProcessSnapshot(ControllerSnapshot snapshot)
    {
        if (snapshot == null) return;

        lock (_syncLock)
        {
            if (_disposed) return;

            if (!snapshot.IsConnected)
            {
                if (_activeDeviceId == snapshot.DeviceId || _activeVirtualKeys.Count > 0 || _activeMouseButtons.Count > 0)
                {
                    ReleaseHeldInputsInternal();
                    _activeDeviceId = null;
                }
                return;
            }

            _activeDeviceId = snapshot.DeviceId;

            // Se desarmado, apenas rastreamos estados anteriores para evitar disparo fantasma ao armar
            if (!_isArmed)
            {
                foreach (var (button, isPressed) in snapshot.Buttons)
                {
                    _previousButtonStates[button] = isPressed;
                }
                _previousTriggerStates[GamepadAxis.TriggerLeft] = snapshot.GetAxis(GamepadAxis.TriggerLeft) >= _profile.TriggerThreshold;
                _previousTriggerStates[GamepadAxis.TriggerRight] = snapshot.GetAxis(GamepadAxis.TriggerRight) >= _profile.TriggerThreshold;
                return;
            }

            // 1. Processar botões digitais
            foreach (var (button, action) in _profile.ButtonBindings)
            {
                if (action.Type == ActionType.None) continue;

                var isPressed = snapshot.IsButtonPressed(button);
                _previousButtonStates.TryGetValue(button, out var wasPressed);

                if (!wasPressed && isPressed)
                {
                    DispatchActionDown(action);
                }
                else if (wasPressed && !isPressed)
                {
                    DispatchActionUp(action);
                }

                _previousButtonStates[button] = isPressed;
            }

            // 2. Processar gatilhos analógicos (LT e RT)
            ProcessTriggerAxis(snapshot, GamepadAxis.TriggerLeft);
            ProcessTriggerAxis(snapshot, GamepadAxis.TriggerRight);

            // 3. Processar movimentação de mouse por stick analógico
            ProcessStickMouseMovement(snapshot);
        }
    }

    private void ProcessTriggerAxis(ControllerSnapshot snapshot, GamepadAxis axis)
    {
        if (!_profile.TriggerBindings.TryGetValue(axis, out var action) || action.Type == ActionType.None)
        {
            return;
        }

        float value = snapshot.GetAxis(axis);
        bool isPressed = value >= _profile.TriggerThreshold;
        _previousTriggerStates.TryGetValue(axis, out bool wasPressed);

        if (!wasPressed && isPressed)
        {
            DispatchActionDown(action);
        }
        else if (wasPressed && !isPressed)
        {
            DispatchActionUp(action);
        }

        _previousTriggerStates[axis] = isPressed;
    }

    private void ProcessStickMouseMovement(ControllerSnapshot snapshot)
    {
        if (_profile.StickMouseMode == StickMouseMode.None) return;

        GamepadAxis axisX = _profile.StickMouseMode == StickMouseMode.RightStick ? GamepadAxis.RightX : GamepadAxis.LeftX;
        GamepadAxis axisY = _profile.StickMouseMode == StickMouseMode.RightStick ? GamepadAxis.RightY : GamepadAxis.LeftY;

        float rawX = snapshot.GetAxis(axisX);
        float rawY = snapshot.GetAxis(axisY);

        float deadzone = _profile.StickDeadzone;

        float filteredX = ApplyDeadzone(rawX, deadzone);
        float filteredY = ApplyDeadzone(rawY, deadzone);

        if (filteredX != 0.0f || filteredY != 0.0f)
        {
            // Aplica curva quadrática suave para precisão em movimentos finos
            float signX = Math.Sign(filteredX);
            float signY = Math.Sign(filteredY);

            float speedX = signX * (filteredX * filteredX) * _profile.MouseSensitivity;
            float speedY = signY * (filteredY * filteredY) * _profile.MouseSensitivity;

            int dx = (int)Math.Round(speedX);
            int dy = (int)Math.Round(speedY);

            if (dx != 0 || dy != 0)
            {
                _inputSink.SendMouseMove(dx, dy);
            }
        }
    }

    private static float ApplyDeadzone(float value, float deadzone)
    {
        float abs = Math.Abs(value);
        if (abs <= deadzone) return 0.0f;

        // Normaliza a faixa restante de 0 a 1
        float scaled = (abs - deadzone) / (1.0f - deadzone);
        return Math.Sign(value) * scaled;
    }

    private void DispatchActionDown(InputAction action)
    {
        switch (action.Type)
        {
            case ActionType.Key when action.Key.HasValue:
                _inputSink.SendKeyDown(action.Key.Value);
                _activeVirtualKeys.Add(action.Key.Value);
                break;

            case ActionType.MouseButton when action.MouseButton.HasValue:
                _inputSink.SendMouseDown(action.MouseButton.Value);
                _activeMouseButtons.Add(action.MouseButton.Value);
                break;
        }
    }

    private void DispatchActionUp(InputAction action)
    {
        switch (action.Type)
        {
            case ActionType.Key when action.Key.HasValue:
                _inputSink.SendKeyUp(action.Key.Value);
                _activeVirtualKeys.Remove(action.Key.Value);
                break;

            case ActionType.MouseButton when action.MouseButton.HasValue:
                _inputSink.SendMouseUp(action.MouseButton.Value);
                _activeMouseButtons.Remove(action.MouseButton.Value);
                break;
        }
    }

    private void ReleaseHeldInputsInternal()
    {
        try
        {
            _inputSink.ReleaseAll();
        }
        catch (Exception ex)
        {
            LogDiagnostics($"Exceção ao liberar inputs: {ex.Message}");
        }
        finally
        {
            _activeVirtualKeys.Clear();
            _activeMouseButtons.Clear();
            _previousButtonStates.Clear();
            _previousTriggerStates.Clear();
        }
    }

    private void LogDiagnostics(string message)
    {
        DiagnosticsLogged?.Invoke(this, $"[{DateTime.Now:HH:mm:ss.fff}] {message}");
    }

    public void Dispose()
    {
        lock (_syncLock)
        {
            if (_disposed) return;
            _disposed = true;
            _isArmed = false;
            ReleaseHeldInputsInternal();
            _inputSink.Dispose();
        }
        GC.SuppressFinalize(this);
    }
}
