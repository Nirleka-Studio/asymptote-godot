using System;
using System.Collections.Generic;
using Asymptote.Shared.World.Entity.Component;
using Asymptote.Shared.World.Entity.Component.Inventory;
using Asymptote.Shared.World.Level.Scene;
using Godot;

namespace Asymptote.Shared.World.Entity.Player;

public partial class PlayerEntity : CharacterBody3D, IEntity
{
    private Scene scene { get; set; }

    // public Vector3 position { get; set; }
    // let me not delete this to tell you a fucking story.
    // for fucking days ive been debugging and trying to fucking figure out
    // ripping my fucking hairs off
    // on why the players position are not synchronizing.
    // turns fucking out c# scripts can also override Godot Node properties
    // wtf?
    // I left out position without thinking about it
    // and no fucking editor ever tells me its shadowing the Godot's position property
    // what is this shit
    // fucking bullshit
    // now I can fuck off and move on with the inventory.

    public string uuid { get; set; }
    public int instId { get; set; }
    public event Action<IEntity> onPositionChanged;
    public event Action<IEntity> onRemovedFromWorld;

    private CharacterBody3D character;
    private Vector3 lastPosition;

    private Dictionary<Type, IEntityComponent> components;

    private IEntity.RemovalReason? removalReason;

    public PlayerEntity()
    {
        this.character = this;
        this.components = new()
        {
            // HACK: ffs we are setting the key as the class PlayerDetectableEntityComponent is extending to.
            // Oh well.
            [typeof(DetectableEntityComponent)] = new PlayerDetectableEntityComponent(this),
            [typeof(AccessComponent)] = new AccessComponent(this),
            [typeof(InventoryComponent)] = new InventoryComponent(this)
        };
        character.TreeExited += _ExitTree;
    }

    public string getUuid()
    {
        return uuid;
    }

    public Vector3 getPosition()
    {
        return character.GlobalPosition;
    }

    public void setScene(Scene givenScene)
    {
        this.scene = givenScene;
    }

    public bool isInScene()
    {
        return scene != null;
    }

    public Scene getScene()
    {
        return scene;
    }

    public Rid getRid()
    {
        return character.GetRid();
    }

    #region Components

    public bool hasComponent<T>() where T : IEntityComponent
    {
        return components.ContainsKey(typeof(T));
    }

    public T getComponent<T>() where T : IEntityComponent
    {
        var component = components[typeof(T)];
        if (component == null)
        {
            throw new Exception($"Component {typeof(T).Name} does not exist");
        }
        else
        {
            return (T)component;
        }
    }

    #endregion

    #region Lifecycle

    public void update(double delta, double currentTime)
    {
        Vector3 currentPos = character.GlobalPosition;
        if (currentPos != lastPosition)
        {
            lastPosition = currentPos;
            onPositionChanged?.Invoke(this);
        }

        if (this.getPosition().Y < this.scene.minY)
        {
            this.SetPosition(new Vector3(0, 10, 0)); // Maybe don't banish them to the shadow realm yet
        }
    }

    public override void _EnterTree()
    {
        base._EnterTree();

        if (int.TryParse(Name, out int id))
        {
            SetMultiplayerAuthority(id, true);
            GD.Print(GetMultiplayerAuthority());
        }
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

    #endregion
}