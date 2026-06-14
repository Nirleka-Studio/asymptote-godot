using Asymptote.Shared.World.Entity.AI.Detection;
using Asymptote.Shared.World.Entity.Component;
using Asymptote.Shared.World.Entity.Player;
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
        GD.Print($"Body entered: {body.Name}");
        if (body is PlayerEntity playerEntity)
        {
            var component = playerEntity.getComponent<AccessComponent>();
            if (component != null)
            {
                component.onEnterZone(this);
            }
        }
    }

    private void onBodyExited(Node3D body)
    {
        GD.Print($"Body left: {body.Name}");
        if (body is PlayerEntity playerEntity)
        {
            var component = playerEntity.getComponent<AccessComponent>();
            if (component != null)
            {
                component.onLeaveZone(this);
            }
        }
    }

    public bool isAuthorized(PlayerEntity player)
    {
        return false; // TODO: FOR NOW
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