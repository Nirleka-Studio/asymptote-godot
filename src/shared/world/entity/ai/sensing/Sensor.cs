using System.Collections.Generic;

namespace Asymptote.Shared.World.Entity.AI.Sensing;

public abstract class Sensor<T> : SensorControl<T> where T : Npc
{
	private static readonly int DEFAULT_SCAN_RATE = 20; // Scans per second
	private readonly int scanRate = DEFAULT_SCAN_RATE;
	private float timeAccumulator;
	public abstract List<IMemoryModuleType> getRequiredMemories();

	public void update(T agent, float deltaTime)
	{
		this.timeAccumulator += deltaTime;

		if (this.timeAccumulator >= this.scanRate) {
			this.timeAccumulator = 0;
			this.doUpdate(agent, deltaTime);
		}
	}

	public abstract void doUpdate(T agent, float deltaTime);
}