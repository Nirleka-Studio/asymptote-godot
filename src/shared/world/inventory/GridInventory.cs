using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Asymptote.Shared.World.Inventory;

public class GridInventory
{
    public readonly int width;
    public readonly int height;

    private readonly List<PlacedItem> items = new();

    public IReadOnlyList<PlacedItem> placedItems => items;

    public GridInventory(int width, int height)
    {
        this.width = width;
        this.height = height;
    }

    public int getTotalWeightGrams()
    {
        return items.Sum(i => i.stack.getTotalWeightGrams());
    }

    public bool isInBounds(Vector2I position, Vector2I size)
    {
        return position.X >= 0 && position.Y >= 0
                               && position.X + size.X <= width
                               && position.Y + size.Y <= height;
    }

    // `ignore` lets a moving item check space without colliding with itself
    public bool isFree(Vector2I position, Vector2I size, PlacedItem ignore = null)
    {
        if (!isInBounds(position, size)) return false;

        var candidateRect = new Rect2I(position, size);
        foreach (var placed in items)
        {
            if (placed == ignore) continue;

            var existingRect = new Rect2I(placed.position, placed.getSize());
            if (candidateRect.Intersects(existingRect)) return false;
        }

        return true;
    }

    public bool tryPlace(ItemStack stack, Vector2I position)
    {
        var size = new Vector2I(stack.definition.gridWidth, stack.definition.gridHeight);
        if (!isFree(position, size)) return false;

        items.Add(new PlacedItem(stack, position));
        return true;
    }

    // Scans top-left to bottom-right for the first cell the stack fits in.
    // Good enough for now — swap for a smarter packing strategy later without
    // touching any callers, since they only care about true/false.
    public bool tryAutoPlace(ItemStack stack)
    {
        var size = new Vector2I(stack.definition.gridWidth, stack.definition.gridHeight);

        for (int y = 0; y <= height - size.Y; y++)
        {
            for (int x = 0; x <= width - size.X; x++)
            {
                var candidate = new Vector2I(x, y);
                if (isFree(candidate, size))
                {
                    items.Add(new PlacedItem(stack, candidate));
                    return true;
                }
            }
        }

        return false;
    }

    public bool tryMove(PlacedItem item, Vector2I newPosition)
    {
        if (!items.Contains(item)) return false;

        if (!isFree(newPosition, item.getSize(), item)) return false;

        item.position = newPosition;
        return true;
    }

    public bool remove(PlacedItem item)
    {
        return items.Remove(item);
    }

    public PlacedItem findByStackId(Guid networkId)
    {
        return items.FirstOrDefault(i => i.stack.networkId == networkId);
    }
}