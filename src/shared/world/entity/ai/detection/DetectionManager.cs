using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Asymptote.Shared.World.Entity.AI.Detection;

public class DetectionManager
{
    private const float DECAY_RATE_PER_SEC = 0.01f / 0.045f;
    private const float QUICK_DETECTION_RANGE = 10f;
    private const float QUIK_DETECTION_MULTIPLIER = 3.33f;
    private const float BASE_DETECTION_TIME = 1.25f;
    private const float CURIOUS_COOLDOWN_TIME = 2f;
    private const float CURIOUS_THRESHOLD = 60f / 100f;

    // Fields and backing data
    public Npc agent { get; private set; }
    public IDetectableEntity highestTarget { get; private set; }
    public IDetectableEntity focusingTarget { get; private set; }
    public bool curious { get; private set; }

    private float curiousCooldown;

    private readonly Dictionary<IDetectableEntity, DetectionTracker> trackers = new();

    public DetectionManager(Npc agent)
    {
        this.agent = agent;
    }

    // Explicit lowercase getters matching your Luau functions
    public bool isCurious() => this.curious;
    public bool isDetecting() => this.focusingTarget != null;
    public IDetectableEntity getFocusingTarget() => this.focusingTarget;
    public IDetectableEntity getPrioritizedTarget() => this.highestTarget;

    public bool isFullyDetected(string uuid)
    {
        return trackers.Values.Any(t => t.source.getUuid() == uuid && t.value >= 1.0f);
    }

    public void processPerception(
        List<IDetectableEntity> perceived,
        Dictionary<string, DetectionContext> contexts,
        float deltaTime)
    {
        var seenThisFrame = new HashSet<IDetectableEntity>();

        float priorityFloor = trackers
            .Where(pair => pair.Value.value >= 1.0f)
            .Select(pair => pair.Key.getPriority(contexts.GetValueOrDefault(pair.Value.source.getUuid()) ?? new()))
            .DefaultIfEmpty(0f)
            .Max();

        IDetectableEntity focus = null;

        foreach (var detectable in perceived)
        {
            var context = contexts.GetValueOrDefault(detectable.getUuid()) ?? new DetectionContext();
            float dist = agent.getPosition().DistanceTo(detectable.getPosition());

            if (dist > detectable.getBaseDetectionRadius() || !detectable.shouldRaiseDetection(agent, context))
                continue;

            if (detectable.getPriority(context) < priorityFloor)
                continue;

            var tracker = getOrCreateTracker(detectable, context);
            float speed = calculateSpeed(detectable, dist, context);

            tracker.increase(speed * deltaTime);
            seenThisFrame.Add(detectable);

            focus = detectable;
        }

        focusingTarget = focus;
        bool shouldBeCurious = false;

        var deadEntities = new List<IDetectableEntity>();

        foreach (var (entity, tracker) in trackers)
        {
            var worldEntity = agent.getScene().getEntityByUuid(entity.getUuid()) as IEntity;

            if (!seenThisFrame.Contains(entity))
            {
                if (tracker.value >= 1.0f)
                {
                    continue;
                }
                if (worldEntity == null || !worldEntity.isInScene())
                {
                    deadEntities.Add(entity);
                    continue;
                }

                tracker.decay(DECAY_RATE_PER_SEC, deltaTime);

                if (tracker.value <= 0)
                    deadEntities.Add(entity);
            }

            // TODO: Logic to send detection value to clients
            /* if (worldEntity?.typeId == "player")
			{
				var player = worldEntity.getPlayer();
				// if (!entityValues.ContainsKey(player))
				//	entityValues[player] = new List<DetectionData>();
				if (tracker.value < 1.0f || (tracker.value >= 1.0f && !tracker.wasCappedLastFrame))
				{
					// For sending it to the client, used in flushToClients
					// entityValues[player].Add(new DetectionData(agent.getUuid(), agent.getWorldInstance(), tracker.value));
					tracker.wasCappedLastFrame = tracker.value >= 1.0f;
				}
			} */

            if (!shouldBeCurious && !tracker.wasCappedLastFrame && tracker.value >= CURIOUS_THRESHOLD)
                shouldBeCurious = true;

            if (tracker.value >= 1.0f)
                shouldBeCurious = true;
        }

        foreach (var dead in deadEntities) trackers.Remove(dead);

        if (shouldBeCurious)
        {
            curious = true;
            curiousCooldown = CURIOUS_COOLDOWN_TIME;
        }
        else if (curious && curiousCooldown > 0)
        {
            curiousCooldown -= deltaTime;
        }

        if (curiousCooldown <= 0)
            curious = false;

        updateHighestPriority();
    }

    private void updateHighestPriority()
    {
        float activeFloor = trackers
            .Where(p => p.Value.value >= 1.0f)
            .Select(p => p.Key.getBasePriority(p.Value.context))
            .DefaultIfEmpty(-1f)
            .Max();

        highestTarget = trackers
            .Where(p => p.Key.getBasePriority(p.Value.context) >= activeFloor)
            .OrderByDescending(p => p.Key.getBasePriority(p.Value.context) + p.Value.value)
            .Select(p => p.Key)
            .FirstOrDefault();
    }

    private DetectionTracker getOrCreateTracker(IDetectableEntity entity, DetectionContext context)
    {
        if (!trackers.TryGetValue(entity, out var tracker))
        {
            tracker = new DetectionTracker(entity, context ?? new DetectionContext());
            trackers[entity] = tracker;
        }
        else
        {
            tracker.context = context ?? new DetectionContext();
        }
        return tracker;
    }

    private float calculateSpeed(IDetectableEntity entity, float distance, DetectionContext context)
    {
        float speedMultiplier = entity.getDetectionMultiplier(context);
        if (distance <= QUICK_DETECTION_RANGE) speedMultiplier *= QUIK_DETECTION_MULTIPLIER;

        speedMultiplier *= entity.getMovementMultiplier(context);
        return 1f / BASE_DETECTION_TIME * speedMultiplier;
    }

    public static void flushToClients()
    {
        // Network shit to tell the clients an NPC's detection value for the detection meter.
    }
}