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

    [Test]
    public void PhysicalInput_TwoButtonsSameController_MappedToSameKey_MaintainsKeyWhileOneIsHeld()
    {
        // Configura perfil onde tanto A quanto B disparam a tecla Space
        var profile = new MappingProfile { Name = "DualSpace", SchemaVersion = 1 };
        profile.ButtonBindings[GamepadButton.A] = InputAction.FromKey(VirtualKey.Space);
        profile.ButtonBindings[GamepadButton.B] = InputAction.FromKey(VirtualKey.Space);

        _engine.SetProfile(profile);
        _engine.Arm();

        // 1. Pressiona A -> Espaço deve ser enviado (refCount = 1)
        var dict1 = new Dictionary<GamepadButton, bool> { [GamepadButton.A] = true, [GamepadButton.B] = false };
        _engine.ProcessSnapshot(CreateCustomSnapshot("pad_1", dict1));

        Assert.That(_sink.CurrentlyPressedKeys, Contains.Item(VirtualKey.Space));
        Assert.That(_engine.ActiveVirtualKeys, Contains.Item(VirtualKey.Space));

        // 2. Pressiona B também (ambos A e B pressionados) -> Espaço continua ativo (refCount = 2)
        var dict2 = new Dictionary<GamepadButton, bool> { [GamepadButton.A] = true, [GamepadButton.B] = true };
        _engine.ProcessSnapshot(CreateCustomSnapshot("pad_1", dict2));

        Assert.That(_sink.CurrentlyPressedKeys, Contains.Item(VirtualKey.Space));

        // 3. Solta A mantendo B pressionado -> ESPAÇO DEVE CONTINUAR PRESSIONADO!
        var dict3 = new Dictionary<GamepadButton, bool> { [GamepadButton.A] = false, [GamepadButton.B] = true };
        _engine.ProcessSnapshot(CreateCustomSnapshot("pad_1", dict3));

        Assert.That(_sink.CurrentlyPressedKeys, Contains.Item(VirtualKey.Space), "Espaço não pode ser liberado prematuramente enquanto o Botão B continuar mantido!");
        Assert.That(_engine.ActiveVirtualKeys, Contains.Item(VirtualKey.Space));

        // 4. Solta B -> Agora que nenhuma entrada física exige Espaço, deve ser liberado!
        var dict4 = new Dictionary<GamepadButton, bool> { [GamepadButton.A] = false, [GamepadButton.B] = false };
        _engine.ProcessSnapshot(CreateCustomSnapshot("pad_1", dict4));

        Assert.That(_sink.CurrentlyPressedKeys, Does.Not.Contain(VirtualKey.Space));
        Assert.That(_engine.ActiveVirtualKeys, Does.Not.Contain(VirtualKey.Space));
    }

    [Test]
    public void PhysicalInput_ButtonAndTrigger_MappedToSameKey_MaintainsKeyUntilAllPhysicalInputsReleased()
    {
        var profile = new MappingProfile { Name = "ButtonAndTrigger", SchemaVersion = 1 };
        profile.ButtonBindings[GamepadButton.A] = InputAction.FromKey(VirtualKey.Space);
        profile.TriggerBindings[GamepadAxis.TriggerRight] = InputAction.FromKey(VirtualKey.Space);

        _engine.SetProfile(profile);
        _engine.Arm();

        // 1. Pressiona botão A
        _engine.ProcessSnapshot(CreateCustomSnapshot(
            "pad_1",
            new Dictionary<GamepadButton, bool> { [GamepadButton.A] = true },
            new Dictionary<GamepadAxis, float> { [GamepadAxis.TriggerRight] = 0.0f }));

        Assert.That(_sink.CurrentlyPressedKeys, Contains.Item(VirtualKey.Space));

        // 2. Puxa gatilho RT
        _engine.ProcessSnapshot(CreateCustomSnapshot(
            "pad_1",
            new Dictionary<GamepadButton, bool> { [GamepadButton.A] = true },
            new Dictionary<GamepadAxis, float> { [GamepadAxis.TriggerRight] = 0.9f }));

        Assert.That(_sink.CurrentlyPressedKeys, Contains.Item(VirtualKey.Space));

        // 3. Solta botão A mantendo RT puxado -> Espaço permanece ativo
        _engine.ProcessSnapshot(CreateCustomSnapshot(
            "pad_1",
            new Dictionary<GamepadButton, bool> { [GamepadButton.A] = false },
            new Dictionary<GamepadAxis, float> { [GamepadAxis.TriggerRight] = 0.9f }));

        Assert.That(_sink.CurrentlyPressedKeys, Contains.Item(VirtualKey.Space));

        // 4. Solta gatilho RT -> Espaço liberado
        _engine.ProcessSnapshot(CreateCustomSnapshot(
            "pad_1",
            new Dictionary<GamepadButton, bool> { [GamepadButton.A] = false },
            new Dictionary<GamepadAxis, float> { [GamepadAxis.TriggerRight] = 0.0f }));

        Assert.That(_sink.CurrentlyPressedKeys, Does.Not.Contain(VirtualKey.Space));
    }

    [Test]
    public void BackStartCombo_IsIsolatedPerDevice_DoesNotFlickerOnAlternatingSnapshots()
    {
        // Começa desarmado
        Assert.That(_engine.IsArmed, Is.False);

        // Pad 1 aciona Back + Start -> Deve Armar
        var pad1Combo = CreateCustomSnapshot(
            "pad_1",
            new Dictionary<GamepadButton, bool> { [GamepadButton.Back] = true, [GamepadButton.Start] = true });
        _engine.ProcessSnapshot(pad1Combo);
        Assert.That(_engine.IsArmed, Is.True);

        // Pad 2 envia snapshot normal sem atalho -> Não pode interferir ou desarmar!
        var pad2Normal = CreateCustomSnapshot(
            "pad_2",
            new Dictionary<GamepadButton, bool> { [GamepadButton.Back] = false, [GamepadButton.Start] = false });
        _engine.ProcessSnapshot(pad2Normal);
        Assert.That(_engine.IsArmed, Is.True);

        // Pad 1 continua segurando Back + Start -> Não pode alternar repetidamente
        _engine.ProcessSnapshot(pad1Combo);
        Assert.That(_engine.IsArmed, Is.True);

        // Pad 1 solta o atalho
        var pad1Released = CreateCustomSnapshot(
            "pad_1",
            new Dictionary<GamepadButton, bool> { [GamepadButton.Back] = false, [GamepadButton.Start] = false });
        _engine.ProcessSnapshot(pad1Released);
        Assert.That(_engine.IsArmed, Is.True);

        // Pad 1 aperta novamente Back + Start -> Deve Desarmar
        _engine.ProcessSnapshot(pad1Combo);
        Assert.That(_engine.IsArmed, Is.False);
    }

    private static ControllerSnapshot CreateCustomSnapshot(
        string deviceId,
        IDictionary<GamepadButton, bool>? buttonsToSet = null,
        IDictionary<GamepadAxis, float>? axesToSet = null)
    {
        var buttons = new Dictionary<GamepadButton, bool>();
        foreach (GamepadButton b in Enum.GetValues<GamepadButton>())
        {
            buttons[b] = false;
        }
        if (buttonsToSet != null)
        {
            foreach (var (b, val) in buttonsToSet)
            {
                buttons[b] = val;
            }
        }

        var axes = new Dictionary<GamepadAxis, float>();
        foreach (GamepadAxis a in Enum.GetValues<GamepadAxis>())
        {
            axes[a] = 0.0f;
        }
        if (axesToSet != null)
        {
            foreach (var (a, val) in axesToSet)
            {
                axes[a] = val;
            }
        }

        return new ControllerSnapshot(deviceId, "Virtual Controller", true, DateTime.UtcNow, buttons, axes);
    }
}
