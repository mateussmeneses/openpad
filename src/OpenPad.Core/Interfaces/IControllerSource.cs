using OpenPad.Core.Models;

namespace OpenPad.Core.Interfaces;

public interface IControllerSource : IDisposable
{
    event EventHandler<ControllerInfo>? ControllerConnected;
    event EventHandler<string>? ControllerDisconnected;
    event EventHandler<ControllerSnapshot>? StateUpdated;

    void Start();
    void Stop();
    bool IsRunning { get; }

    IReadOnlyList<ControllerInfo> GetConnectedControllers();

    bool SetRumble(string deviceId, ushort lowFrequencyRumble, ushort highFrequencyRumble, uint durationMs);
}
