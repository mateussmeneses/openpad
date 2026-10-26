using OpenPad.Core.Interfaces;
using OpenPad.Core.Models;
using SDL3;

namespace OpenPad.Input;

/// <summary>
/// Fonte de entrada de controles reais baseada no SDL3 Gamepad API.
/// Executa polling em thread de trabalho dedicada para não bloquear a thread UI.
/// Publica snapshots imutáveis e trata conexões, desconexões e falhas de inicialização com segurança.
/// </summary>
public class SDL3ControllerSource : IControllerSource
{
    private readonly object _syncLock = new();
    private Thread? _workerThread;
    private volatile bool _isRunning = false;
    private bool _sdlInitialized = false;
    private bool _disposed = false;

    private readonly Dictionary<uint, IntPtr> _openGamepads = new();
    private readonly Dictionary<uint, ControllerInfo> _connectedControllers = new();

    public event EventHandler<ControllerInfo>? ControllerConnected;
    public event EventHandler<string>? ControllerDisconnected;
    public event EventHandler<ControllerSnapshot>? StateUpdated;
    public event EventHandler<string>? LogMessage;

    public bool IsRunning => _isRunning;

    public bool IsSdlAvailable
    {
        get
        {
            lock (_syncLock) return _sdlInitialized;
        }
    }

    public IReadOnlyList<ControllerInfo> GetConnectedControllers()
    {
        lock (_syncLock)
        {
            return _connectedControllers.Values.ToList();
        }
    }

    public void Start()
    {
        lock (_syncLock)
        {
            if (_isRunning) return;

            try
            {
                if (!_sdlInitialized)
                {
                    if (!SDL.Init(SDL.InitFlags.Gamepad))
                    {
                        var error = SDL.GetError();
                        LogMessage?.Invoke(this, $"Falha ao inicializar SDL3 Gamepad: {error}");
                        return;
                    }
                    _sdlInitialized = true;
                    LogMessage?.Invoke(this, "SDL3 inicializado com sucesso.");
                }

                _isRunning = true;
                _workerThread = new Thread(WorkerLoop)
                {
                    Name = "OpenPad.SDL3Worker",
                    IsBackground = true,
                    Priority = ThreadPriority.AboveNormal
                };
                _workerThread.Start();
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke(this, $"Erro ao iniciar SDL3ControllerSource: {ex.Message}");
            }
        }
    }

    public void Stop()
    {
        lock (_syncLock)
        {
            if (!_isRunning) return;
            _isRunning = false;
        }

        if (_workerThread != null && _workerThread.IsAlive)
        {
            _workerThread.Join(500);
            _workerThread = null;
        }

        lock (_syncLock)
        {
            CloseAllGamepads();
        }
    }

    private void WorkerLoop()
    {
        while (_isRunning)
        {
            try
            {
                SDL.UpdateGamepads();
                ScanForGamepads();
                PollGamepadStates();
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke(this, $"Exceção no ciclo de leitura SDL3: {ex.Message}");
            }

            // Polling a ~120Hz (aprox. 8ms)
            Thread.Sleep(8);
        }
    }

    private void ScanForGamepads()
    {
        var gamepadIds = SDL.GetGamepads(out int count);
        var currentIds = new HashSet<uint>();

        if (gamepadIds != null && count > 0)
        {
            for (int i = 0; i < count; i++)
            {
                var instanceId = gamepadIds[i];
                currentIds.Add(instanceId);

                lock (_syncLock)
                {
                    if (!_openGamepads.ContainsKey(instanceId))
                    {
                        var handle = SDL.OpenGamepad(instanceId);
                        if (handle != IntPtr.Zero)
                        {
                            var name = SDL.GetGamepadName(handle) ?? $"Controle #{instanceId}";
                            var info = new ControllerInfo(instanceId.ToString(), name, true);

                            _openGamepads[instanceId] = handle;
                            _connectedControllers[instanceId] = info;

                            LogMessage?.Invoke(this, $"Novo controle detectado: {name} (ID: {instanceId})");
                            ControllerConnected?.Invoke(this, info);
                        }
                    }
                }
            }
        }

        // Verifica controles que foram desconectados
        List<uint> disconnectedIds;
        lock (_syncLock)
        {
            disconnectedIds = _openGamepads.Keys.Where(id => !currentIds.Contains(id)).ToList();
        }

        foreach (var id in disconnectedIds)
        {
            lock (_syncLock)
            {
                if (_openGamepads.TryGetValue(id, out var handle))
                {
                    SDL.CloseGamepad(handle);
                    _openGamepads.Remove(id);
                }
                _connectedControllers.Remove(id);
            }

            LogMessage?.Invoke(this, $"Controle desconectado: ID {id}");
            ControllerDisconnected?.Invoke(this, id.ToString());
        }
    }

    private void PollGamepadStates()
    {
        List<KeyValuePair<uint, IntPtr>> activeList;
        lock (_syncLock)
        {
            activeList = _openGamepads.ToList();
        }

        foreach (var (instanceId, handle) in activeList)
        {
            string devName;
            lock (_syncLock)
            {
                devName = _connectedControllers.TryGetValue(instanceId, out var info) ? info.Name : $"Controle {instanceId}";
            }

            var buttons = new Dictionary<GamepadButton, bool>();
            foreach (var (coreBtn, sdlBtn) in ButtonMap)
            {
                buttons[coreBtn] = SDL.GetGamepadButton(handle, sdlBtn);
            }

            var axes = new Dictionary<GamepadAxis, float>();
            foreach (var (coreAxis, sdlAxis) in AxisMap)
            {
                short rawVal = SDL.GetGamepadAxis(handle, sdlAxis);
                axes[coreAxis] = NormalizeAxis(coreAxis, rawVal);
            }

            var snapshot = new ControllerSnapshot(
                instanceId.ToString(),
                devName,
                true,
                DateTime.UtcNow,
                buttons,
                axes
            );

            StateUpdated?.Invoke(this, snapshot);
        }
    }

    private static float NormalizeAxis(GamepadAxis axis, short raw)
    {
        if (axis == GamepadAxis.TriggerLeft || axis == GamepadAxis.TriggerRight)
        {
            // Gatilhos variam de 0 a 32767
            return Math.Clamp(raw / 32767.0f, 0.0f, 1.0f);
        }

        // Sticks variam de -32768 a 32767
        return Math.Clamp(raw / 32767.0f, -1.0f, 1.0f);
    }

    private void CloseAllGamepads()
    {
        foreach (var handle in _openGamepads.Values)
        {
            if (handle != IntPtr.Zero)
            {
                SDL.CloseGamepad(handle);
            }
        }
        _openGamepads.Clear();
        _connectedControllers.Clear();
    }

    public void Dispose()
    {
        lock (_syncLock)
        {
            if (_disposed) return;
            _disposed = true;
        }

        Stop();

        lock (_syncLock)
        {
            if (_sdlInitialized)
            {
                SDL.Quit();
                _sdlInitialized = false;
            }
        }

        GC.SuppressFinalize(this);
    }

    private static readonly KeyValuePair<GamepadButton, SDL.GamepadButton>[] ButtonMap =
    {
        new(GamepadButton.A, SDL.GamepadButton.South),
        new(GamepadButton.B, SDL.GamepadButton.East),
        new(GamepadButton.X, SDL.GamepadButton.West),
        new(GamepadButton.Y, SDL.GamepadButton.North),
        new(GamepadButton.Back, SDL.GamepadButton.Back),
        new(GamepadButton.Guide, SDL.GamepadButton.Guide),
        new(GamepadButton.Start, SDL.GamepadButton.Start),
        new(GamepadButton.LeftStick, SDL.GamepadButton.LeftStick),
        new(GamepadButton.RightStick, SDL.GamepadButton.RightStick),
        new(GamepadButton.LeftShoulder, SDL.GamepadButton.LeftShoulder),
        new(GamepadButton.RightShoulder, SDL.GamepadButton.RightShoulder),
        new(GamepadButton.DpadUp, SDL.GamepadButton.DPadUp),
        new(GamepadButton.DpadDown, SDL.GamepadButton.DPadDown),
        new(GamepadButton.DpadLeft, SDL.GamepadButton.DPadLeft),
        new(GamepadButton.DpadRight, SDL.GamepadButton.DPadRight),
        new(GamepadButton.Misc1, SDL.GamepadButton.Misc1),
        new(GamepadButton.RightPaddle1, SDL.GamepadButton.RightPaddle1),
        new(GamepadButton.LeftPaddle1, SDL.GamepadButton.LeftPaddle1),
        new(GamepadButton.RightPaddle2, SDL.GamepadButton.RightPaddle2),
        new(GamepadButton.LeftPaddle2, SDL.GamepadButton.LeftPaddle2),
        new(GamepadButton.Touchpad, SDL.GamepadButton.Touchpad),
    };

    private static readonly KeyValuePair<GamepadAxis, SDL.GamepadAxis>[] AxisMap =
    {
        new(GamepadAxis.LeftX, SDL.GamepadAxis.LeftX),
        new(GamepadAxis.LeftY, SDL.GamepadAxis.LeftY),
        new(GamepadAxis.RightX, SDL.GamepadAxis.RightX),
        new(GamepadAxis.RightY, SDL.GamepadAxis.RightY),
        new(GamepadAxis.TriggerLeft, SDL.GamepadAxis.LeftTrigger),
        new(GamepadAxis.TriggerRight, SDL.GamepadAxis.RightTrigger),
    };
}
