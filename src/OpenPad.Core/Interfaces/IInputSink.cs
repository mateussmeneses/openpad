using OpenPad.Core.Models;

namespace OpenPad.Core.Interfaces;

public interface IInputSink : IDisposable
{
    bool SendKeyDown(VirtualKey key);
    bool SendKeyUp(VirtualKey key);
    bool SendMouseDown(MouseButton button);
    bool SendMouseUp(MouseButton button);
    bool SendMouseMove(int dx, int dy);
    bool ReleaseAll();

    IReadOnlySet<VirtualKey> CurrentlyPressedKeys { get; }
    IReadOnlySet<MouseButton> CurrentlyPressedMouseButtons { get; }
    IReadOnlySet<VirtualKey> PendingReleaseKeys { get; }
    IReadOnlySet<MouseButton> PendingReleaseMouseButtons { get; }

    int RetryPendingReleases();
}
