using NUnit.Framework;
using OpenPad.Core.Engine;
using OpenPad.Core.Models;
using OpenPad.Core.Testing;

namespace OpenPad.Tests;

[TestFixture]
public class DualActionTriggerEngineTests
{
    private RecordingInputSink _sink = null!;

    [SetUp]
    public void SetUp()
    {
        _sink = new RecordingInputSink();
    }

    [TearDown]
    public void TearDown()
    {
        _sink.Dispose();
    }

    [Test]
    public void Update_BelowSoftThreshold_RemainsReleased()
    {
        var config = new DualActionTriggerConfig
        {
            SoftThreshold = 0.3f,
            FullThreshold = 0.8f,
            SoftAction = InputAction.FromKey(VirtualKey.Shift),
            FullAction = InputAction.FromMouse(MouseButton.Left)
        };
        var engine = new DualActionTriggerEngine(config);

        var stage = engine.Update(0.15f, _sink);

        Assert.That(stage, Is.EqualTo(TriggerStage.Released));
        Assert.That(_sink.CurrentlyPressedKeys, Is.Empty);
        Assert.That(_sink.CurrentlyPressedMouseButtons, Is.Empty);
    }

    [Test]
    public void Update_SoftPull_EngagesSoftActionOnly()
    {
        var config = new DualActionTriggerConfig
        {
            SoftThreshold = 0.3f,
            FullThreshold = 0.8f,
            SoftAction = InputAction.FromKey(VirtualKey.Shift),
            FullAction = InputAction.FromMouse(MouseButton.Left)
        };
        var engine = new DualActionTriggerEngine(config);

        var stage = engine.Update(0.5f, _sink);

        Assert.That(stage, Is.EqualTo(TriggerStage.SoftPull));
        Assert.That(_sink.CurrentlyPressedKeys, Contains.Item(VirtualKey.Shift));
        Assert.That(_sink.CurrentlyPressedMouseButtons, Is.Empty);
    }

    [Test]
    public void Update_FullPull_EngagesBothActionsWhenConfigured()
    {
        var config = new DualActionTriggerConfig
        {
            SoftThreshold = 0.3f,
            FullThreshold = 0.8f,
            SoftAction = InputAction.FromKey(VirtualKey.Shift),
            FullAction = InputAction.FromMouse(MouseButton.Left),
            ReleaseSoftOnFullPull = false
        };
        var engine = new DualActionTriggerEngine(config);

        engine.Update(0.5f, _sink);
        var stage = engine.Update(0.95f, _sink);

        Assert.That(stage, Is.EqualTo(TriggerStage.FullPull));
        Assert.That(_sink.CurrentlyPressedKeys, Contains.Item(VirtualKey.Shift));
        Assert.That(_sink.CurrentlyPressedMouseButtons, Contains.Item(MouseButton.Left));
    }

    [Test]
    public void Update_FullPull_ReleasesSoftWhenReleaseSoftConfigured()
    {
        var config = new DualActionTriggerConfig
        {
            SoftThreshold = 0.3f,
            FullThreshold = 0.8f,
            SoftAction = InputAction.FromKey(VirtualKey.Shift),
            FullAction = InputAction.FromMouse(MouseButton.Left),
            ReleaseSoftOnFullPull = true
        };
        var engine = new DualActionTriggerEngine(config);

        engine.Update(0.5f, _sink);
        Assert.That(_sink.CurrentlyPressedKeys, Contains.Item(VirtualKey.Shift));

        var stage = engine.Update(0.95f, _sink);
        Assert.That(stage, Is.EqualTo(TriggerStage.FullPull));
        Assert.That(_sink.CurrentlyPressedKeys, Is.Not.Contains(VirtualKey.Shift));
        Assert.That(_sink.CurrentlyPressedMouseButtons, Contains.Item(MouseButton.Left));
    }

    [Test]
    public void Update_ReleaseFromFull_ReleasesAllEngaged()
    {
        var config = new DualActionTriggerConfig
        {
            SoftThreshold = 0.3f,
            FullThreshold = 0.8f,
            SoftAction = InputAction.FromKey(VirtualKey.Shift),
            FullAction = InputAction.FromMouse(MouseButton.Left)
        };
        var engine = new DualActionTriggerEngine(config);

        engine.Update(0.9f, _sink);
        var stage = engine.Update(0.0f, _sink);

        Assert.That(stage, Is.EqualTo(TriggerStage.Released));
        Assert.That(_sink.CurrentlyPressedKeys, Is.Empty);
        Assert.That(_sink.CurrentlyPressedMouseButtons, Is.Empty);
    }
}
