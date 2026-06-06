using System.Collections.Generic;

namespace Asymptote.Shared.World.Entity.AI.Sensing;

public interface SensorControl<T> : ISensorControl
{
    List<IMemoryModuleType> getRequiredMemories();
    void update(T agent, double deltaTime);
}

public interface ISensorControl
{
}