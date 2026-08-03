using System;

namespace Asymptote.Shared.World.Inventory;

// Wraps a GridInventory with an id and label so an entity can own several of
// these (MAIN, BACKPACK, POCKETS...) and so a future networking layer has
// something stable to address in RPCs instead of raw object references.
public class InventoryContainer
{
    public Guid id { get; } = Guid.NewGuid();
    public string label;
    public GridInventory grid;

    public InventoryContainer(string label, int width, int height)
    {
        this.label = label;
        this.grid = new GridInventory(width, height);
    }

    public bool addItem(ItemStack stack)
    {
        return grid.tryAutoPlace(stack);
    }
}