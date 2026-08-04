#nullable enable

using System;
using Asymptote.Shared.World.Entity;
using Asymptote.Shared.World.Level.Entity;
using Asymptote.Shared.World.Inventory;
using Godot;

namespace Asymptote.Shared.World.Level.Scene;

public class Scene
{
    public EntityTickList entityTickList { get; private set; }
    public EntitySectionManager entityManager { get; private set; }

    // Set by ItemSpawner._Ready() on the server. Any Node3D should work, it just
    // needs to sit under the level's MultiplayerSpawner watched path so
    // dropped items replicate to clients.
    public Node3D itemSpawnRoot { get; set; }
    public Node3D entitiesNode { get; set; }

    public Scene()
    {
        this.entityTickList = new EntityTickList();

        LevelCallbackProxy callbackProxy = new(
            onTickingStart: (entity) => this.entityTickList.add(entity),
            onTickingStop: (entity) => this.entityTickList.remove(entity)
        );

        this.entityManager = new EntitySectionManager(callbackProxy);
    }

    public IEntity getEntityByUuid(string uuid)
    {
        return this.entityManager.getEntityByUuid(uuid);
    }

    public IEntity? spawnEntity(string scenePath, Vector3 position, Vector3? rotation = null)
    {
        PackedScene entityScene = GD.Load<PackedScene>(scenePath);
        if (entityScene == null)
        {
            GD.PushError($"Failure to spawn entity from path '{scenePath}'; possible invalid path");
            return null;
        }

        IEntity character = (IEntity)entityScene.Instantiate();
        character.initialize(this);
        character.setPosition(position);
        if (rotation != null)
        {
            character.setRotation(rotation.Value);
        }

        character.setScene(this);
        this.entityManager.addEntity(character);

        return character;
    }

    public WorldItem? spawnItem(ItemStack stack, Vector3 position)
    {
        if (itemSpawnRoot == null)
        {
            GD.PushError("Scene.spawnItem: itemSpawnRoot is null — is ItemSpawner in the level scene?");
            return null;
        }

        if (!itemSpawnRoot.Multiplayer.IsServer())
        {
            GD.PushWarning("Scene.spawnItem called on a non-authoritative peer");
            return null;
        }

        PackedScene worldItemScene = GD.Load<PackedScene>("res://scenes/world_item.tscn");
        WorldItem item = worldItemScene.Instantiate<WorldItem>();

        itemSpawnRoot.AddChild(item, true);
        item.GlobalPosition = position;
        item.setStack(stack);
        item.setScene(this);

        entityManager.addEntity(item);

        return item;
    }

    internal void update(double deltaTime, double currentTime)
    {
        this.entityTickList.forEach((entity => entity.update(deltaTime, currentTime)));

        // FOR NOW

        if (true)
        {
            if (new Random().NextDouble() < 0.1)
            {
                const float spawnRandom = (float)5.0;
                Vector2 pos = Vector2.FromAngle(GD.Randf() * 2 * Single.Pi);
                Vector3 finalPos = new Vector3(pos.X * spawnRandom * GD.Randf(), 3, pos.Y * spawnRandom);

                var definition = GD.Load<InventoryDefinition>("res://scenes/test_can.tres");
                this.spawnItem(new ItemStack(definition), new Vector3(0, 10, 0));
                this.spawnEntity("res://scenes/npc.tscn", finalPos);
            }
        }
    }
}