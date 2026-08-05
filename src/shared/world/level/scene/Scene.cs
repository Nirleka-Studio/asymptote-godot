#nullable enable

using System;
using Asymptote.Shared.World.Entity;
using Asymptote.Shared.World.Level.Entity;
using Asymptote.Shared.World.Inventory;
using Asymptote.Util;
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

    #region Level Attributes

    public float minY = -64; // TODO: Later on, this is changed dynamically based on total level bounds

    #endregion

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

    public void createExplosion(Vector3 epicenter, float radius = 10.0f, float force = 25.0f, float upwardBias = 0.35f)
    {
        float radiusSquared = radius * radius;

        foreach (IEntity entity in this.entityManager.getEntitiesInRange(epicenter, 0, radius))
        {
            Vector3 entityPos = entity.getPosition();
            float distSquared = epicenter.DistanceSquaredTo(entityPos);

            float distance = Mathf.Sqrt(distSquared);

            Vector3 pushDirection = (entityPos - epicenter) / distance;

            pushDirection.Y += upwardBias;
            pushDirection = pushDirection.Normalized();

            float attenuation = 1.0f - (distance / radius);
            attenuation *= attenuation;

            Vector3 impulse = pushDirection * force * attenuation;

            if (entity is RigidBody3D rigidBody)
            {
                rigidBody.ApplyImpulse(impulse);
            }

            else if (entity is CharacterBody3D characterBody)
            {
                characterBody.Velocity += impulse;
            }
            else if (entity is Node3D nodeEntity)
            {
                var rb = nodeEntity.GetNodeOrNull<RigidBody3D>(".");
                if (rb != null)
                {
                    rb.ApplyImpulse(impulse);
                }
            }
        }
    }

    internal void update(double deltaTime, double currentTime)
    {
        this.entityTickList.forEach((entity => entity.update(deltaTime, currentTime)));

        // FOR NOW

        if (true)
        {
            if (new Random().NextDouble() < 0.15)
            {
                const float spawnRandom = (float)30;
                Vector2 pos = Vector2.FromAngle(GD.Randf() * 2 * Single.Pi);
                Vector3 finalPos = new Vector3(pos.X * spawnRandom * GD.Randf(), 3, pos.Y * spawnRandom);
                var definition = GD.Load<InventoryDefinition>("res://scenes/test_can.tres");
                this.spawnItem(new ItemStack(definition), new Vector3(0, 10, 0));

                if (new Random().NextDouble() < 0.2)
                {
                    this.spawnEntity("res://scenes/npc.tscn", finalPos);
                    this.createExplosion(finalPos, 20.0f, 15.0f);
                    var finalExplosionPos = new Vector3(finalPos.X, finalPos.Y - 1.5f, finalPos.Z);
                    this.createExplosion(finalExplosionPos, 20.0f, 15.0f);
                    this.createExplosion(new Vector3(0, 2, 0), 20.0f, 15.0f);
                }
            }
        }
    }
}