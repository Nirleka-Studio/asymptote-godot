using System;
using System.Collections.Generic;
using Asymptote.Shared.World.Entity;

namespace Asymptote.Shared.World.Level.Entity;

public class EntityTickList
{
    private Dictionary<int, IEntity> active = new();
    private Dictionary<int, IEntity> passive = new();
    private Dictionary<int, IEntity> iterated = null;

    private void ensureActiveIsNotIterated()
    {
        if (this.iterated == this.active)
        {
            this.passive.Clear();

            foreach (var kvp in this.active)
            {
                this.passive.Add(kvp.Key, kvp.Value);
            }

            // Using tuple for swapping cuz it won't shut the fuck up
            (this.active, this.passive) = (this.passive, this.active);
        }
    }

    public void add(IEntity entity)
    {
        this.ensureActiveIsNotIterated();
        this.active[entity.getId()] = entity;
    }

    public void remove(IEntity entity)
    {
        this.ensureActiveIsNotIterated();
        this.active.Remove(entity.getId());
    }

    public bool contains(IEntity entity)
    {
        return this.active.ContainsKey(entity.getId());
    }

    public void forEach(Action<IEntity> action)
    {
        if (this.iterated != null)
        {
            throw new("Only one concurrent iteration supported");
        }

        this.iterated = this.active;

        try
        {
            // Even if an Npc changes 'this.active' mid-loop via add/remove,
            // 'this.iterated' stays fixed on the snapshot array, avoiding crashes.
            foreach (var entity in this.iterated.Values)
            {
                action(entity);
            }
        }
        finally
        {
            this.iterated = null;
        }
    }
}