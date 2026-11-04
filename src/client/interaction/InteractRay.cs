using Asymptote.Shared.Physics.Collision;
using Asymptote.Shared.World.Entity.Player;
using Asymptote.Shared.World.Interaction;
using Godot;

namespace Asymptote.Client.Interaction;

public partial class InteractRay : Node3D
{
    [Export] public PlayerEntity parent { get; set; }
    public Prompt target { get; private set; }

    public override void _PhysicsProcess(double delta)
    {
        var space = GetWorld3D().DirectSpaceState;
        var exclude = new Godot.Collections.Array<Rid> { this.parent.GetRid() };

        Prompt best = null;
        float bestDistSq = float.MaxValue;

        foreach (Prompt prompt in PromptRegistry.Instance.prompts)
        {
            float distSq = this.GlobalPosition.DistanceSquaredTo(prompt.GlobalPosition);
            GD.Print(distSq);

            if (distSq > prompt.activationDistance * prompt.activationDistance || distSq >= bestDistSq)
            {
                GD.Print("dist fail");
                continue;
            }

            var query = PhysicsRayQueryParameters3D.Create(
                this.GlobalPosition, prompt.GlobalPosition, CollisionMasks.LOS, exclude);

            var hit = space.IntersectRay(query);
            if (hit.Count > 0)
            {
                var col = hit["collider"].As<CollisionObject3D>();
                GD.Print($"blocked by {col?.GetPath()} layer={col?.CollisionLayer} at {(Vector3)hit["position"]}");
                continue;
            }

            best = prompt;
            bestDistSq = distSq;
        }

        this.target = best;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        GD.Print(this.target);
        if (this.target == null) return;

        if (@event.IsActionPressed(this.target.action))
        {
            // hand off, don't do networking here
            this.parent.requestInteract("idfk", "NOT SET");
            GetViewport().SetInputAsHandled();
        }
    }
}