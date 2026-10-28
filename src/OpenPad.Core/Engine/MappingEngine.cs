using OpenPad.Core.Interfaces;
using OpenPad.Core.Models;

namespace OpenPad.Core.Engine;

/// <summary>
/// Motor de remapeamento desacoplado de UI e hardware com isolamento completo por dispositivo.
/// Converte eventos semânticos do controle em comandos do InputSink.
/// Garante que o sistema inicie desarmado e que nenhum input permaneça
/// pressionado ao desarmar, desconectar, trocar perfil ou encerrar.
/// Suporta contagem de referências para múltiplos controles simultâneos sem contaminação cruzada.
/// </summary>
public class MappingEngine : IDisposable
{
    private sealed class DeviceInputState
    {
        public string DeviceId { get; }
        public Dictionary<GamepadButton, bool> PreviousButtonStates { get; } = new();
        public Dictionary<GamepadAxis, bool> PreviousTriggerStates { get; } = new();
        public Dictionary<GamepadButton, (DateTime timestamp, bool isDown, InputAction action)> TurboStates { get; } = new();
        public HashSet<VirtualKey> HeldVirtualKeys { get; } = new();
        public HashSet<MouseButton> HeldMouseButtons { get; } = new();

        public DeviceInputState(string deviceId)
        {
            DeviceId = deviceId;
        }

        public void Reset()
        {
            PreviousButtonStates.Clear();
            PreviousTriggerStates.Clear();
            TurboStates.Clear();
            HeldVirtualKeys.Clear();
            HeldMouseButtons.Clear();
        }
    }

    private readonly IInputSink _inputSink;
    private readonly IControllerSource? _controllerSource;
    private readonly object _syncLock = new();
    private bool _isArmed = false;
    private MappingProfile _profile;
    private string? _activeDeviceId;
    private bool _disposed = false;

    private readonly Dictionary<string, DeviceInputState> _deviceStates = new();
    private readonly Dictionary<VirtualKey, int> _virtualKeyRefCounts = new();
    private readonly Dictionary<MouseButton, int> _mouseButtonRefCounts = new();
    private bool _wasArmComboActive = false;

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
            lock (_syncLock) return new HashSet<VirtualKey>(_virtualKeyRefCounts.Keys);
        }
    }

    public IReadOnlySet<MouseButton> ActiveMouseButtons
    {
        get
        {
            lock (_syncLock) return new HashSet<MouseButton>(_mouseButtonRefCounts.Keys);
        }
    }

    public MappingEngine(IInputSink inputSink, MappingProfile? initialProfile = null, IControllerSource? controllerSource = null)
    {
        _inputSink = inputSink ?? throw new ArgumentNullException(nameof(inputSink));
        _profile = initialProfile ?? MappingProfile.CreateDefault();
        _controllerSource = controllerSource;
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
        if (_profile.HapticFeedbackEnabled)
        {
            _controllerSource?.SetRumble(_activeDeviceId ?? "all", 0, 35000, 80);
        }
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
        if (_profile.HapticFeedbackEnabled)
        {
            _controllerSource?.SetRumble(_activeDeviceId ?? "all", 40000, 0, 120);
        }
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
            HandleDeviceDisconnectedInternal(deviceId);
        }
    }

    private void HandleDeviceDisconnectedInternal(string deviceId)
    {
        if (_deviceStates.TryGetValue(deviceId, out var state))
        {
            if (_deviceStates.Count > 1)
            {
                // Múltiplos dispositivos conectados: libera apenas os inputs deste dispositivo isolado
                foreach (var key in state.HeldVirtualKeys.ToList())
                {
                    if (_virtualKeyRefCounts.TryGetValue(key, out var count))
                    {
                        if (count <= 1)
                        {
                            _virtualKeyRefCounts.Remove(key);
                            _inputSink.SendKeyUp(key);
                        }
                        else
                        {
                            _virtualKeyRefCounts[key] = count - 1;
                        }
                    }
                }

                foreach (var btn in state.HeldMouseButtons.ToList())
                {
                    if (_mouseButtonRefCounts.TryGetValue(btn, out var count))
                    {
                        if (count <= 1)
                        {
                            _mouseButtonRefCounts.Remove(btn);
                            _inputSink.SendMouseUp(btn);
                        }
                        else
                        {
                            _mouseButtonRefCounts[btn] = count - 1;
                        }
                    }
                }

                _deviceStates.Remove(deviceId);
                LogDiagnostics($"Controle desconectado: {deviceId}. Entradas do dispositivo liberadas isoladamente.");
            }
            else
            {
                // Dispositivo único: executa liberação global com ReleaseAll no sink
                _deviceStates.Remove(deviceId);
                ReleaseHeldInputsInternal();
                LogDiagnostics($"Último controle desconectado: {deviceId}. Todos os inputs liberados.");
            }
        }
        else if (string.IsNullOrEmpty(_activeDeviceId) || _activeDeviceId == deviceId)
        {
            ReleaseHeldInputsInternal();
        }

        if (_activeDeviceId == deviceId)
        {
            _activeDeviceId = _deviceStates.Keys.FirstOrDefault();
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
                HandleDeviceDisconnectedInternal(snapshot.DeviceId);
                return;
            }

            _activeDeviceId = snapshot.DeviceId;
            var deviceState = GetOrCreateDeviceState(snapshot.DeviceId);

            // 0. Detecção de atalho físico no gamepad para alternar estado Armado/Desarmado (Back + Start)
            bool isComboActive = snapshot.IsButtonPressed(GamepadButton.Back) && snapshot.IsButtonPressed(GamepadButton.Start);
            if (isComboActive && !_wasArmComboActive)
            {
                _wasArmComboActive = true;
                ToggleArmed();
                LogDiagnostics($"Atalho físico de controle (Back+Start) acionado: motor {(_isArmed ? "ARMADO" : "DESARMADO")}");
                return;
            }
            else if (!isComboActive)
            {
                _wasArmComboActive = false;
            }

            // Se desarmado, apenas rastreamos estados anteriores para evitar disparo fantasma ao armar
            if (!_isArmed)
            {
                foreach (var (button, isPressed) in snapshot.Buttons)
                {
                    deviceState.PreviousButtonStates[button] = isPressed;
                }
                deviceState.PreviousTriggerStates[GamepadAxis.TriggerLeft] = snapshot.GetAxis(GamepadAxis.TriggerLeft) >= _profile.TriggerThreshold;
                deviceState.PreviousTriggerStates[GamepadAxis.TriggerRight] = snapshot.GetAxis(GamepadAxis.TriggerRight) >= _profile.TriggerThreshold;
                return;
            }

            // 1. Processar botões digitais (com suporte a Shift Modifier e Modo Turbo)
            bool isShiftActive = _profile.ShiftModifier.HasValue && snapshot.IsButtonPressed(_profile.ShiftModifier.Value);
            var candidateButtons = new HashSet<GamepadButton>(_profile.ButtonBindings.Keys);
            if (isShiftActive)
            {
                foreach (var k in _profile.ShiftBindings.Keys)
                {
                    candidateButtons.Add(k);
                }
            }

            foreach (var button in candidateButtons)
            {
                if (_profile.ShiftModifier.HasValue && button == _profile.ShiftModifier.Value)
                {
                    continue; // Modificador em si não dispara tecla regular
                }

                var action = ResolveActionForButton(button, isShiftActive);
                if (action.Type == ActionType.None) continue;

                var isPressed = snapshot.IsButtonPressed(button);
                deviceState.PreviousButtonStates.TryGetValue(button, out var wasPressed);

                if (action.IsTurbo)
                {
                    ProcessTurboButton(deviceState, button, action, isPressed, wasPressed, snapshot.Timestamp);
                }
                else
                {
                    if (!wasPressed && isPressed)
                    {
                        DispatchActionDown(action, deviceState);
                    }
                    else if (wasPressed && !isPressed)
                    {
                        DispatchActionUp(action, deviceState);
                    }
                }

                deviceState.PreviousButtonStates[button] = isPressed;
            }

            // 2. Processar gatilhos analógicos (LT e RT)
            ProcessTriggerAxis(deviceState, snapshot, GamepadAxis.TriggerLeft);
            ProcessTriggerAxis(deviceState, snapshot, GamepadAxis.TriggerRight);

            // 3. Processar movimentação de mouse por stick analógico
            ProcessStickMouseMovement(snapshot);
        }
    }

    private DeviceInputState GetOrCreateDeviceState(string deviceId)
    {
        if (!_deviceStates.TryGetValue(deviceId, out var state))
        {
            state = new DeviceInputState(deviceId);
            _deviceStates[deviceId] = state;
        }
        return state;
    }

    private void ProcessTurboButton(DeviceInputState deviceState, GamepadButton button, InputAction action, bool isPressed, bool wasPressed, DateTime timestamp)
    {
        if (!wasPressed && isPressed)
        {
            // Começou a pressionar: dispara primeiro KeyDown / MouseDown
            DispatchActionDown(action, deviceState);
            deviceState.TurboStates[button] = (timestamp, true, action);
        }
        else if (wasPressed && isPressed)
        {
            // Mantido pressionado: pulsa periodicamente a cada 50ms
            if (deviceState.TurboStates.TryGetValue(button, out var state))
            {
                if ((timestamp - state.timestamp).TotalMilliseconds >= 50)
                {
                    if (state.isDown)
                    {
                        DispatchActionUp(action, deviceState);
                        deviceState.TurboStates[button] = (timestamp, false, action);
                    }
                    else
                    {
                        DispatchActionDown(action, deviceState);
                        deviceState.TurboStates[button] = (timestamp, true, action);
                    }
                }
            }
        }
        else if (wasPressed && !isPressed)
        {
            // Soltou o botão: garante que se estava Down, solte
            if (deviceState.TurboStates.TryGetValue(button, out var state) && state.isDown)
            {
                DispatchActionUp(action, deviceState);
            }
            deviceState.TurboStates.Remove(button);
        }
    }

    private InputAction ResolveActionForButton(GamepadButton button, bool isShiftActive)
    {
        if (isShiftActive && _profile.ShiftBindings.TryGetValue(button, out var shiftAction) && shiftAction.Type != ActionType.None)
        {
            return shiftAction;
        }

        if (_profile.ButtonBindings.TryGetValue(button, out var regularAction))
        {
            return regularAction;
        }

        return InputAction.None();
    }

    private void ProcessTriggerAxis(DeviceInputState deviceState, ControllerSnapshot snapshot, GamepadAxis axis)
    {
        if (!_profile.TriggerBindings.TryGetValue(axis, out var action) || action.Type == ActionType.None)
        {
            return;
        }

        float value = snapshot.GetAxis(axis);
        bool isPressed = value >= _profile.TriggerThreshold;
        deviceState.PreviousTriggerStates.TryGetValue(axis, out bool wasPressed);

        if (!wasPressed && isPressed)
        {
            DispatchActionDown(action, deviceState);
            if (_profile.HapticFeedbackEnabled)
            {
                _controllerSource?.SetRumble(_activeDeviceId ?? "all", 0, 15000, 30);
            }
        }
        else if (wasPressed && !isPressed)
        {
            DispatchActionUp(action, deviceState);
        }

        deviceState.PreviousTriggerStates[axis] = isPressed;
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
            float signX = Math.Sign(filteredX);
            float signY = Math.Sign(filteredY);

            float magX = Math.Abs(filteredX);
            float magY = Math.Abs(filteredY);

            float factorX = ApplyCurve(magX, _profile.StickCurveType);
            float factorY = ApplyCurve(magY, _profile.StickCurveType);

            float speedX = signX * factorX * _profile.MouseSensitivity;
            float speedY = signY * factorY * _profile.MouseSensitivity;

            int dx = (int)Math.Round(speedX);
            int dy = (int)Math.Round(speedY);

            if (dx != 0 || dy != 0)
            {
                _inputSink.SendMouseMove(dx, dy);
            }
        }
    }

    private static float ApplyCurve(float normalizedMagnitude, StickCurveType curveType)
    {
        return curveType switch
        {
            StickCurveType.Linear => normalizedMagnitude,
            StickCurveType.Aggressive => (float)Math.Pow(normalizedMagnitude, 1.4),
            StickCurveType.Smooth => (float)Math.Pow(normalizedMagnitude, 2.5),
            _ => normalizedMagnitude * normalizedMagnitude // Quadratic (default)
        };
    }

    private static float ApplyDeadzone(float value, float deadzone)
    {
        float abs = Math.Abs(value);
        if (abs <= deadzone) return 0.0f;

        float scaled = (abs - deadzone) / (1.0f - deadzone);
        return Math.Sign(value) * scaled;
    }

    private void DispatchActionDown(InputAction action, DeviceInputState deviceState)
    {
        switch (action.Type)
        {
            case ActionType.Key when action.Key.HasValue:
                var key = action.Key.Value;
                if (deviceState.HeldVirtualKeys.Add(key))
                {
                    _virtualKeyRefCounts.TryGetValue(key, out int count);
                    _virtualKeyRefCounts[key] = count + 1;
                    if (count == 0)
                    {
                        _inputSink.SendKeyDown(key);
                    }
                }
                break;

            case ActionType.MouseButton when action.MouseButton.HasValue:
                var btn = action.MouseButton.Value;
                if (deviceState.HeldMouseButtons.Add(btn))
                {
                    _mouseButtonRefCounts.TryGetValue(btn, out int count);
                    _mouseButtonRefCounts[btn] = count + 1;
                    if (count == 0)
                    {
                        _inputSink.SendMouseDown(btn);
                    }
                }
                break;
        }
    }

    private void DispatchActionUp(InputAction action, DeviceInputState deviceState)
    {
        switch (action.Type)
        {
            case ActionType.Key when action.Key.HasValue:
                var key = action.Key.Value;
                if (deviceState.HeldVirtualKeys.Remove(key))
                {
                    if (_virtualKeyRefCounts.TryGetValue(key, out int count))
                    {
                        if (count <= 1)
                        {
                            _virtualKeyRefCounts.Remove(key);
                            _inputSink.SendKeyUp(key);
                        }
                        else
                        {
                            _virtualKeyRefCounts[key] = count - 1;
                        }
                    }
                }
                break;

            case ActionType.MouseButton when action.MouseButton.HasValue:
                var btn = action.MouseButton.Value;
                if (deviceState.HeldMouseButtons.Remove(btn))
                {
                    if (_mouseButtonRefCounts.TryGetValue(btn, out int count))
                    {
                        if (count <= 1)
                        {
                            _mouseButtonRefCounts.Remove(btn);
                            _inputSink.SendMouseUp(btn);
                        }
                        else
                        {
                            _mouseButtonRefCounts[btn] = count - 1;
                        }
                    }
                }
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
            _virtualKeyRefCounts.Clear();
            _mouseButtonRefCounts.Clear();
            foreach (var state in _deviceStates.Values)
            {
                state.Reset();
            }
            _deviceStates.Clear();
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
