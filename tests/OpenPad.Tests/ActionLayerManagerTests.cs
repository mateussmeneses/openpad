using NUnit.Framework;
using OpenPad.Core.Engine;
using OpenPad.Core.Models;

namespace OpenPad.Tests;

[TestFixture]
public class ActionLayerManagerTests
{
    private ActionLayerManager _manager = null!;
    private ActionLayer _sniperLayer = null!;
    private ActionLayer _numpadLayer = null!;

    [SetUp]
    public void Setup()
    {
        _manager = new ActionLayerManager();

        _sniperLayer = new ActionLayer
        {
            Id = "layer_sniper",
            Name = "Sniper Shift",
            TriggerButton = GamepadButton.LeftShoulder,
            Mode = LayerActivationMode.Hold,
            OverrideBindings = new Dictionary<GamepadButton, InputAction>
            {
                { GamepadButton.A, InputAction.FromKey(VirtualKey.Shift) },
                { GamepadButton.RightShoulder, InputAction.FromMouse(MouseButton.Right) }
            }
        };

        _numpadLayer = new ActionLayer
        {
            Id = "layer_numpad",
            Name = "Numpad Toggle",
            TriggerButton = GamepadButton.Back,
            Mode = LayerActivationMode.Toggle,
            OverrideBindings = new Dictionary<GamepadButton, InputAction>
            {
                { GamepadButton.A, InputAction.FromKey(VirtualKey.D1) }
            }
        };

        _manager.AddLayer(_sniperLayer);
        _manager.AddLayer(_numpadLayer);
    }

    [Test]
    public void ProcessTriggerEvent_HoldMode_ActivatesOnPressAndRevertsOnRelease()
    {
        Assert.That(_manager.IsLayerActive, Is.False);

        // Press Left Shoulder -> Activates layer
        bool consumed = _manager.ProcessTriggerEvent(GamepadButton.LeftShoulder, isPressed: true);
        Assert.That(consumed, Is.True);
        Assert.That(_manager.IsLayerActive, Is.True);
        Assert.That(_manager.ActiveLayerId, Is.EqualTo("layer_sniper"));

        // Release Left Shoulder -> Deactivates layer
        consumed = _manager.ProcessTriggerEvent(GamepadButton.LeftShoulder, isPressed: false);
        Assert.That(consumed, Is.True);
        Assert.That(_manager.IsLayerActive, Is.False);
        Assert.That(_manager.ActiveLayerId, Is.Null);
    }

    [Test]
    public void ProcessTriggerEvent_ToggleMode_TogglesLayerState()
    {
        Assert.That(_manager.IsLayerActive, Is.False);

        // First press Back -> Activates numpad layer
        bool consumed = _manager.ProcessTriggerEvent(GamepadButton.Back, isPressed: true);
        Assert.That(consumed, Is.True);
        Assert.That(_manager.ActiveLayerId, Is.EqualTo("layer_numpad"));

        // Release Back -> Stays active (because it's Toggle)
        consumed = _manager.ProcessTriggerEvent(GamepadButton.Back, isPressed: false);
        Assert.That(consumed, Is.True);
        Assert.That(_manager.ActiveLayerId, Is.EqualTo("layer_numpad"));

        // Second press Back -> Deactivates
        consumed = _manager.ProcessTriggerEvent(GamepadButton.Back, isPressed: true);
        Assert.That(consumed, Is.True);
        Assert.That(_manager.ActiveLayerId, Is.Null);
    }

    [Test]
    public void ResolveAction_PrioritizesLayerOverrideOverBaseAction()
    {
        var baseActionA = InputAction.FromKey(VirtualKey.Space);

        // In base layer, resolves to baseAction
        var resolved = _manager.ResolveAction(GamepadButton.A, baseActionA);
        Assert.That(resolved?.Key, Is.EqualTo(VirtualKey.Space));

        // Activate sniper layer
        _manager.ProcessTriggerEvent(GamepadButton.LeftShoulder, isPressed: true);

        // Now resolves to Shift from override
        resolved = _manager.ResolveAction(GamepadButton.A, baseActionA);
        Assert.That(resolved?.Key, Is.EqualTo(VirtualKey.Shift));
    }

    [Test]
    public void ResolveAction_FallsBackToBaseActionWhenNotOverriddenInLayer()
    {
        var baseActionB = InputAction.FromKey(VirtualKey.Escape);

        // Activate sniper layer (which does NOT override B)
        _manager.ProcessTriggerEvent(GamepadButton.LeftShoulder, isPressed: true);

        var resolved = _manager.ResolveAction(GamepadButton.B, baseActionB);
        Assert.That(resolved?.Key, Is.EqualTo(VirtualKey.Escape));
    }

    [Test]
    public void RemoveLayer_WhenActive_RevertsToBaseLayer()
    {
        _manager.ProcessTriggerEvent(GamepadButton.LeftShoulder, isPressed: true);
        Assert.That(_manager.ActiveLayerId, Is.EqualTo("layer_sniper"));

        bool removed = _manager.RemoveLayer("layer_sniper");
        Assert.That(removed, Is.True);
        Assert.That(_manager.IsLayerActive, Is.False);
        Assert.That(_manager.ActiveLayerId, Is.Null);
    }

    [Test]
    public void Reset_ClearsActiveLayer()
    {
        _manager.ProcessTriggerEvent(GamepadButton.LeftShoulder, isPressed: true);
        Assert.That(_manager.IsLayerActive, Is.True);

        _manager.Reset();
        Assert.That(_manager.IsLayerActive, Is.False);
        Assert.That(_manager.ActiveLayerId, Is.Null);
    }
}
