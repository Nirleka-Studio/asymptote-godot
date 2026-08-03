using Godot;

namespace Asymptote.Shared.World.Inventory;

// A stack sitting at a specific top-left cell inside a GridInventory.
// This is the "occupancy" record — the stack itself has no idea where it lives.
public class PlacedItem
{
    public ItemStack stack;
    public Vector2I position; // top-left cell, grid-local

    public PlacedItem(ItemStack stack, Vector2I position)
    {
        this.stack = stack;
        this.position = position;
    }

    public Vector2I getSize()
    {
        // TODO: once we added rotations, flip X/Y here based on a rotated flag
        // we press r to rotate it clockwise
        return new Vector2I(stack.definition.gridWidth, stack.definition.gridHeight);
    }
}