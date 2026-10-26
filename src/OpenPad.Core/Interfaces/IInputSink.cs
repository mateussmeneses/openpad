using OpenPad.Core.Models;

namespace OpenPad.Core.Interfaces;

public interface IInputSink : IDisposable
{
    void SendKeyDown(VirtualKey key);
    void SendKeyUp(VirtualKey key);
    void SendMouseDown(MouseButton button);
    void SendMouseUp(MouseButton button);
    void ReleaseAll();

    IReadOnlySet<VirtualKey> CurrentlyPressedKeys { get; }
    IReadOnlySet<MouseButton> CurrentlyPressedMouseButtons { get; }
}
