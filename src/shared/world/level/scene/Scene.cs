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

    public WorldItem spawnItem(ItemStack stack, Vector3 position)
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

        itemSpawnRoot.AddChild(item);
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

        if (false)
        {
            if (new Random().NextDouble() < 0.1)
            {
                var definition = GD.Load<InventoryDefinition>("res://scenes/test_can.tres");
                this.spawnItem(new ItemStack(definition), new Vector3(0, 10, 0));
            }
        }
    }
}