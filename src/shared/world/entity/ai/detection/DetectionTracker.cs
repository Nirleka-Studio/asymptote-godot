using Godot;

namespace Asymptote.Shared.World.Entity.AI.Detection;

public class DetectionTracker
{
    public IDetectableEntity source { get; private set; }
    public float value { get; set; }
    public DetectionContext context { get; set; }
    public bool wasCappedLastFrame { get; set; }

    public float timeSinceLastSeen { get; private set; }

    public DetectionTracker(IDetectableEntity source, DetectionContext context)
    {
        this.source = source;
        this.context = context;
        this.value = 0.0f;
        this.wasCappedLastFrame = false;
        this.timeSinceLastSeen = 0.0f;
    }

    public DetectionContext getContext()
    {
        return this.context;
    }

    public void increase(float amount)
    {
        this.value = Mathf.Clamp(this.value + amount, 0.0f, 1.0f);
        this.timeSinceLastSeen = 0.0f;
    }

    public void decay(float rate, float deltaTime)
    {
        this.value = Mathf.Max(0, this.value - (rate * deltaTime));
        this.timeSinceLastSeen += deltaTime;
    }
}