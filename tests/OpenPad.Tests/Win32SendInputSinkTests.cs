using NUnit.Framework;
using OpenPad.Core.Models;
using OpenPad.Input;

namespace OpenPad.Tests;

[TestFixture]
public class Win32SendInputSinkTests
{
    [Test]
    public void SendKeyDown_WhenNativeSucceeds_TracksKeyAndIncrementsCount()
    {
        uint mockHandler(uint nInputs, Win32SendInputSink.NativeInput[] pInputs, int cbSize) => nInputs;

        using var sink = new Win32SendInputSink(mockHandler);

        sink.SendKeyDown(VirtualKey.Space);

        Assert.That(sink.CurrentlyPressedKeys, Contains.Item(VirtualKey.Space));
        Assert.That(sink.SuccessfulEventCount, Is.EqualTo(1));
        Assert.That(sink.FailedEventCount, Is.EqualTo(0));
    }

    [Test]
    public void SendKeyDown_WhenNativeReturnsZero_DoesNotTrackKeyAndIncrementsFailures()
    {
        uint mockHandler(uint nInputs, Win32SendInputSink.NativeInput[] pInputs, int cbSize) => 0;

        using var sink = new Win32SendInputSink(mockHandler);
        string? diagMessage = null;
        sink.DiagnosticsLogged += (_, msg) => diagMessage = msg;

        sink.SendKeyDown(VirtualKey.Space);

        // Não deve assumir que a tecla está pressionada se o envio falhou
        Assert.That(sink.CurrentlyPressedKeys, Does.Not.Contain(VirtualKey.Space));
        Assert.That(sink.SuccessfulEventCount, Is.EqualTo(0));
        Assert.That(sink.FailedEventCount, Is.EqualTo(1));
        Assert.That(diagMessage, Is.Not.Null);
        Assert.That(diagMessage, Contains.Substring("Win32 SendInput rejeitado"));
    }

    [Test]
    public void SendKeyUp_AlwaysRemovesKeyFromPressedSet()
    {
        uint mockHandler(uint nInputs, Win32SendInputSink.NativeInput[] pInputs, int cbSize) => nInputs;

        using var sink = new Win32SendInputSink(mockHandler);

        sink.SendKeyDown(VirtualKey.Space);
        Assert.That(sink.CurrentlyPressedKeys, Contains.Item(VirtualKey.Space));

        sink.SendKeyUp(VirtualKey.Space);
        Assert.That(sink.CurrentlyPressedKeys, Does.Not.Contain(VirtualKey.Space));
    }

    [Test]
    public void SendMouseDownAndUp_TracksMouseButtonState()
    {
        uint mockHandler(uint nInputs, Win32SendInputSink.NativeInput[] pInputs, int cbSize) => nInputs;

        using var sink = new Win32SendInputSink(mockHandler);

        sink.SendMouseDown(MouseButton.Left);
        Assert.That(sink.CurrentlyPressedMouseButtons, Contains.Item(MouseButton.Left));

        sink.SendMouseUp(MouseButton.Left);
        Assert.That(sink.CurrentlyPressedMouseButtons, Does.Not.Contain(MouseButton.Left));
    }

    [Test]
    public void ReleaseAll_ClearsAllHeldKeysAndButtonsSafely()
    {
        uint mockHandler(uint nInputs, Win32SendInputSink.NativeInput[] pInputs, int cbSize) => nInputs;

        using var sink = new Win32SendInputSink(mockHandler);

        sink.SendKeyDown(VirtualKey.A);
        sink.SendKeyDown(VirtualKey.B);
        sink.SendMouseDown(MouseButton.Right);

        Assert.That(sink.CurrentlyPressedKeys.Count, Is.EqualTo(2));
        Assert.That(sink.CurrentlyPressedMouseButtons.Count, Is.EqualTo(1));

        sink.ReleaseAll();

        Assert.That(sink.CurrentlyPressedKeys, Is.Empty);
        Assert.That(sink.CurrentlyPressedMouseButtons, Is.Empty);
    }
}
