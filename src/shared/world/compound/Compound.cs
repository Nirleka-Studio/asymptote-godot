#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Asymptote.Shared.World.Entity;
using Asymptote.Shared.World.Entity.Player;
using Asymptote.Shared.World.Level.Scene;
using Godot;

namespace Asymptote.Shared.World.Compound;

public record NpcSpawnDefinition(string id, string scenePath, Vector3 position, Vector3 rotation);

public enum CompoundState
{
    DORMANT, // nothing spawned
    ACTIVE // NPCs spawned (or being spawned)
}

public class Compound
{
    private const int MAX_SPAWNS_PER_TICK = 2;

    public string id { get; }
    public CompoundState state { get; private set; } = CompoundState.DORMANT;

    private readonly Vector3 center;
    private readonly float activateRadiusSq;
    private readonly float deactivateRadiusSq;
    private readonly double despawnDelay;
    private readonly List<NpcSpawnDefinition> spawnDefinitions;

    private readonly Queue<NpcSpawnDefinition> pendingSpawns = new();
    private readonly Dictionary<string, Npc> liveNpcs = new();
    private readonly HashSet<string> deadThisVisit = new();

    private double timeOutsideRadius;
    private bool despawning;

    public Compound(
        string id, Vector3 center, float activateRadius, float deactivateRadius,
        float despawnDelay, List<NpcSpawnDefinition> spawnDefinitions)
    {
        this.id = id;
        this.center = center;
        this.activateRadiusSq = activateRadius * activateRadius;
        this.deactivateRadiusSq = deactivateRadius * deactivateRadius;
        this.despawnDelay = despawnDelay;
        this.spawnDefinitions = spawnDefinitions;
    }

    public IReadOnlyDictionary<string, Npc> getLiveNpcs() => this.liveNpcs;
    public bool isDead(string definitionId) => this.deadThisVisit.Contains(definitionId);

    #region Proximity (called a couple of times per second by CompoundManager)

    public void updateProximity(IReadOnlyList<PlayerEntity> players, double elapsed)
    {
        float nearestSq = float.MaxValue;
        foreach (var player in players)
        {
            // Flat (XZ) distance: height shouldn't matter for a compound
            Vector3 p = player.getPosition();
            float dx = p.X - this.center.X;
            float dz = p.Z - this.center.Z;
            nearestSq = Mathf.Min(nearestSq, dx * dx + dz * dz);
        }

        if (this.state == CompoundState.DORMANT)
        {
            if (nearestSq <= this.activateRadiusSq)
            {
                this.activate();
            }

            return;
        }

        if (nearestSq > this.deactivateRadiusSq)
        {
            this.timeOutsideRadius += elapsed;
            if (this.timeOutsideRadius >= this.despawnDelay && this.canDeactivate())
            {
                this.deactivate();
            }
        }
        else
        {
            this.timeOutsideRadius = 0;
        }
    }

    private bool canDeactivate()
    {
        // TODO: return false while alert level > CALM?
        return true;
    }

    #endregion

    #region Spawning (called every server tick)

    public void tick(Scene scene)
    {
        for (int i = 0; i < MAX_SPAWNS_PER_TICK && this.pendingSpawns.Count > 0; i++)
        {
            this.spawn(scene, this.pendingSpawns.Dequeue());
        }
    }

    private void activate()
    {
        this.state = CompoundState.ACTIVE;
        this.timeOutsideRadius = 0;
        this.deadThisVisit.Clear();

        foreach (var def in this.spawnDefinitions)
        {
            this.pendingSpawns.Enqueue(def);
        }

        GD.Print($"Compound '{this.id}' activated ({this.spawnDefinitions.Count} NPCs queued)");
    }

    private void deactivate()
    {
        this.state = CompoundState.DORMANT;
        this.pendingSpawns.Clear();

        // Our own removal handlers must not count these as deaths
        this.despawning = true;
        foreach (var npc in this.liveNpcs.Values.ToList())
        {
            ((IEntity)npc).remove(IEntity.RemovalReason.DESPAWNED);
        }

        this.liveNpcs.Clear();
        this.despawning = false;

        GD.Print($"Compound '{this.id}' deactivated");
    }

    private void spawn(Scene scene, NpcSpawnDefinition def)
    {
        // Stable id: survives respawn, so missions can reference it
        string uuid = $"{this.id}/{def.id}";

        if (scene.spawnEntity(def.scenePath, def.position, def.rotation, uuid) is not Npc npc)
        {
            GD.PushError($"Compound '{this.id}': '{def.id}' did not spawn an Npc");
            return;
        }

        this.liveNpcs[def.id] = npc;

        void onRemoved(IEntity _)
        {
            npc.onRemovedFromWorld -= onRemoved;
            if (this.despawning)
            {
                return;
            }

            // Real removal (killed, fell off the map...), not a compound reset
            this.liveNpcs.Remove(def.id);
            this.deadThisVisit.Add(def.id);
            // TODO: publish EntityKilled on the event bus
        }

        npc.onRemovedFromWorld += onRemoved;
    }

    #endregion
}