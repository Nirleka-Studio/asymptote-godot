using Asymptote.Shared.World.Entity;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Asymptote.Shared.World.Level.Entity;

public class EntitySectionManager
{
    private static int idCounter = 0;
    public static readonly float SECTION_SIZE = 16.0f;
    private readonly Dictionary<Vector3I, HashSet<IEntity>> sections = new();
    private readonly Dictionary<IEntity, Vector3I> entityToSection = new();
    private readonly Dictionary<string, IEntity> uuidToEntity = new();

    private readonly LevelCallbackProxy levelCallback;

    public EntitySectionManager(LevelCallbackProxy levelCallback)
    {
        this.levelCallback = levelCallback;
    }

    private Vector3I getSectionKey(Vector3 position)
    {
        return new Vector3I(
            Mathf.FloorToInt(position.X / SECTION_SIZE),
            Mathf.FloorToInt(position.Y / SECTION_SIZE),
            Mathf.FloorToInt(position.Z / SECTION_SIZE)
        );
    }

    public void addEntity(IEntity entity)
    {
        if (string.IsNullOrEmpty(entity.getUuid()) || entity.getUuid() == "UNSET")
        {
            entity.setUuid(Guid.NewGuid().ToString());
        }

        // This feels fucking illegal
        // But it solves the fucking problem that will otherwise be a pain in the ass
        // if done the other way.
        entity.instId = Interlocked.Increment(ref idCounter);

        Vector3 pos = entity.getPosition();
        Vector3I key = getSectionKey(pos);

        if (!sections.ContainsKey(key))
        {
            sections[key] = new HashSet<IEntity>();
        }

        sections[key].Add(entity);
        entityToSection[entity] = key;
        uuidToEntity[entity.getUuid()] = entity;

        entity.onPositionChanged += updateEntityPosition;
        entity.onRemovedFromWorld += removeEntity;

        levelCallback.onTickingStart(entity);
    }

    public void removeEntity(IEntity entity)
    {
        if (entityToSection.TryGetValue(entity, out Vector3I key))
        {
            if (sections.TryGetValue(key, out var section))
            {
                section.Remove(entity);
                if (section.Count == 0)
                {
                    sections.Remove(key);
                }
            }

            entityToSection.Remove(entity);
            uuidToEntity.Remove(entity.getUuid());

            entity.onPositionChanged -= updateEntityPosition;
            entity.onRemovedFromWorld -= removeEntity;

            levelCallback.onTickingStop(entity);
        }
    }

    public void updateEntityPosition(IEntity entity)
    {
        if (!entityToSection.TryGetValue(entity, out Vector3I oldKey)) return;

        Vector3 pos = entity.getPosition();
        Vector3I newKey = getSectionKey(pos);

        if (oldKey != newKey)
        {
            // Erase from previous chunk
            if (sections.TryGetValue(oldKey, out var oldSection))
            {
                oldSection.Remove(entity);
                if (oldSection.Count == 0) sections.Remove(oldKey);
            }

            // Append to new chunk
            if (!sections.ContainsKey(newKey))
            {
                sections[newKey] = new HashSet<IEntity>();
            }

            sections[newKey].Add(entity);
            entityToSection[entity] = newKey;
        }
    }

    public IEntity getEntityByUuid(string uuid)
    {
        return uuidToEntity.GetValueOrDefault(uuid);
    }

    public List<IEntity> getEntitiesInRange(Vector3 origin, float minDist, float maxDist)
    {
        var foundEntities = new List<IEntity>();

        float minSq = minDist * minDist;
        float maxSq = maxDist * maxDist;

        // Calculate min/max boundaries in world space to capture chunk ranges
        Vector3I minCell = getSectionKey(origin - new Vector3(maxDist, maxDist, maxDist));
        Vector3I maxCell = getSectionKey(origin + new Vector3(maxDist, maxDist, maxDist));

        // Iterate through intersecting grid coordinate space exclusively
        for (int x = minCell.X; x <= maxCell.X; x++)
        {
            for (int y = minCell.Y; y <= maxCell.Y; y++)
            {
                for (int z = minCell.Z; z <= maxCell.Z; z++)
                {
                    var lookupKey = new Vector3I(x, y, z);

                    if (sections.TryGetValue(lookupKey, out var section))
                    {
                        foreach (var entity in section)
                        {
                            float distSq = origin.DistanceSquaredTo(entity.getPosition());

                            if (distSq >= minSq && distSq <= maxSq)
                            {
                                foundEntities.Add(entity);
                            }
                        }
                    }
                }
            }
        }

        return foundEntities;
    }

    internal Dictionary<Vector3I, List<IEntity>> getActiveSections()
    {
        return sections.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.ToList()
        );
    }
}