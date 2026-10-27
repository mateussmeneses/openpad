using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using OpenPad.App.ViewModels;

namespace OpenPad.App;

/// <summary>
/// Interaction logic for MainWindow.xaml com suporte a System Tray e Atalho Global do Windows.
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly System.Windows.Forms.NotifyIcon _notifyIcon;
    private bool _isExplicitExit = false;

    // Win32 HotKey API
    private const int WM_HOTKEY = 0x0312;
    private const int HOTKEY_ID = 9001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const uint VK_O = 0x4F; // Tecla 'O'

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private HwndSource? _hwndSource;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;

        // Configura ícone da bandeja do sistema
        _notifyIcon = new System.Windows.Forms.NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "OpenPad — Gamepad Remapper",
            Visible = true
        };

        var contextMenu = new System.Windows.Forms.ContextMenuStrip();
        contextMenu.Items.Add("Abrir OpenPad", null, (s, e) => RestoreFromTray());
        contextMenu.Items.Add("Alternar Armado / Desarmado", null, (s, e) => _viewModel.ToggleArmedCommand.Execute(null));
        contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        contextMenu.Items.Add("Sair", null, (s, e) =>
        {
            _isExplicitExit = true;
            Close();
        });

        _notifyIcon.ContextMenuStrip = contextMenu;
        _notifyIcon.DoubleClick += (s, e) => RestoreFromTray();

        // Notifica na bandeja quando o estado mudar
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.IsArmed))
            {
                _notifyIcon.Text = $"OpenPad — {(_viewModel.IsArmed ? "ARMADO (Ativo)" : "DESARMADO (Inativo)")}";
            }
        };

        // Minimizar para a bandeja
        StateChanged += MainWindow_StateChanged;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        // Registra atalho global Win32: Ctrl + Shift + O para armar/desarmar
        var handle = new WindowInteropHelper(this).Handle;
        _hwndSource = HwndSource.FromHwnd(handle);
        _hwndSource?.AddHook(HwndHook);

        RegisterHotKey(handle, HOTKEY_ID, MOD_CONTROL | MOD_SHIFT, VK_O);
    }

    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
        {
            _viewModel.ToggleArmedCommand.Execute(null);
            _notifyIcon.ShowBalloonTip(
                1200,
                "OpenPad",
                _viewModel.IsArmed ? "⚡ Motor ARMADO (Ativo)" : "🛑 Motor DESARMADO (Inativo)",
                System.Windows.Forms.ToolTipIcon.Info
            );
            handled = true;
        }
        return IntPtr.Zero;
    }

    private void MainWindow_StateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            Hide();
            _notifyIcon.ShowBalloonTip(
                1500,
                "OpenPad",
                "Executando em segundo plano. Use Ctrl+Shift+O ou Back+Start no controle para alternar.",
                System.Windows.Forms.ToolTipIcon.Info
            );
        }
    }

    private void RestoreFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_isExplicitExit)
        {
            // Minimiza para a bandeja em vez de fechar bruscamente
            e.Cancel = true;
            WindowState = WindowState.Minimized;
            return;
        }

        // Saída explícita: libera recursos e desregistra hooks
        var handle = new WindowInteropHelper(this).Handle;
        UnregisterHotKey(handle, HOTKEY_ID);
        _hwndSource?.RemoveHook(HwndHook);

        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _viewModel.Dispose();

        base.OnClosing(e);
    }
}
