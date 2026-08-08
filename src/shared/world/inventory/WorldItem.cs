using System;
using System.Linq;
using Asymptote.Shared.World.Entity;
using Asymptote.Shared.World.Entity.Component;
using Asymptote.Shared.World.Entity.Component.Inventory;
using Asymptote.Shared.World.Entity.Player;
using Asymptote.Shared.World.Level.Scene;
using Godot;

namespace Asymptote.Shared.World.Inventory;

/// <summary>
/// Class <c>WorldItem</c> represents a physical ItemStack sitting in the Scene
/// that can be picked up.
/// </summary>
public partial class WorldItem : RigidBody3D, IEntity
{
    public ItemStack stack { get; private set; }

    private Scene scene;
    private Vector3 lastReportedPosition;

    public string uuid { get; set; }
    public int instId { get; set; }
    public event Action<IEntity> onPositionChanged;
    public event Action<IEntity> onRemovedFromWorld;

    private IEntity.RemovalReason? removalReason;

    public string getUuid() => uuid;

    public Vector3 getPosition() => GlobalPosition;

    public Rid getRid() => GetRid();

    public void setScene(Scene scene)
    {
        this.scene = scene;
    }

    public bool isInScene()
    {
        return scene != null;
    }

    public void update(double deltaTime, double currentTime)
    {
        if (this.getPosition().Y < this.scene.minY)
        {
            ((IEntity)this).remove(IEntity.RemovalReason.BELOW_WORLD);
        }
    }

    public void setStack(ItemStack givenStack)
    {
        this.stack = givenStack;
        refreshVisual();
    }

    public override void _Ready()
    {
        refreshVisual();
    }

    private void refreshVisual()
    {
        if (stack == null) return;
        // TODO: ItemPresentation.worldModel here once that resource exists
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void RequestPickup()
    {
        if (!Multiplayer.IsServer()) return;

        int senderId = Multiplayer.GetRemoteSenderId();
        var requester = GetTree().GetNodesInGroup("players")
            .OfType<PlayerEntity>()
            .FirstOrDefault(p => p.GetMultiplayerAuthority() == senderId);

        if (requester == null) return;

        var inventory = requester.getComponent<InventoryComponent>();
        tryPickup(inventory.main);
    }

    public bool tryPickup(InventoryContainer destination)
    {
        if (!Multiplayer.IsServer())
        {
            GD.PushWarning("WorldItem.tryPickup called on a non-authoritative peer");
            return false;
        }

        if (!destination.addItem(stack))
        {
            return false;
        }

        QueueFree();
        return true;
    }

    #region Life

    public void setRemoved(IEntity.RemovalReason reason)
    {
        if (this.removalReason == null)
        {
            this.removalReason = reason;
        }

        onRemovedFromWorld?.Invoke(this);
        this.onRemoval(reason);
    }

    private void onRemoval(IEntity.RemovalReason reason)
    {
        if (reason != IEntity.RemovalReason.EXIT_TREE)
        {
            this.QueueFree();
        }
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        ((IEntity)this).remove(IEntity.RemovalReason.EXIT_TREE);
    }

    #endregion
}