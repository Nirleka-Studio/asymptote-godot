using System;
using System.Threading;
using Asymptote.Shared.World.Entity.Component;
using Asymptote.Shared.World.Level.Scene;
using Godot;

namespace Asymptote.Shared.World.Entity;

public interface IEntity
{
    string uuid { get; set; }

    int instId { get; internal set; }

    // Native function pointers replacing the EntityInLevelCallback blueprint
    public event Action<IEntity> onPositionChanged;
    public event Action<IEntity> onRemovedFromWorld;

    string getUuid()
    {
        return this.uuid;
    }

    void update(double deltaTime, double currentTime);

    int getId()
    {
        return this.instId;
    }

    internal void setUuid(string uuid)
    {
        this.uuid = uuid;
    }

    Godot.Vector3 getPosition();

    Godot.Rid getRid();

    void setScene(Scene scene);

    bool isInScene();

    #region Components

    bool hasComponent<T>() where T : IEntityComponent
    {
        return false;
    }

    T getComponent<T>() where T : IEntityComponent
    {
        throw new NotImplementedException();
    }

    #endregion
}