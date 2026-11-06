using Asymptote.Shared.Physics.Collision;
using Godot;

namespace Asymptote.Shared.World.Interaction;

public partial class Prompt : Marker3D
{
    public const string GROUP = "prompts";

    [Export] public StringName action { get; set; } = "interact";
    [Export] public string interactionType { get; set; } = "interact";
    [Export] public string labelKey { get; set; } = "PROMPT_INTERACT";

    [Export] public float activationDistance { get; set; } = 3f;

    [Export] public bool enabled { get; set; } = true;

    public override void _EnterTree()
    {
        // Thank God Godot has Roblox's equivalent of CollectionService
        AddToGroup(GROUP);
    }

    public bool isReachableFrom(Vector3 origin, Rid exclude, float slack = 0f)
    {
        float max = this.activationDistance + slack;
        if (origin.DistanceSquaredTo(this.GlobalPosition) > max * max)
        {
            return false;
        }

        var query = PhysicsRayQueryParameters3D.Create(
            origin, this.GlobalPosition, CollisionMasks.LOS,
            new Godot.Collections.Array<Rid> { exclude });

        return GetWorld3D().DirectSpaceState.IntersectRay(query).Count == 0;
    }
}