using System.Collections.Generic;
using Asymptote.Shared.World.Entity.AI.Memory;
using Godot;

namespace Asymptote.Shared.World.Entity.AI.Sensing;

public class VisibleEntitiesSensor : Sensor<Npc>
{
    private static List<IMemoryModuleType> REQUIRED_MEMORIES = new()
    {
        MemoryModuleTypes.VISIBLE_ENTITIES
    };

    private float maxDistance = 50;
    private float sightRadius = 20;
    private float peripheralVisionAngle = 90;

    public override List<IMemoryModuleType> getRequiredMemories()
    {
        return REQUIRED_MEMORIES;
    }

    public override void doUpdate(Npc agent, double deltaTime)
    {
        var entitiesInRadius = agent.getScene().entityManager
            .getEntitiesInRange(agent.getPosition(), 0, this.maxDistance);
        var visibleEntities = new HashSet<string>();
        foreach (var entity in entitiesInRadius)
        {
            if (entity == agent) continue;

            if (this.isInVision(agent, entity))
            {
                GD.Print(entity.getUuid(), "Is in vision!");
                visibleEntities.Add(entity.getUuid());
            }
        }

        agent.getBrain().setMemory(MemoryModuleTypes.VISIBLE_ENTITIES, visibleEntities);
    }

    private bool isInVision(Npc agent, IEntity entity)
    {
        Vector3 agentPos = agent.getEyePosition();
        Vector3 entityPos = entity.getPosition();

        Vector3 toEntity = entityPos - agentPos;
        float dist = toEntity.Length();

        if (dist > this.sightRadius)
        {
            return false;
        }

        Vector3 facingDirection = agent.getEyeVector().Normalized();
        Vector3 targetDirection = toEntity.Normalized();

        float dotProduct = facingDirection.Dot(targetDirection);

        float angleToTarget = Mathf.RadToDeg(Mathf.Acos(dotProduct));

        if (angleToTarget > (this.peripheralVisionAngle / 2f))
        {
            return false;
        }

        var world3D = agent.GetWorld3D();
        var spaceState = world3D.DirectSpaceState;

        var query = PhysicsRayQueryParameters3D.Create(agentPos, entityPos);

        query.Exclude = new Godot.Collections.Array<Rid> { agent.getRid(), entity.getRid() };

        var result = spaceState.IntersectRay(query);

        if (result.Count > 0)
        {
            return false;
        }

        return true;
    }
}