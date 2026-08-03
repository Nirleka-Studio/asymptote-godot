using Godot;

namespace Asymptote.Shared.World.Inventory.Behavior;

[GlobalClass]
public abstract partial class ItemBehavior : Resource
{
    // most behaviors are stateless so override only if we need per-instance data
    public virtual ItemBehaviorState createState() => null;
}

public abstract class ItemBehaviorState
{
}