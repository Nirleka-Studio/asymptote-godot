using System.Collections.Generic;
using Asymptote.Shared.World.Entity.Player;
using Asymptote.Shared.World.Inventory;
using Asymptote.Shared.World.Inventory.Behavior;

namespace Asymptote.Shared.World.Entity.Component.Inventory;

public class InventoryComponent : EntityComponent<PlayerEntity>
{
    public PlayerEntity entity { get; }

    public InventoryContainer main;
    public InventoryContainer backpack;

    private readonly Dictionary<EquipmentSlot, ItemStack> equipment = new();

    public InventoryComponent(PlayerEntity entity)
    {
        this.entity = entity;
        this.main = new InventoryContainer("MAIN", 8, 5);
        this.backpack = new InventoryContainer("BACKPACK", 4, 6);
    }

    public ItemStack getEquipped(EquipmentSlot slot)
    {
        return equipment.TryGetValue(slot, out var stack) ? stack : null;
    }

    public int getTotalWeightGrams()
    {
        int total = main.grid.getTotalWeightGrams() + backpack.grid.getTotalWeightGrams();
        foreach (var stack in equipment.Values)
        {
            total += stack.getTotalWeightGrams();
        }

        return total;
    }

    // Equips a stack that's already "free" — caller is responsible for having
    // removed it from wherever it was (a grid, the ground, etc) beforehand.
    // Use tryEquipFrom() below if it's currently sitting in one of your grids.
    public bool tryEquip(ItemStack stack, EquipmentSlot slot)
    {
        if (!stack.definition.compatibleSlots.Contains(slot))
        {
            return false;
        }

        if (equipment.ContainsKey(slot))
        {
            return false; // something's already there — caller decides what to do with it
        }

        equipment[slot] = stack;

        foreach (var behavior in stack.definition.behaviors)
        {
            if (behavior is IEquippableItemBehavior equippable)
            {
                equippable.onEquip(entity, stack, slot);
            }
        }

        return true;
    }

    // Atomic version for the common case: dragging an item straight out of a
    // grid cell into an equipment slot. Rolls back to its original cell if
    // equipping fails for any reason.
    public bool tryEquipFrom(InventoryContainer source, PlacedItem placed, EquipmentSlot slot)
    {
        if (!placed.stack.definition.compatibleSlots.Contains(slot) || equipment.ContainsKey(slot))
        {
            return false;
        }

        var originalPosition = placed.position;
        source.grid.remove(placed);

        if (!tryEquip(placed.stack, slot))
        {
            source.grid.tryPlace(placed.stack, originalPosition);
            return false;
        }

        return true;
    }

    // Removes whatever's in the slot and tries to auto-place it into a grid
    // (main first, then backpack). Returns null if the slot was empty or
    // there wasn't room anywhere — in that case the item stays equipped.
    public ItemStack tryUnequip(EquipmentSlot slot)
    {
        if (!equipment.TryGetValue(slot, out var stack))
        {
            return null;
        }

        if (!main.addItem(stack) && !backpack.addItem(stack))
        {
            return null; // no room — tell the player, don't silently drop it
        }

        equipment.Remove(slot);

        foreach (var behavior in stack.definition.behaviors)
        {
            if (behavior is IEquippableItemBehavior equippable)
            {
                equippable.onUnequip(entity, stack, slot);
            }
        }

        return stack;
    }
}