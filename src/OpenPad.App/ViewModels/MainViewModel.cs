using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using OpenPad.App.Common;
using OpenPad.Core.Engine;
using OpenPad.Core.Interfaces;
using OpenPad.Core.Models;
using OpenPad.Core.Storage;
using OpenPad.Input;

namespace OpenPad.App.ViewModels;

public class MainViewModel : ViewModelBase, IDisposable
{
    private readonly IControllerSource _controllerSource;
    private readonly IInputSink _inputSink;
    private readonly MappingEngine _mappingEngine;
    private readonly JsonProfileStore _profileStore;
    private readonly Dispatcher _dispatcher;

    private bool _isArmed;
    private string _controllerName = "Nenhum controle detectado";
    private bool _isControllerConnected = false;
    private string _profileName = "Padrão";
    private string _statusMessage = "Pronto. O sistema inicia desarmado por segurança.";
    private DateTime _lastUiUpdate = DateTime.MinValue;

    // Estados visuais ao vivo
    private bool _btnA, _btnB, _btnX, _btnY;
    private bool _btnLB, _btnRB, _btnBack, _btnStart, _btnGuide;
    private bool _btnLS, _btnRS;
    private bool _dpadUp, _dpadDown, _dpadLeft, _dpadRight;
    private double _axisLX, _axisLY, _axisRX, _axisRY;
    private double _triggerLT, _triggerRT;

    public bool IsArmed
    {
        get => _isArmed;
        set
        {
            if (SetField(ref _isArmed, value))
            {
                OnPropertyChanged(nameof(ArmedStatusText));
                OnPropertyChanged(nameof(ArmedStatusColor));
            }
        }
    }

    public string ArmedStatusText => IsArmed ? "ARMADO (ATIVO)" : "DESARMADO (INATIVO)";
    public string ArmedStatusColor => IsArmed ? "#4CAF50" : "#E53935";

    public string ControllerName
    {
        get => _controllerName;
        set => SetField(ref _controllerName, value);
    }

    public bool IsControllerConnected
    {
        get => _isControllerConnected;
        set => SetField(ref _isControllerConnected, value);
    }

    public string ProfileName
    {
        get => _profileName;
        set => SetField(ref _profileName, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetField(ref _statusMessage, value);
    }

    // Botões visuais
    public bool BtnA { get => _btnA; set => SetField(ref _btnA, value); }
    public bool BtnB { get => _btnB; set => SetField(ref _btnB, value); }
    public bool BtnX { get => _btnX; set => SetField(ref _btnX, value); }
    public bool BtnY { get => _btnY; set => SetField(ref _btnY, value); }
    public bool BtnLB { get => _btnLB; set => SetField(ref _btnLB, value); }
    public bool BtnRB { get => _btnRB; set => SetField(ref _btnRB, value); }
    public bool BtnBack { get => _btnBack; set => SetField(ref _btnBack, value); }
    public bool BtnStart { get => _btnStart; set => SetField(ref _btnStart, value); }
    public bool BtnGuide { get => _btnGuide; set => SetField(ref _btnGuide, value); }
    public bool BtnLS { get => _btnLS; set => SetField(ref _btnLS, value); }
    public bool BtnRS { get => _btnRS; set => SetField(ref _btnRS, value); }
    public bool DpadUp { get => _dpadUp; set => SetField(ref _dpadUp, value); }
    public bool DpadDown { get => _dpadDown; set => SetField(ref _dpadDown, value); }
    public bool DpadLeft { get => _dpadLeft; set => SetField(ref _dpadLeft, value); }
    public bool DpadRight { get => _dpadRight; set => SetField(ref _dpadRight, value); }

    // Eixos analógicos (-100 a +100 ou 0 a 100 para gatilhos)
    public double AxisLX { get => _axisLX; set => SetField(ref _axisLX, value); }
    public double AxisLY { get => _axisLY; set => SetField(ref _axisLY, value); }
    public double AxisRX { get => _axisRX; set => SetField(ref _axisRX, value); }
    public double AxisRY { get => _axisRY; set => SetField(ref _axisRY, value); }
    public double TriggerLT { get => _triggerLT; set => SetField(ref _triggerLT, value); }
    public double TriggerRT { get => _triggerRT; set => SetField(ref _triggerRT, value); }

    // Configurações avançadas de stick e gatilhos
    private StickMouseMode _selectedStickMouseMode = StickMouseMode.RightStick;
    private float _mouseSensitivity = 15.0f;
    private float _stickDeadzone = 0.15f;
    private float _triggerThreshold = 0.30f;

    public StickMouseMode SelectedStickMouseMode
    {
        get => _selectedStickMouseMode;
        set => SetField(ref _selectedStickMouseMode, value);
    }

    public float MouseSensitivity
    {
        get => _mouseSensitivity;
        set => SetField(ref _mouseSensitivity, value);
    }

    public float StickDeadzone
    {
        get => _stickDeadzone;
        set => SetField(ref _stickDeadzone, value);
    }

    public float TriggerThreshold
    {
        get => _triggerThreshold;
        set => SetField(ref _triggerThreshold, value);
    }

    public IReadOnlyList<StickMouseMode> AvailableStickMouseModes { get; } = Enum.GetValues<StickMouseMode>();

    public ObservableCollection<BindingItemViewModel> Bindings { get; } = new();
    public ObservableCollection<string> LogEntries { get; } = new();

    public IReadOnlyList<VirtualKey> AvailableKeys { get; } = Enum.GetValues<VirtualKey>()
        .Where(k => k != VirtualKey.None)
        .OrderBy(k => k.ToString())
        .ToList();

    public IReadOnlyList<MouseButton> AvailableMouseButtons { get; } = Enum.GetValues<MouseButton>().ToList();
    public IReadOnlyList<ActionType> AvailableActionTypes { get; } = Enum.GetValues<ActionType>().ToList();

    // Comandos
    public RelayCommand ToggleArmedCommand { get; }
    public RelayCommand EmergencyDisarmCommand { get; }
    public RelayCommand SaveProfileCommand { get; }
    public RelayCommand LoadProfileCommand { get; }
    public RelayCommand ResetProfileCommand { get; }
    public RelayCommand ApplyBindingsCommand { get; }

    public MainViewModel()
    {
        _dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
        _profileStore = new JsonProfileStore();
        _inputSink = new Win32SendInputSink();

        var initialProfile = MappingProfile.CreateDefault();
        _mappingEngine = new MappingEngine(_inputSink, initialProfile);
        _controllerSource = new SDL3ControllerSource();

        ToggleArmedCommand = new RelayCommand(ToggleArmed);
        EmergencyDisarmCommand = new RelayCommand(Disarm);
        SaveProfileCommand = new RelayCommand(SaveProfile);
        LoadProfileCommand = new RelayCommand(LoadProfile);
        ResetProfileCommand = new RelayCommand(ResetToDefault);
        ApplyBindingsCommand = new RelayCommand(ApplyCurrentBindingsToEngine);

        // Conecta eventos do motor
        _mappingEngine.ArmedStateChanged += (s, armed) =>
        {
            _dispatcher.InvokeAsync(() => IsArmed = armed);
        };

        _mappingEngine.DiagnosticsLogged += (s, msg) =>
        {
            _dispatcher.InvokeAsync(() => AddLog(msg));
        };

        // Conecta eventos da fonte de controles
        _controllerSource.ControllerConnected += OnControllerConnected;
        _controllerSource.ControllerDisconnected += OnControllerDisconnected;
        _controllerSource.StateUpdated += OnControllerStateUpdated;

        if (_controllerSource is SDL3ControllerSource sdlSource)
        {
            sdlSource.LogMessage += (s, msg) => _dispatcher.InvokeAsync(() => AddLog(msg));
        }

        // Popula bindings do perfil inicial
        LoadProfileIntoEditor(initialProfile);

        // Inicia escuta
        _controllerSource.Start();
        AddLog("OpenPad iniciado com sucesso. Desarmado por padrão.");
    }

    private void ToggleArmed()
    {
        _mappingEngine.ToggleArmed();
    }

    private void Disarm()
    {
        _mappingEngine.Disarm();
        StatusMessage = "EMERGÊNCIA: Motor desarmado e todos os inputs liberados.";
    }

    private void OnControllerConnected(object? sender, ControllerInfo info)
    {
        _dispatcher.InvokeAsync(() =>
        {
            ControllerName = $"{info.Name} (ID: {info.Id})";
            IsControllerConnected = true;
            StatusMessage = $"Controle conectado: {info.Name}";
        });
    }

    private void OnControllerDisconnected(object? sender, string deviceId)
    {
        _mappingEngine.HandleDeviceDisconnected(deviceId);

        _dispatcher.InvokeAsync(() =>
        {
            ControllerName = "Nenhum controle detectado";
            IsControllerConnected = false;
            StatusMessage = $"Controle desconectado (ID: {deviceId}). Inputs liberados.";
            ResetVisualState();
        });
    }

    private void OnControllerStateUpdated(object? sender, ControllerSnapshot snapshot)
    {
        // 1. Processa no motor de remapeamento (baixa latência)
        _mappingEngine.ProcessSnapshot(snapshot);

        // 2. Atualiza UI visual com throttling (~30-60 fps)
        var now = DateTime.UtcNow;
        if ((now - _lastUiUpdate).TotalMilliseconds < 25) return;
        _lastUiUpdate = now;

        _dispatcher.InvokeAsync(() =>
        {
            BtnA = snapshot.IsButtonPressed(GamepadButton.A);
            BtnB = snapshot.IsButtonPressed(GamepadButton.B);
            BtnX = snapshot.IsButtonPressed(GamepadButton.X);
            BtnY = snapshot.IsButtonPressed(GamepadButton.Y);
            BtnLB = snapshot.IsButtonPressed(GamepadButton.LeftShoulder);
            BtnRB = snapshot.IsButtonPressed(GamepadButton.RightShoulder);
            BtnBack = snapshot.IsButtonPressed(GamepadButton.Back);
            BtnStart = snapshot.IsButtonPressed(GamepadButton.Start);
            BtnGuide = snapshot.IsButtonPressed(GamepadButton.Guide);
            BtnLS = snapshot.IsButtonPressed(GamepadButton.LeftStick);
            BtnRS = snapshot.IsButtonPressed(GamepadButton.RightStick);
            DpadUp = snapshot.IsButtonPressed(GamepadButton.DpadUp);
            DpadDown = snapshot.IsButtonPressed(GamepadButton.DpadDown);
            DpadLeft = snapshot.IsButtonPressed(GamepadButton.DpadLeft);
            DpadRight = snapshot.IsButtonPressed(GamepadButton.DpadRight);

            AxisLX = snapshot.GetAxis(GamepadAxis.LeftX) * 100.0;
            AxisLY = snapshot.GetAxis(GamepadAxis.LeftY) * 100.0;
            AxisRX = snapshot.GetAxis(GamepadAxis.RightX) * 100.0;
            AxisRY = snapshot.GetAxis(GamepadAxis.RightY) * 100.0;
            TriggerLT = snapshot.GetAxis(GamepadAxis.TriggerLeft) * 100.0;
            TriggerRT = snapshot.GetAxis(GamepadAxis.TriggerRight) * 100.0;
        });
    }

    private void ResetVisualState()
    {
        BtnA = BtnB = BtnX = BtnY = false;
        BtnLB = BtnRB = BtnBack = BtnStart = BtnGuide = false;
        BtnLS = BtnRS = DpadUp = DpadDown = DpadLeft = DpadRight = false;
        AxisLX = AxisLY = AxisRX = AxisRY = 0;
        TriggerLT = TriggerRT = 0;
    }

    private void LoadProfileIntoEditor(MappingProfile profile)
    {
        ProfileName = profile.Name;
        SelectedStickMouseMode = profile.StickMouseMode;
        MouseSensitivity = profile.MouseSensitivity;
        StickDeadzone = profile.StickDeadzone;
        TriggerThreshold = profile.TriggerThreshold;

        Bindings.Clear();

        foreach (GamepadButton btn in Enum.GetValues<GamepadButton>())
        {
            if (btn == GamepadButton.Misc1 || btn.ToString().StartsWith("RightPaddle") || btn.ToString().StartsWith("LeftPaddle"))
            {
                continue; // Foco nos botões padrão no MVP
            }

            var action = profile.ButtonBindings.TryGetValue(btn, out var act) ? act : InputAction.None();
            Bindings.Add(new BindingItemViewModel(btn, action));
        }
    }

    private void ApplyCurrentBindingsToEngine()
    {
        var profile = new MappingProfile
        {
            SchemaVersion = 1,
            Name = ProfileName,
            Description = "Mapeamento atual do usuário",
            StickMouseMode = SelectedStickMouseMode,
            MouseSensitivity = MouseSensitivity,
            StickDeadzone = StickDeadzone,
            TriggerThreshold = TriggerThreshold
        };

        foreach (var item in Bindings)
        {
            profile.ButtonBindings[item.Button] = item.ToInputAction();
        }

        // Configura gatilhos padrão (RT -> Clique esquerdo, LT -> Clique direito)
        profile.TriggerBindings[GamepadAxis.TriggerRight] = InputAction.FromMouse(MouseButton.Left);
        profile.TriggerBindings[GamepadAxis.TriggerLeft] = InputAction.FromMouse(MouseButton.Right);

        _mappingEngine.SetProfile(profile);
        StatusMessage = "Mapeamentos aplicados ao motor de entrada.";
    }

    private void SaveProfile()
    {
        ApplyCurrentBindingsToEngine();

        var dialog = new SaveFileDialog
        {
            Filter = "Perfis OpenPad (*.json)|*.json",
            DefaultExt = ".json",
            FileName = $"{ProfileName.Replace(' ', '_')}.json"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                _profileStore.SaveToFile(dialog.FileName, _mappingEngine.CurrentProfile);
                AddLog($"Perfil salvo em: {dialog.FileName}");
                StatusMessage = "Perfil salvo com sucesso.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao salvar perfil: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void LoadProfile()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Perfis OpenPad (*.json)|*.json",
            DefaultExt = ".json"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var profile = _profileStore.LoadFromFile(dialog.FileName);
                LoadProfileIntoEditor(profile);
                _mappingEngine.SetProfile(profile);
                AddLog($"Perfil carregado de: {dialog.FileName}");
                StatusMessage = $"Perfil '{profile.Name}' carregado com sucesso.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar perfil: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void ResetToDefault()
    {
        var def = MappingProfile.CreateDefault();
        LoadProfileIntoEditor(def);
        _mappingEngine.SetProfile(def);
        StatusMessage = "Restaurado para o perfil padrão.";
        AddLog("Perfil restaurado para as configurações padrão.");
    }

    private void AddLog(string msg)
    {
        LogEntries.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {msg}");
        if (LogEntries.Count > 100)
        {
            LogEntries.RemoveAt(LogEntries.Count - 1);
        }
    }

    public void Dispose()
    {
        _controllerSource.Stop();
        _controllerSource.Dispose();
        _mappingEngine.Dispose();
        _inputSink.Dispose();
        GC.SuppressFinalize(this);
    }
}
