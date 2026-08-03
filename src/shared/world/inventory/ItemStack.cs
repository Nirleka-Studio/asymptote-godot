using System;
using System.Collections.Generic;
using Asymptote.Shared.World.Inventory.Behavior;

namespace Asymptote.Shared.World.Inventory;

public class ItemStack
{
    public Guid networkId { get; set; } = Guid.NewGuid();
    public InventoryDefinition definition;
    public int count = 1; // pinned to 1 for now

    private Dictionary<ItemBehavior, ItemBehaviorState> behaviorStates = new();

    public ItemStack(InventoryDefinition definition)
    {
        this.definition = definition;
        foreach (var behavior in definition.behaviors)
        {
            var state = behavior.createState();
            if (state != null) behaviorStates[behavior] = state;
        }
    }

    public T getState<T>(ItemBehavior behavior) where T : ItemBehaviorState
        => (T)behaviorStates[behavior];

    public int getTotalWeightGrams() => definition.weightInGrams * count;

    public bool canStackWith(ItemStack other)
    {
        return definition.stackable
               && definition == other.definition
               && behaviorStates.Count == 0; // items with meaningful per-unit state never merge
    }
}