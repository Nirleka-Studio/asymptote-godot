using Asymptote.Shared.World.Inventory.Behavior;
using Godot;

namespace Asymptote.Shared.World.Inventory;

[GlobalClass]
public partial class InventoryDefinition : Resource
{
    [Export] public string id;
    [Export] public string displayName;
    [Export] public string subtitle;
    [Export] public Texture2D icon;
    [Export] public int gridWidth = 1;
    [Export] public int gridHeight = 1;
    [Export] public int weightInGrams = 0;

    [Export] public bool stackable = false; // everything false today
    [Export] public int maxStackSize = 1;

    [Export] public Godot.Collections.Array<EquipmentSlot> compatibleSlots = new();

    [Export] public Godot.Collections.Array<ItemBehavior> behaviors;
}