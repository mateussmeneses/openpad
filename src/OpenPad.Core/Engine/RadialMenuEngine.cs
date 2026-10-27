using OpenPad.Core.Models;

namespace OpenPad.Core.Engine;

public record RadialMenuItem(int Index, string Label, InputAction Action, string? IconName = null);

/// <summary>
/// Virtual radial overlay action wheel engine.
/// Calculates active sectors from 2D stick vectors with deadzone and angular sector discretization.
/// </summary>
public class RadialMenuEngine
{
    private readonly List<RadialMenuItem> _items = new();
    private readonly object _syncLock = new();

    public float DeadzoneThreshold { get; set; } = 0.35f;
    public bool IsOpen { get; private set; }
    public int? SelectedSector { get; private set; }

    public event EventHandler<int?>? ActiveSectorChanged;
    public event EventHandler<RadialMenuItem>? ItemExecuted;

    public RadialMenuEngine(IEnumerable<RadialMenuItem>? initialItems = null)
    {
        if (initialItems != null)
        {
            _items.AddRange(initialItems);
        }
    }

    public void SetItems(IEnumerable<RadialMenuItem> items)
    {
        lock (_syncLock)
        {
            _items.Clear();
            _items.AddRange(items);
            SelectedSector = null;
        }
    }

    public void Open()
    {
        lock (_syncLock)
        {
            IsOpen = true;
            SelectedSector = null;
        }
    }

    public void Close(bool executeCurrentSelection = true)
    {
        RadialMenuItem? itemToExecute = null;

        lock (_syncLock)
        {
            if (!IsOpen) return;
            IsOpen = false;

            if (executeCurrentSelection && SelectedSector.HasValue)
            {
                itemToExecute = _items.FirstOrDefault(i => i.Index == SelectedSector.Value);
            }

            SelectedSector = null;
            ActiveSectorChanged?.Invoke(this, null);
        }

        if (itemToExecute != null)
        {
            ItemExecuted?.Invoke(this, itemToExecute);
        }
    }

    /// <summary>
    /// Updates the radial wheel with normalized stick coordinates (-1.0 to 1.0).
    /// </summary>
    public int? UpdateStick(float stickX, float stickY)
    {
        lock (_syncLock)
        {
            if (!IsOpen || _items.Count == 0) return null;

            float radius = MathF.Sqrt(stickX * stickX + stickY * stickY);
            if (radius < DeadzoneThreshold)
            {
                if (SelectedSector != null)
                {
                    SelectedSector = null;
                    ActiveSectorChanged?.Invoke(this, null);
                }
                return null;
            }

            // Math.Atan2 returns angle in radians (-PI to PI) where 0 is right (East) and +Y is usually down in screen coords,
            // but in stick coords +Y is Up. Let's compute angle in degrees clockwise starting from 12 o'clock (Up).
            // Up: (0, 1) -> 0 deg; Right: (1, 0) -> 90 deg; Down: (0, -1) -> 180 deg; Left: (-1, 0) -> 270 deg.
            float angleRad = MathF.Atan2(stickX, stickY); // Atan2(x, y) gives 0 at (0, 1), pi/2 at (1, 0)
            float angleDeg = angleRad * (180f / MathF.PI);
            if (angleDeg < 0) angleDeg += 360f;

            int sectorCount = _items.Count;
            float sectorSpan = 360f / sectorCount;

            // Offset by half sector span so that 12 o'clock is centered on sector 0
            float offsetAngle = (angleDeg + (sectorSpan / 2f)) % 360f;
            int sectorIndex = (int)(offsetAngle / sectorSpan);

            if (sectorIndex >= sectorCount) sectorIndex = 0;

            if (SelectedSector != sectorIndex)
            {
                SelectedSector = sectorIndex;
                ActiveSectorChanged?.Invoke(this, SelectedSector);
            }

            return SelectedSector;
        }
    }

    public IReadOnlyList<RadialMenuItem> Items
    {
        get
        {
            lock (_syncLock) return _items.ToList();
        }
    }
}
