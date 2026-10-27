using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace OpenPad.App.Services;

/// <summary>
/// Monitor de processos em primeiro plano no Windows via Win32 API.
/// </summary>
public class ForegroundProcessWatcher : IDisposable
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    private readonly DispatcherTimer _timer;
    private string? _currentProcessName;

    public event EventHandler<string>? ForegroundProcessChanged;

    public string? CurrentProcessName => _currentProcessName;

    public ForegroundProcessWatcher(int intervalMs = 1000)
    {
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(intervalMs)
        };
        _timer.Tick += CheckForegroundProcess;
    }

    public void Start() => _timer.Start();
    public void Stop() => _timer.Stop();

    private void CheckForegroundProcess(object? sender, EventArgs e)
    {
        try
        {
            var hWnd = GetForegroundWindow();
            if (hWnd == IntPtr.Zero) return;

            GetWindowThreadProcessId(hWnd, out var processId);
            if (processId == 0) return;

            using var process = Process.GetProcessById((int)processId);
            var processName = process.ProcessName;
            if (!processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                processName += ".exe";
            }

            if (!string.Equals(_currentProcessName, processName, StringComparison.OrdinalIgnoreCase))
            {
                _currentProcessName = processName;
                ForegroundProcessChanged?.Invoke(this, processName);
            }
        }
        catch
        {
            // Processos protegidos ou que finalizaram durante a checagem são ignorados silenciosamente
        }
    }

    public void Dispose()
    {
        Stop();
    }
}
