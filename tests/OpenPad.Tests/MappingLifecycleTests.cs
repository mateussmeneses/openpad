using NUnit.Framework;
using OpenPad.Core.Engine;
using OpenPad.Core.Models;
using OpenPad.Core.Testing;

namespace OpenPad.Tests;

[TestFixture]
public class MappingLifecycleTests
{
    private RecordingInputSink _sink = null!;
    private MappingProfile _profile = null!;
    private MappingEngine _engine = null!;

    [SetUp]
    public void SetUp()
    {
        _sink = new RecordingInputSink();
        _profile = new MappingProfile
        {
            SchemaVersion = 1,
            Name = "LifecycleTestProfile",
            ButtonBindings = new Dictionary<GamepadButton, InputAction>
            {
                [GamepadButton.A] = InputAction.FromKey(VirtualKey.Space),
                [GamepadButton.B] = InputAction.FromKey(VirtualKey.E),
                [GamepadButton.RightShoulder] = InputAction.FromMouse(MouseButton.Left)
            }
        };
        _engine = new MappingEngine(_sink, _profile);
    }

    [TearDown]
    public void TearDown()
    {
        _engine.Dispose();
        _sink.Dispose();
    }

    private static ControllerSnapshot CreateSnapshot(string deviceId, GamepadButton button, bool isPressed)
    {
        var buttons = new Dictionary<GamepadButton, bool>();
        foreach (GamepadButton b in Enum.GetValues<GamepadButton>())
        {
            buttons[b] = false;
        }
        buttons[button] = isPressed;

        var axes = new Dictionary<GamepadAxis, float>();
        foreach (GamepadAxis a in Enum.GetValues<GamepadAxis>())
        {
            axes[a] = 0.0f;
        }

        return new ControllerSnapshot(deviceId, "Virtual Controller", true, DateTime.UtcNow, buttons, axes);
    }

    [Test]
    public void MultiController_TwoControllersMappingSameKey_UsesRefCounting()
    {
        _engine.Arm();

        // 1. Controle 1 pressiona Botão A -> KeySpace
        _engine.ProcessSnapshot(CreateSnapshot("pad_1", GamepadButton.A, true));
        Assert.That(_sink.CurrentlyPressedKeys, Contains.Item(VirtualKey.Space));
        Assert.That(_engine.ActiveVirtualKeys, Contains.Item(VirtualKey.Space));
        int keyDownCount = _sink.RecordedEvents.Count(e => e.EventType == InputEventType.KeyDown && e.Key == VirtualKey.Space);
        Assert.That(keyDownCount, Is.EqualTo(1));

        // 2. Controle 2 pressiona também Botão A -> KeySpace
        _engine.ProcessSnapshot(CreateSnapshot("pad_2", GamepadButton.A, true));
        // Não deve reenviar KeyDown duplicado no sistema operacional (ref count = 2)
        keyDownCount = _sink.RecordedEvents.Count(e => e.EventType == InputEventType.KeyDown && e.Key == VirtualKey.Space);
        Assert.That(keyDownCount, Is.EqualTo(1));

        // 3. Controle 1 solta Botão A
        _engine.ProcessSnapshot(CreateSnapshot("pad_1", GamepadButton.A, false));
        // Tecla Space DEVE CONTINUAR pressionada pois Controle 2 ainda está segurando!
        Assert.That(_sink.CurrentlyPressedKeys, Contains.Item(VirtualKey.Space));
        Assert.That(_engine.ActiveVirtualKeys, Contains.Item(VirtualKey.Space));
        int keyUpCount = _sink.RecordedEvents.Count(e => e.EventType == InputEventType.KeyUp && e.Key == VirtualKey.Space);
        Assert.That(keyUpCount, Is.EqualTo(0));

        // 4. Controle 2 finalmente solta Botão A
        _engine.ProcessSnapshot(CreateSnapshot("pad_2", GamepadButton.A, false));
        // Agora sim a tecla Space é liberada!
        Assert.That(_sink.CurrentlyPressedKeys, Does.Not.Contain(VirtualKey.Space));
        Assert.That(_engine.ActiveVirtualKeys, Does.Not.Contain(VirtualKey.Space));
        keyUpCount = _sink.RecordedEvents.Count(e => e.EventType == InputEventType.KeyUp && e.Key == VirtualKey.Space);
        Assert.That(keyUpCount, Is.EqualTo(1));
    }

    [Test]
    public void MultiController_DisconnectingOneDevice_DoesNotReleaseOtherDeviceKeys()
    {
        _engine.Arm();

        // Pad 1 segura A (Space)
        _engine.ProcessSnapshot(CreateSnapshot("pad_1", GamepadButton.A, true));
        // Pad 2 segura B (E)
        _engine.ProcessSnapshot(CreateSnapshot("pad_2", GamepadButton.B, true));

        Assert.That(_engine.ActiveVirtualKeys, Contains.Item(VirtualKey.Space));
        Assert.That(_engine.ActiveVirtualKeys, Contains.Item(VirtualKey.E));

        // Pad 2 desconecta
        _engine.HandleDeviceDisconnected("pad_2");

        // E deve ser liberado, mas Space do Pad 1 DEVE PERMANECER ATIVO!
        Assert.That(_sink.CurrentlyPressedKeys, Does.Not.Contain(VirtualKey.E));
        Assert.That(_sink.CurrentlyPressedKeys, Contains.Item(VirtualKey.Space));
        Assert.That(_engine.ActiveVirtualKeys, Contains.Item(VirtualKey.Space));
    }

    [Test]
    public void Lifecycle_TenStepSequence_ExecutesDeterministically()
    {
        // 1. Conectar controle
        var snapshot = CreateSnapshot("device_0", GamepadButton.A, false);
        _engine.ProcessSnapshot(snapshot);
        Assert.That(_engine.IsArmed, Is.False);

        // 2. Armar motor
        _engine.Arm();
        Assert.That(_engine.IsArmed, Is.True);

        // 3. Pressionar botão mapeado
        _engine.ProcessSnapshot(CreateSnapshot("device_0", GamepadButton.A, true));

        // 4. Confirmar evento de saída
        Assert.That(_sink.CurrentlyPressedKeys, Contains.Item(VirtualKey.Space));
        Assert.That(_engine.ActiveVirtualKeys, Contains.Item(VirtualKey.Space));

        // 5. Soltar botão
        _engine.ProcessSnapshot(CreateSnapshot("device_0", GamepadButton.A, false));

        // 6. Confirmar liberação
        Assert.That(_sink.CurrentlyPressedKeys, Does.Not.Contain(VirtualKey.Space));
        Assert.That(_engine.ActiveVirtualKeys, Does.Not.Contain(VirtualKey.Space));

        // 7. Pressionar e desarmar com botão pressionado
        _engine.ProcessSnapshot(CreateSnapshot("device_0", GamepadButton.A, true));
        Assert.That(_sink.CurrentlyPressedKeys, Contains.Item(VirtualKey.Space));
        _engine.Disarm();
        Assert.That(_sink.CurrentlyPressedKeys, Does.Not.Contain(VirtualKey.Space));
        Assert.That(_engine.ActiveVirtualKeys, Is.Empty);

        // 8. Re-armar, pressionar e desconectar durante entrada ativa
        _engine.Arm();
        _engine.ProcessSnapshot(CreateSnapshot("device_0", GamepadButton.A, true));
        Assert.That(_sink.CurrentlyPressedKeys, Contains.Item(VirtualKey.Space));
        _engine.HandleDeviceDisconnected("device_0");
        Assert.That(_sink.CurrentlyPressedKeys, Does.Not.Contain(VirtualKey.Space));
        Assert.That(_engine.ActiveVirtualKeys, Is.Empty);

        // 9. Re-conectar, pressionar e trocar perfil durante entrada ativa
        _engine.ProcessSnapshot(CreateSnapshot("device_0", GamepadButton.A, true));
        Assert.That(_sink.CurrentlyPressedKeys, Contains.Item(VirtualKey.Space));
        var newProfile = new MappingProfile { Name = "Profile2", SchemaVersion = 1 };
        _engine.SetProfile(newProfile);
        Assert.That(_sink.CurrentlyPressedKeys, Does.Not.Contain(VirtualKey.Space));
        Assert.That(_engine.ActiveVirtualKeys, Is.Empty);

        // 10. Re-conectar, pressionar e encerrar aplicação (Dispose)
        _engine.SetProfile(_profile);
        _engine.ProcessSnapshot(CreateSnapshot("device_0", GamepadButton.A, true));
        Assert.That(_sink.CurrentlyPressedKeys, Contains.Item(VirtualKey.Space));
        _engine.Dispose();
        Assert.That(_sink.CurrentlyPressedKeys, Does.Not.Contain(VirtualKey.Space));
        Assert.That(_engine.ActiveVirtualKeys, Is.Empty);
    }
}
