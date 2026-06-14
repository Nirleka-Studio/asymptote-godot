using Asymptote.Shared.World.Entity.AI.Detection;

namespace Asymptote.Shared.World.Entity.Component;

public abstract class DetectableEntityComponent : EntityComponent<IEntity>, IDetectableEntity
{
    public IEntity entity { get; }

    public DetectableEntityComponent(IEntity entity)
    {
        this.entity = entity;
    }

    public string getUuid()
    {
        return this.entity.getUuid();
    }

    public Godot.Vector3 getPosition()
    {
        return this.entity.getPosition();
    }

    public abstract float getBasePriority(DetectionContext context);

    public abstract float getPriority(DetectionContext context);

    public abstract float getBaseDetectionRadius();

    public abstract float getDetectionMultiplier(DetectionContext context);

    public abstract float getMovementMultiplier(DetectionContext context);

    public abstract bool shouldRaiseDetection(Npc agent, DetectionContext context);
}