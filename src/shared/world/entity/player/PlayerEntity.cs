using System;
using Asymptote.Shared.World.Level.Scene;
using Godot;

namespace Asymptote.Shared.World.Entity.player;

public class PlayerEntity : IEntity
{
    private Scene scene { get; set; }

    public Vector3 position { get; set; }
    public string uuid { get; set; }
    public int instId { get; set; }
    public event Action<IEntity> onPositionChanged;
    public event Action<IEntity> onRemovedFromWorld;

    private CharacterBody3D character;
    private Vector3 lastPosition;

    public PlayerEntity(CharacterBody3D character)
    {
        this.character = character;
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