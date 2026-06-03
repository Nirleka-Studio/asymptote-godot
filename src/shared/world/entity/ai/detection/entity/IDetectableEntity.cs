using Godot;

namespace Asymptote.Shared.World.Entity.AI.Detection;

public interface IDetectableEntity
{
	string getUuid();

	Godot.Vector3 getPosition();

	float getBasePriority(DetectionContext context);

	float getPriority(DetectionContext context);

	float getBaseDetectionRadius();

	float getDetectionMultiplier(DetectionContext context);

	float getMovementMultiplier(DetectionContext context);

	bool shouldRaiseDetection(Npc agent, DetectionContext context);
}