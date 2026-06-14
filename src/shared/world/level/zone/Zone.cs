using Asymptote.Shared.World.Entity.AI.Detection;
using Godot;

namespace Asymptote.Shared.World.Level.Zone;

public partial class Zone : Area3D
{
    public override void _Ready()
    {
        // Zones should not waste the poor clients' RAM.
        // So we immediately delete them.
        if (!Multiplayer.IsServer())
        {
            QueueFree();
            return;
        }

        base.BodyEntered += this.onBodyEntered;
        base.BodyExited += this.onBodyExited;

        base._Ready();
    }

    private void onBodyEntered(Node3D body)
    {
    }

    private void onBodyExited(Node3D body)
    {
    }

    public override void _ExitTree()
    {
        if (Multiplayer.IsServer())
        {
            base.BodyEntered -= this.onBodyEntered;
            base.BodyExited -= this.onBodyExited;
        }

        base._ExitTree();
    }
}