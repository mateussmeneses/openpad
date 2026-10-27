using NUnit.Framework;
using OpenPad.Core.Engine;
using OpenPad.Core.Models;

namespace OpenPad.Tests;

[TestFixture]
public class RadialMenuEngineTests
{
    private RadialMenuEngine _menu = null!;

    [SetUp]
    public void SetUp()
    {
        var items = new List<RadialMenuItem>
        {
            new(0, "North", InputAction.FromKey(VirtualKey.D1)),
            new(1, "East", InputAction.FromKey(VirtualKey.D2)),
            new(2, "South", InputAction.FromKey(VirtualKey.D3)),
            new(3, "West", InputAction.FromKey(VirtualKey.D4))
        };
        _menu = new RadialMenuEngine(items)
        {
            DeadzoneThreshold = 0.3f
        };
    }

    [Test]
    public void UpdateStick_InsideDeadzone_ReturnsNull()
    {
        _menu.Open();
        var sector = _menu.UpdateStick(0.1f, 0.1f);

        Assert.That(sector, Is.Null);
        Assert.That(_menu.SelectedSector, Is.Null);
    }

    [Test]
    public void UpdateStick_CardinalsIn4SectorMenu_IdentifiesCorrectSectors()
    {
        _menu.Open();

        // Up: (x: 0, y: 1) -> North (Sector 0)
        Assert.That(_menu.UpdateStick(0f, 1f), Is.EqualTo(0));

        // Right: (x: 1, y: 0) -> East (Sector 1)
        Assert.That(_menu.UpdateStick(1f, 0f), Is.EqualTo(1));

        // Down: (x: 0, y: -1) -> South (Sector 2)
        Assert.That(_menu.UpdateStick(0f, -1f), Is.EqualTo(2));

        // Left: (x: -1, y: 0) -> West (Sector 3)
        Assert.That(_menu.UpdateStick(-1f, 0f), Is.EqualTo(3));
    }

    [Test]
    public void Close_WithExecute_DispatchesExecutedItem()
    {
        _menu.Open();
        _menu.UpdateStick(1f, 0f); // Select Sector 1 ("East")

        RadialMenuItem? executedItem = null;
        _menu.ItemExecuted += (_, item) => executedItem = item;

        _menu.Close(executeCurrentSelection: true);

        Assert.That(executedItem, Is.Not.Null);
        Assert.That(executedItem!.Label, Is.EqualTo("East"));
        Assert.That(_menu.IsOpen, Is.False);
    }

    [Test]
    public void Close_WithoutExecute_CancelsWithoutFiring()
    {
        _menu.Open();
        _menu.UpdateStick(1f, 0f); // Select Sector 1

        bool fired = false;
        _menu.ItemExecuted += (_, _) => fired = true;

        _menu.Close(executeCurrentSelection: false);

        Assert.That(fired, Is.False);
        Assert.That(_menu.IsOpen, Is.False);
    }
}
