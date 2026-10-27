using NUnit.Framework;
using OpenPad.Core.Engine;
using OpenPad.Core.Models;

namespace OpenPad.Tests;

[TestFixture]
public class GamepadSlotManagerTests
{
    private GamepadSlotManager _manager = null!;

    [SetUp]
    public void SetUp()
    {
        _manager = new GamepadSlotManager(maxSlots: 4);
    }

    [Test]
    public void RegisterController_AssignsFirstAvailableSlotSequentially()
    {
        var c1 = new ControllerInfo("id-1", "Xbox Wireless Controller", true);
        var c2 = new ControllerInfo("id-2", "DualSense Wireless Controller", true);

        int slot1 = _manager.RegisterController(c1);
        int slot2 = _manager.RegisterController(c2);

        Assert.That(slot1, Is.EqualTo(0));
        Assert.That(slot2, Is.EqualTo(1));
        Assert.That(_manager.OccupiedCount, Is.EqualTo(2));
        Assert.That(_manager.GetDeviceInSlot(0)?.Name, Is.EqualTo("Xbox Wireless Controller"));
        Assert.That(_manager.GetDeviceInSlot(1)?.Name, Is.EqualTo("DualSense Wireless Controller"));
    }

    [Test]
    public void RegisterController_SameDeviceTwice_PreservesExistingSlot()
    {
        var c1 = new ControllerInfo("id-1", "Xbox Wireless Controller", true);
        _manager.RegisterController(c1);

        var c1Updated = new ControllerInfo("id-1", "Xbox Wireless Controller (Renamed)", true);
        int slot = _manager.RegisterController(c1Updated);

        Assert.That(slot, Is.EqualTo(0));
        Assert.That(_manager.OccupiedCount, Is.EqualTo(1));
        Assert.That(_manager.GetDeviceInSlot(0)?.Name, Is.EqualTo("Xbox Wireless Controller (Renamed)"));
    }

    [Test]
    public void UnregisterController_FreesSlotAndRetainsOtherSlots()
    {
        var c1 = new ControllerInfo("id-1", "Controller 1", true);
        var c2 = new ControllerInfo("id-2", "Controller 2", true);
        var c3 = new ControllerInfo("id-3", "Controller 3", true);

        _manager.RegisterController(c1); // Slot 0
        _manager.RegisterController(c2); // Slot 1
        _manager.RegisterController(c3); // Slot 2

        bool removed = _manager.UnregisterController("id-2");

        Assert.That(removed, Is.True);
        Assert.That(_manager.GetDeviceInSlot(1), Is.Null);
        Assert.That(_manager.GetDeviceInSlot(0)?.Id, Is.EqualTo("id-1"));
        Assert.That(_manager.GetDeviceInSlot(2)?.Id, Is.EqualTo("id-3"));
        Assert.That(_manager.OccupiedCount, Is.EqualTo(2));
    }

    [Test]
    public void UnregisterController_WhenPrimaryDisconnects_SwitchesPrimaryToNextOccupied()
    {
        var c1 = new ControllerInfo("id-1", "Controller 1", true);
        var c2 = new ControllerInfo("id-2", "Controller 2", true);

        _manager.RegisterController(c1); // Slot 0 (Primary)
        _manager.RegisterController(c2); // Slot 1

        Assert.That(_manager.PrimarySlot, Is.EqualTo(0));

        _manager.UnregisterController("id-1");

        Assert.That(_manager.PrimarySlot, Is.EqualTo(1));
    }

    [Test]
    public void AssignSlot_SwapsControllersBetweenSlots()
    {
        var c1 = new ControllerInfo("id-1", "Controller 1", true);
        var c2 = new ControllerInfo("id-2", "Controller 2", true);

        _manager.RegisterController(c1); // Slot 0
        _manager.RegisterController(c2); // Slot 1

        bool swapped = _manager.AssignSlot("id-1", 1);

        Assert.That(swapped, Is.True);
        Assert.That(_manager.GetDeviceInSlot(1)?.Id, Is.EqualTo("id-1"));
        Assert.That(_manager.GetDeviceInSlot(0)?.Id, Is.EqualTo("id-2"));
    }

    [Test]
    public void RegisterController_WhenMaxSlotsExceeded_ReturnsNegativeOne()
    {
        for (int i = 0; i < 4; i++)
        {
            _manager.RegisterController(new ControllerInfo($"id-{i}", $"Pad {i}", true));
        }

        int overflow = _manager.RegisterController(new ControllerInfo("id-overflow", "Extra Pad", true));

        Assert.That(overflow, Is.EqualTo(-1));
        Assert.That(_manager.OccupiedCount, Is.EqualTo(4));
    }
}
