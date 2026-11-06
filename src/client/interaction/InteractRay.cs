using Asymptote.Shared.World.Entity.Player;
using Asymptote.Shared.World.Interaction;
using Godot;

namespace Asymptote.Client.Interaction;

public partial class InteractRay : Node3D
{
    [Export] public CollisionObject3D parent { get; set; }

    public Prompt target { get; private set; }

    public override void _Ready()
    {
        bool local = this.parent.IsMultiplayerAuthority();
        SetPhysicsProcess(local);
        SetProcessUnhandledInput(local);
    }

    public override void _PhysicsProcess(double delta)
    {
        Rid exclude = this.parent.GetRid();
        Prompt best = null;
        float bestSq = float.MaxValue;

        foreach (Node node in GetTree().GetNodesInGroup(Prompt.GROUP))
        {
            if (node is not Prompt prompt || !prompt.enabled)
            {
                continue;
            }

            float distSq = this.GlobalPosition.DistanceSquaredTo(prompt.GlobalPosition);
            if (distSq >= bestSq)
            {
                continue;
            }

            if (!prompt.isReachableFrom(this.GlobalPosition, exclude))
            {
                continue;
            }

            best = prompt;
            bestSq = distSq;
        }

        this.target = best;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!IsInstanceValid(this.target) || !@event.IsActionPressed(this.target.action))
        {
            return;
        }

        (this.parent as PlayerEntity)?.requestInteract(this.target.GetPath().ToString());
        GetViewport().SetInputAsHandled();
    }
}