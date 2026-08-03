using Asymptote.Shared.World.Entity;

namespace Asymptote.Shared.World.Inventory.Behavior;

// Implement this on any ItemBehavior that needs to react to being equipped
// e.g. a weapon behavior that wants to attach a world model to a bone socket,
// or vest that applies damage modifier when worn
public interface IEquippableItemBehavior
{
    void onEquip(IEntity user, ItemStack stack, EquipmentSlot slot);
    void onUnequip(IEntity user, ItemStack stack, EquipmentSlot slot);
}