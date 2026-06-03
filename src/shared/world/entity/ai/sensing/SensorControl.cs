using System.Collections.Generic;

namespace Asymptote.Shared.World.Entity.AI.Sensing;

public interface SensorControl<T>
{
	List<IMemoryModuleType> getRequiredMemories();
	void update(T agent, float deltaTime);
}