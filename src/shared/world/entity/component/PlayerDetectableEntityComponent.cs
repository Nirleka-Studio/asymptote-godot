using Asymptote.Shared.World.Entity.AI.Detection;
using Asymptote.Shared.World.Entity.player;

namespace Asymptote.Shared.World.Entity.Component;

public class PlayerDetectableEntityComponent : DetectableEntityComponent
{
    public PlayerDetectableEntityComponent(PlayerEntity entity) : base(entity)
    {
    }

    public override float getBasePriority(DetectionContext context)
    {
        return 1;
    }

    public override float getPriority(DetectionContext context)
    {
        return 1;
    }

    public override float getBaseDetectionRadius()
    {
        return 20;
    }

    public override float getDetectionMultiplier(DetectionContext context)
    {
        return 0.1f;
    }

    public override float getMovementMultiplier(DetectionContext context)
    {
        return 1;
    }

    public override bool shouldRaiseDetection(Npc agent, DetectionContext context)
    {
        return true;
    }
}