using System;
using System.Collections.Generic;
using Asymptote.Shared.World.Entity.Component;
using Asymptote.Shared.World.Level.Scene;
using Godot;

namespace Asymptote.Shared.World.Entity.Player;

public partial class PlayerEntity : CharacterBody3D, IEntity
{
    private Scene scene { get; set; }

    public Vector3 position { get; set; }
    public string uuid { get; set; }
    public int instId { get; set; }
    public event Action<IEntity> onPositionChanged;
    public event Action<IEntity> onRemovedFromWorld;

    private CharacterBody3D character;
    private Vector3 lastPosition;

    private Dictionary<Type, IEntityComponent> components;

    public PlayerEntity()
    {
        this.character = this;
        this.components = new()
        {
            // HACK: ffs we are setting the key as the class PlayerDetectableEntityComponent is extending to.
            // Oh well.
            [typeof(DetectableEntityComponent)] = new PlayerDetectableEntityComponent(this)
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

    public void setScene(Scene scene)
    {
        this.scene = scene;
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
    }

    private void _ExitTree()
    {
        onRemovedFromWorld?.Invoke(this);
    }

    #endregion
}