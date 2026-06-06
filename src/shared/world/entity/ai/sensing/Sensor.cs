using System.Collections.Generic;

namespace Asymptote.Shared.World.Entity.AI.Sensing;

public abstract class Sensor<T> : SensorControl<T> where T : Npc
{
    private static readonly int DEFAULT_SCAN_RATE = 20; // Scans per second
    private readonly int scanRate = DEFAULT_SCAN_RATE;

    private readonly double scanInterval;
    private double timeAccumulator;

    protected Sensor()
    {
        this.scanInterval = 1.0 / this.scanRate;
    }

    public void update(T agent, double deltaTime)
    {
        this.timeAccumulator += deltaTime;

        if (this.timeAccumulator >= this.scanInterval)
        {
            this.timeAccumulator -= this.scanInterval;

            this.doUpdate(agent, deltaTime);
        }
    }

    public abstract void doUpdate(T agent, double deltaTime);

    public abstract List<IMemoryModuleType> getRequiredMemories();
}