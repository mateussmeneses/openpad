using System.Runtime.InteropServices;
using OpenPad.Core.Interfaces;
using OpenPad.Core.Models;

namespace OpenPad.Input;

/// <summary>
/// Implementação de IInputSink usando Win32 SendInput com validação estrita de retorno,
/// tratamento de erros de privilégio/UIPI, diagnósticos e suporte a injeção nativa para testes.
/// </summary>
public class Win32SendInputSink : IInputSink
{
    public delegate uint SendInputNativeHandler(uint nInputs, NativeInput[] pInputs, int cbSize);

    private const uint INPUT_MOUSE = 0;
    private const uint INPUT_KEYBOARD = 1;

    private const uint KEYEVENTF_KEYDOWN = 0x0000;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    private const uint MOUSEEVENTF_MOVE = 0x0001;
    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;
    private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
    private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
    private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
    private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
    private const uint MOUSEEVENTF_XDOWN = 0x0080;
    private const uint MOUSEEVENTF_XUP = 0x0100;

    private const uint XBUTTON1 = 0x0001;
    private const uint XBUTTON2 = 0x0002;

    [StructLayout(LayoutKind.Sequential)]
    public struct NativeInput
    {
        public uint type;
        public InputUnion u;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct InputUnion
    {
        [FieldOffset(0)]
        public MOUSEINPUT mi;
        [FieldOffset(0)]
        public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, NativeInput[] pInputs, int cbSize);

    private readonly SendInputNativeHandler _sendInputNative;
    private readonly object _syncLock = new();
    private readonly HashSet<VirtualKey> _pressedKeys = new();
    private readonly HashSet<MouseButton> _pressedMouseButtons = new();
    private readonly HashSet<VirtualKey> _pendingReleaseKeys = new();
    private readonly HashSet<MouseButton> _pendingReleaseMouseButtons = new();
    private bool _disposed = false;

    public int LastWin32Error { get; private set; }
    public long SuccessfulEventCount { get; private set; }
    public long FailedEventCount { get; private set; }
    public event EventHandler<string>? DiagnosticsLogged;

    private void LogDiagnostics(string message)
    {
        DiagnosticsLogged?.Invoke(this, $"[{DateTime.Now:HH:mm:ss.fff}] {message}");
    }

    public IReadOnlySet<VirtualKey> CurrentlyPressedKeys
    {
        get
        {
            lock (_syncLock) return new HashSet<VirtualKey>(_pressedKeys);
        }
    }

    public IReadOnlySet<MouseButton> CurrentlyPressedMouseButtons
    {
        get
        {
            lock (_syncLock) return new HashSet<MouseButton>(_pressedMouseButtons);
        }
    }

    public IReadOnlySet<VirtualKey> PendingReleaseKeys
    {
        get
        {
            lock (_syncLock) return new HashSet<VirtualKey>(_pendingReleaseKeys);
        }
    }

    public IReadOnlySet<MouseButton> PendingReleaseMouseButtons
    {
        get
        {
            lock (_syncLock) return new HashSet<MouseButton>(_pendingReleaseMouseButtons);
        }
    }

    public Win32SendInputSink() : this(SendInput)
    {
    }

    public Win32SendInputSink(SendInputNativeHandler sendInputNative)
    {
        _sendInputNative = sendInputNative ?? throw new ArgumentNullException(nameof(sendInputNative));
    }

    public bool SendKeyDown(VirtualKey key)
    {
        if (key == VirtualKey.None) return false;

        lock (_syncLock)
        {
            if (_disposed) return false;
            bool ok = SendKeyInputNative((ushort)key, KEYEVENTF_KEYDOWN);
            if (ok)
            {
                _pressedKeys.Add(key);
                _pendingReleaseKeys.Remove(key);
                return true;
            }
            return false;
        }
    }

    public bool SendKeyUp(VirtualKey key)
    {
        if (key == VirtualKey.None) return false;

        lock (_syncLock)
        {
            if (_disposed) return false;
            bool ok = SendKeyInputNative((ushort)key, KEYEVENTF_KEYUP);
            if (ok)
            {
                _pressedKeys.Remove(key);
                _pendingReleaseKeys.Remove(key);
                return true;
            }
            else
            {
                // Liberação falhou no SO: não esquecemos a tecla! Mantemos em pending para recuperação
                _pendingReleaseKeys.Add(key);
                LogDiagnostics($"Falha ao enviar KeyUp para {key} (Win32 Error: {LastWin32Error}). Tecla retida em liberação pendente.");
                return false;
            }
        }
    }

    public bool SendMouseDown(MouseButton button)
    {
        lock (_syncLock)
        {
            if (_disposed) return false;
            var (flags, data) = GetMouseDownFlags(button);
            bool ok = SendMouseInputNative(flags, data, 0, 0);
            if (ok)
            {
                _pressedMouseButtons.Add(button);
                _pendingReleaseMouseButtons.Remove(button);
                return true;
            }
            return false;
        }
    }

    public bool SendMouseUp(MouseButton button)
    {
        lock (_syncLock)
        {
            if (_disposed) return false;
            var (flags, data) = GetMouseUpFlags(button);
            bool ok = SendMouseInputNative(flags, data, 0, 0);
            if (ok)
            {
                _pressedMouseButtons.Remove(button);
                _pendingReleaseMouseButtons.Remove(button);
                return true;
            }
            else
            {
                _pendingReleaseMouseButtons.Add(button);
                LogDiagnostics($"Falha ao enviar MouseUp para {button} (Win32 Error: {LastWin32Error}). Botão retido em liberação pendente.");
                return false;
            }
        }
    }

    public bool SendMouseMove(int dx, int dy)
    {
        if (dx == 0 && dy == 0) return true;

        lock (_syncLock)
        {
            if (_disposed) return false;
            return SendMouseInputNative(MOUSEEVENTF_MOVE, 0, dx, dy);
        }
    }

    public bool ReleaseAll()
    {
        lock (_syncLock)
        {
            if (_disposed) return false;

            bool allSucceeded = true;
            foreach (var key in _pressedKeys.ToList())
            {
                bool ok = SendKeyInputNative((ushort)key, KEYEVENTF_KEYUP);
                if (ok)
                {
                    _pressedKeys.Remove(key);
                    _pendingReleaseKeys.Remove(key);
                }
                else
                {
                    _pendingReleaseKeys.Add(key);
                    allSucceeded = false;
                    LogDiagnostics($"ReleaseAll: Falha ao enviar KeyUp para {key} (Win32 Error: {LastWin32Error}).");
                }
            }

            foreach (var btn in _pressedMouseButtons.ToList())
            {
                var (flags, data) = GetMouseUpFlags(btn);
                bool ok = SendMouseInputNative(flags, data, 0, 0);
                if (ok)
                {
                    _pressedMouseButtons.Remove(btn);
                    _pendingReleaseMouseButtons.Remove(btn);
                }
                else
                {
                    _pendingReleaseMouseButtons.Add(btn);
                    allSucceeded = false;
                    LogDiagnostics($"ReleaseAll: Falha ao enviar MouseUp para {btn} (Win32 Error: {LastWin32Error}).");
                }
            }

            return allSucceeded;
        }
    }

    public int RetryPendingReleases()
    {
        lock (_syncLock)
        {
            if (_disposed || (_pendingReleaseKeys.Count == 0 && _pendingReleaseMouseButtons.Count == 0))
            {
                return 0;
            }

            int recovered = 0;
            foreach (var key in _pendingReleaseKeys.ToList())
            {
                if (SendKeyInputNative((ushort)key, KEYEVENTF_KEYUP))
                {
                    _pressedKeys.Remove(key);
                    _pendingReleaseKeys.Remove(key);
                    recovered++;
                    LogDiagnostics($"Recuperação bem-sucedida de KeyUp pendente para {key}.");
                }
            }

            foreach (var btn in _pendingReleaseMouseButtons.ToList())
            {
                var (flags, data) = GetMouseUpFlags(btn);
                if (SendMouseInputNative(flags, data, 0, 0))
                {
                    _pressedMouseButtons.Remove(btn);
                    _pendingReleaseMouseButtons.Remove(btn);
                    recovered++;
                    LogDiagnostics($"Recuperação bem-sucedida de MouseUp pendente para {btn}.");
                }
            }

            return recovered;
        }
    }

    private bool SendKeyInputNative(ushort vkCode, uint flags)
    {
        var input = new NativeInput
        {
            type = INPUT_KEYBOARD,
            u = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = vkCode,
                    wScan = 0,
                    dwFlags = flags,
                    time = 0,
                    dwExtraInfo = UIntPtr.Zero
                }
            }
        };

        uint sent = _sendInputNative(1, new[] { input }, Marshal.SizeOf<NativeInput>());
        if (sent == 0)
        {
            int err = Marshal.GetLastWin32Error();
            LastWin32Error = err;
            FailedEventCount++;
            DiagnosticsLogged?.Invoke(this, $"Win32 SendInput rejeitado para tecla 0x{vkCode:X4} (Flags: 0x{flags:X4}). Erro Win32: {err}");
            return false;
        }

        SuccessfulEventCount++;
        return true;
    }

    private bool SendMouseInputNative(uint flags, uint mouseData, int dx, int dy)
    {
        var input = new NativeInput
        {
            type = INPUT_MOUSE,
            u = new InputUnion
            {
                mi = new MOUSEINPUT
                {
                    dx = dx,
                    dy = dy,
                    mouseData = mouseData,
                    dwFlags = flags,
                    time = 0,
                    dwExtraInfo = UIntPtr.Zero
                }
            }
        };

        uint sent = _sendInputNative(1, new[] { input }, Marshal.SizeOf<NativeInput>());
        if (sent == 0)
        {
            int err = Marshal.GetLastWin32Error();
            LastWin32Error = err;
            FailedEventCount++;
            DiagnosticsLogged?.Invoke(this, $"Win32 SendInput rejeitado para mouse (Flags: 0x{flags:X4}, dx: {dx}, dy: {dy}). Erro Win32: {err}");
            return false;
        }

        SuccessfulEventCount++;
        return true;
    }

    private static (uint flags, uint data) GetMouseDownFlags(MouseButton button) => button switch
    {
        MouseButton.Left => (MOUSEEVENTF_LEFTDOWN, 0),
        MouseButton.Right => (MOUSEEVENTF_RIGHTDOWN, 0),
        MouseButton.Middle => (MOUSEEVENTF_MIDDLEDOWN, 0),
        MouseButton.XButton1 => (MOUSEEVENTF_XDOWN, XBUTTON1),
        MouseButton.XButton2 => (MOUSEEVENTF_XDOWN, XBUTTON2),
        _ => (0, 0)
    };

    private static (uint flags, uint data) GetMouseUpFlags(MouseButton button) => button switch
    {
        MouseButton.Left => (MOUSEEVENTF_LEFTUP, 0),
        MouseButton.Right => (MOUSEEVENTF_RIGHTUP, 0),
        MouseButton.Middle => (MOUSEEVENTF_MIDDLEUP, 0),
        MouseButton.XButton1 => (MOUSEEVENTF_XUP, XBUTTON1),
        MouseButton.XButton2 => (MOUSEEVENTF_XUP, XBUTTON2),
        _ => (0, 0)
    };

    public void Dispose()
    {
        lock (_syncLock)
        {
            if (_disposed) return;
            _disposed = true;
            ReleaseAll();
        }
        GC.SuppressFinalize(this);
    }
}
