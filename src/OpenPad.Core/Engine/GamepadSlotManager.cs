using OpenPad.Core.Models;

namespace OpenPad.Core.Engine;

/// <summary>
/// Manages player gamepad slots (P1 to P4) and dynamic hot-plug assignment.
/// Guarantees that hot-plug events do not disrupt unassociated player slots.
/// </summary>
public class GamepadSlotManager
{
    private readonly object _syncRoot = new();
    private readonly ControllerInfo?[] _slots;
    public const int MaxSlots = 4;

    public event EventHandler<SlotAssignmentEventArgs>? SlotAssigned;
    public event EventHandler<SlotFreedEventArgs>? SlotFreed;
    public event EventHandler<int>? PrimarySlotChanged;

    public int PrimarySlot { get; private set; } = 0;

    public GamepadSlotManager(int maxSlots = MaxSlots)
    {
        if (maxSlots <= 0) throw new ArgumentOutOfRangeException(nameof(maxSlots));
        _slots = new ControllerInfo?[maxSlots];
    }

    /// <summary>
    /// Registers or updates a connected controller.
    /// Automatically assigns to the first available slot if not already assigned.
    /// </summary>
    public int RegisterController(ControllerInfo controller)
    {
        ArgumentNullException.ThrowIfNull(controller);

        lock (_syncRoot)
        {
            // If already assigned, keep existing slot
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i]?.Id == controller.Id)
                {
                    _slots[i] = controller;
                    return i;
                }
            }

            // Find first available slot
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] == null)
                {
                    _slots[i] = controller;
                    SlotAssigned?.Invoke(this, new SlotAssignmentEventArgs(i, controller));

                    // If primary slot was empty, point to this one
                    if (_slots[PrimarySlot] == null || _slots[PrimarySlot]?.Id == controller.Id)
                    {
                        PrimarySlot = i;
                        PrimarySlotChanged?.Invoke(this, PrimarySlot);
                    }

                    return i;
                }
            }

            return -1; // All slots full
        }
    }

    /// <summary>
    /// Unregisters a disconnected controller by device ID.
    /// </summary>
    public bool UnregisterController(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId)) return false;

        lock (_syncRoot)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i]?.Id == deviceId)
                {
                    var previous = _slots[i]!;
                    _slots[i] = null;
                    SlotFreed?.Invoke(this, new SlotFreedEventArgs(i, previous));

                    // If disconnected controller was in primary slot, fallback to next connected slot
                    if (PrimarySlot == i)
                    {
                        int fallback = FindNextOccupiedSlot();
                        PrimarySlot = fallback >= 0 ? fallback : 0;
                        PrimarySlotChanged?.Invoke(this, PrimarySlot);
                    }

                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Explicitly binds a device to a specific target slot, swapping or displacing if necessary.
    /// </summary>
    public bool AssignSlot(string deviceId, int targetSlot)
    {
        if (targetSlot < 0 || targetSlot >= _slots.Length) return false;

        lock (_syncRoot)
        {
            int currentSlot = -1;
            ControllerInfo? targetController = null;

            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i]?.Id == deviceId)
                {
                    currentSlot = i;
                    targetController = _slots[i];
                    break;
                }
            }

            if (targetController == null) return false;
            if (currentSlot == targetSlot) return true;

            // Swap contents
            var existingInTarget = _slots[targetSlot];
            _slots[targetSlot] = targetController;
            _slots[currentSlot] = existingInTarget;

            SlotAssigned?.Invoke(this, new SlotAssignmentEventArgs(targetSlot, targetController));
            if (existingInTarget != null)
            {
                SlotAssigned?.Invoke(this, new SlotAssignmentEventArgs(currentSlot, existingInTarget));
            }

            return true;
        }
    }

    public ControllerInfo? GetDeviceInSlot(int slot)
    {
        lock (_syncRoot)
        {
            if (slot < 0 || slot >= _slots.Length) return null;
            return _slots[slot];
        }
    }

    public int GetSlotForDevice(string deviceId)
    {
        lock (_syncRoot)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i]?.Id == deviceId) return i;
            }
            return -1;
        }
    }

    public IReadOnlyList<ControllerInfo?> GetAllSlots()
    {
        lock (_syncRoot)
        {
            return _slots.ToArray();
        }
    }

    public int OccupiedCount
    {
        get
        {
            lock (_syncRoot)
            {
                int count = 0;
                for (int i = 0; i < _slots.Length; i++)
                {
                    if (_slots[i] != null) count++;
                }
                return count;
            }
        }
    }

    private int FindNextOccupiedSlot()
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] != null) return i;
        }
        return -1;
    }
}

public class SlotAssignmentEventArgs : EventArgs
{
    public int Slot { get; }
    public ControllerInfo Controller { get; }

    public SlotAssignmentEventArgs(int slot, ControllerInfo controller)
    {
        Slot = slot;
        Controller = controller;
    }
}

public class SlotFreedEventArgs : EventArgs
{
    public int Slot { get; }
    public ControllerInfo Controller { get; }

    public SlotFreedEventArgs(int slot, ControllerInfo controller)
    {
        Slot = slot;
        Controller = controller;
    }
}
