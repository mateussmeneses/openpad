using NUnit.Framework;
using OpenPad.Core.Engine;
using OpenPad.Core.Models;
using OpenPad.Core.Testing;

namespace OpenPad.Tests;

[TestFixture]
public class MappingEngineTests
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
            Name = "TestProfile",
            ButtonBindings = new Dictionary<GamepadButton, InputAction>
            {
                [GamepadButton.A] = InputAction.FromKey(VirtualKey.Space),
                [GamepadButton.B] = InputAction.FromKey(VirtualKey.C),
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

    [Test]
    public void InitialState_IsDisarmed_AndNoInputsSent()
    {
        // DoD: O app inicia DESARMADO por padrão
        Assert.That(_engine.IsArmed, Is.False);

        // Dispara pressão de botão com engine desarmado
        var snapshot = CreateSnapshot(GamepadButton.A, true);
        _engine.ProcessSnapshot(snapshot);

        Assert.That(_sink.RecordedEvents, Is.Empty, "Nenhum input deve ser gerado quando o motor estiver desarmado.");
        Assert.That(_engine.ActiveVirtualKeys, Is.Empty);
    }

    [Test]
    public void ArmAndDisarm_TogglesStateCorrectly()
    {
        _engine.Arm();
        Assert.That(_engine.IsArmed, Is.True);

        _engine.Disarm();
        Assert.That(_engine.IsArmed, Is.False);
    }

    [Test]
    public void PressAndReleaseButton_GeneratesSingleKeyDownAndKeyUp()
    {
        _engine.Arm();

        // 1. Pressiona o botão A (mapeado para Espaço)
        var pressSnapshot = CreateSnapshot(GamepadButton.A, true);
        _engine.ProcessSnapshot(pressSnapshot);

        Assert.That(_sink.RecordedEvents.Count, Is.EqualTo(1));
        Assert.That(_sink.RecordedEvents[0].EventType, Is.EqualTo(InputEventType.KeyDown));
        Assert.That(_sink.RecordedEvents[0].Key, Is.EqualTo(VirtualKey.Space));
        Assert.That(_engine.ActiveVirtualKeys.Contains(VirtualKey.Space), Is.True);

        // 2. Continua segurando o botão A (múltiplos frames com mesmo estado) - NÃO deve repetir KeyDown!
        _engine.ProcessSnapshot(pressSnapshot);
        _engine.ProcessSnapshot(pressSnapshot);
        Assert.That(_sink.RecordedEvents.Count, Is.EqualTo(1), "Não deve gerar repeat incontrolado de KeyDown.");

        // 3. Solta o botão A
        var releaseSnapshot = CreateSnapshot(GamepadButton.A, false);
        _engine.ProcessSnapshot(releaseSnapshot);

        Assert.That(_sink.RecordedEvents.Count, Is.EqualTo(2));
        Assert.That(_sink.RecordedEvents[1].EventType, Is.EqualTo(InputEventType.KeyUp));
        Assert.That(_sink.RecordedEvents[1].Key, Is.EqualTo(VirtualKey.Space));
        Assert.That(_engine.ActiveVirtualKeys.Contains(VirtualKey.Space), Is.False);
    }

    [Test]
    public void MouseButtonMapping_GeneratesMouseDownAndMouseUp()
    {
        _engine.Arm();

        // RightShoulder está mapeado para Left Mouse Button
        var press = CreateSnapshot(GamepadButton.RightShoulder, true);
        _engine.ProcessSnapshot(press);

        Assert.That(_sink.RecordedEvents.Count, Is.EqualTo(1));
        Assert.That(_sink.RecordedEvents[0].EventType, Is.EqualTo(InputEventType.MouseDown));
        Assert.That(_sink.RecordedEvents[0].MouseButton, Is.EqualTo(MouseButton.Left));

        var release = CreateSnapshot(GamepadButton.RightShoulder, false);
        _engine.ProcessSnapshot(release);

        Assert.That(_sink.RecordedEvents.Count, Is.EqualTo(2));
        Assert.That(_sink.RecordedEvents[1].EventType, Is.EqualTo(InputEventType.MouseUp));
        Assert.That(_sink.RecordedEvents[1].MouseButton, Is.EqualTo(MouseButton.Left));
    }

    [Test]
    public void DisarmWhileKeyHeld_ImmediatelyReleasesAllInputs()
    {
        _engine.Arm();

        // Pressiona A
        _engine.ProcessSnapshot(CreateSnapshot(GamepadButton.A, true));
        Assert.That(_engine.ActiveVirtualKeys.Contains(VirtualKey.Space), Is.True);

        // Desarma o motor com a tecla ainda pressionada
        _engine.Disarm();

        Assert.That(_engine.IsArmed, Is.False);
        Assert.That(_engine.ActiveVirtualKeys, Is.Empty);
        Assert.That(_sink.RecordedEvents.Any(e => e.EventType == InputEventType.ReleaseAll), Is.True,
            "Ao desarmar, deve emitir ReleaseAll imediatamente.");
    }

    [Test]
    public void DisconnectWhileKeyHeld_ImmediatelyReleasesAllInputs()
    {
        _engine.Arm();

        // Pressiona A
        _engine.ProcessSnapshot(CreateSnapshot(GamepadButton.A, true, deviceId: "ctrl-1"));
        Assert.That(_engine.ActiveVirtualKeys.Contains(VirtualKey.Space), Is.True);

        // Desconecta o controle
        _engine.HandleDeviceDisconnected("ctrl-1");

        Assert.That(_engine.ActiveVirtualKeys, Is.Empty);
        Assert.That(_sink.RecordedEvents.Any(e => e.EventType == InputEventType.ReleaseAll), Is.True,
            "Ao desconectar o controle, deve liberar todos os inputs.");
    }

    [Test]
    public void SwitchProfileWhileKeyHeld_ReleasesAllInputs()
    {
        _engine.Arm();

        // Pressiona A
        _engine.ProcessSnapshot(CreateSnapshot(GamepadButton.A, true));
        Assert.That(_engine.ActiveVirtualKeys.Contains(VirtualKey.Space), Is.True);

        // Troca de perfil
        var newProfile = MappingProfile.CreateDefault("NovoPerfil");
        _engine.SetProfile(newProfile);

        Assert.That(_engine.ActiveVirtualKeys, Is.Empty);
        Assert.That(_sink.RecordedEvents.Any(e => e.EventType == InputEventType.ReleaseAll), Is.True,
            "Ao trocar de perfil, deve liberar todos os inputs pendentes.");
    }

    [Test]
    public void UnmappedButtons_DoNotGenerateOutput()
    {
        _engine.Arm();

        // X não está mapeado no _profile de teste
        var pressX = CreateSnapshot(GamepadButton.X, true);
        _engine.ProcessSnapshot(pressX);

        Assert.That(_sink.RecordedEvents, Is.Empty);
    }

    private static ControllerSnapshot CreateSnapshot(GamepadButton button, bool pressed, string deviceId = "test-controller")
    {
        var buttons = new Dictionary<GamepadButton, bool>();
        foreach (GamepadButton b in Enum.GetValues<GamepadButton>())
        {
            buttons[b] = false;
        }
        buttons[button] = pressed;

        var axes = new Dictionary<GamepadAxis, float>();
        foreach (GamepadAxis a in Enum.GetValues<GamepadAxis>())
        {
            axes[a] = 0.0f;
        }

        return new ControllerSnapshot(deviceId, "Test Controller", true, DateTime.UtcNow, buttons, axes);
    }
}
