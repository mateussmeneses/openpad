using System.Runtime.InteropServices;
using OpenPad.Core.Interfaces;
using OpenPad.Core.Models;

namespace OpenPad.Input;

/// <summary>
/// Implementação de IInputSink usando Win32 SendInput.
/// Fornece injeção de teclas e cliques do mouse no Windows com rastreamento
/// de estado para garantia de liberação de todas as teclas pressionadas.
/// Não requer privilégios de administrador ou drivers virtuais.
/// </summary>
public class Win32SendInputSink : IInputSink
{
    private const uint INPUT_MOUSE = 0;
    private const uint INPUT_KEYBOARD = 1;

    private const uint KEYEVENTF_KEYDOWN = 0x0000;
    private const uint KEYEVENTF_KEYUP = 0x0002;

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
    private struct NativeInput
    {
        public uint type;
        public InputUnion u;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public MOUSEINPUT mi;
        [FieldOffset(0)]
        public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
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

    private readonly object _syncLock = new();
    private readonly HashSet<VirtualKey> _pressedKeys = new();
    private readonly HashSet<MouseButton> _pressedMouseButtons = new();
    private bool _disposed = false;

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

    public void SendKeyDown(VirtualKey key)
    {
        if (key == VirtualKey.None) return;

        lock (_syncLock)
        {
            if (_disposed) return;
            SendKeyInputNative((ushort)key, KEYEVENTF_KEYDOWN);
            _pressedKeys.Add(key);
        }
    }

    public void SendKeyUp(VirtualKey key)
    {
        if (key == VirtualKey.None) return;

        lock (_syncLock)
        {
            if (_disposed) return;
            SendKeyInputNative((ushort)key, KEYEVENTF_KEYUP);
            _pressedKeys.Remove(key);
        }
    }

    public void SendMouseDown(MouseButton button)
    {
        lock (_syncLock)
        {
            if (_disposed) return;
            var (flags, data) = GetMouseDownFlags(button);
            SendMouseInputNative(flags, data);
            _pressedMouseButtons.Add(button);
        }
    }

    public void SendMouseUp(MouseButton button)
    {
        lock (_syncLock)
        {
            if (_disposed) return;
            var (flags, data) = GetMouseUpFlags(button);
            SendMouseInputNative(flags, data);
            _pressedMouseButtons.Remove(button);
        }
    }

    public void ReleaseAll()
    {
        lock (_syncLock)
        {
            // Libera todas as teclas pressionadas
            foreach (var key in _pressedKeys.ToList())
            {
                SendKeyInputNative((ushort)key, KEYEVENTF_KEYUP);
            }
            _pressedKeys.Clear();

            // Libera todos os botões do mouse pressionados
            foreach (var btn in _pressedMouseButtons.ToList())
            {
                var (flags, data) = GetMouseUpFlags(btn);
                SendMouseInputNative(flags, data);
            }
            _pressedMouseButtons.Clear();
        }
    }

    private static void SendKeyInputNative(ushort vkCode, uint flags)
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

        SendInput(1, new[] { input }, Marshal.SizeOf<NativeInput>());
    }

    private static void SendMouseInputNative(uint flags, uint mouseData)
    {
        var input = new NativeInput
        {
            type = INPUT_MOUSE,
            u = new InputUnion
            {
                mi = new MOUSEINPUT
                {
                    dx = 0,
                    dy = 0,
                    mouseData = mouseData,
                    dwFlags = flags,
                    time = 0,
                    dwExtraInfo = UIntPtr.Zero
                }
            }
        };

        SendInput(1, new[] { input }, Marshal.SizeOf<NativeInput>());
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
