using System.Collections.Generic;
using Asymptote.Shared.World.Entity.AI.Memory;
using Asymptote.Shared.World.Level.Scene;

namespace Asymptote.Shared.World.Entity.AI.Behavior;

public abstract class Behavior<E> : BehaviorControl<E> where E : Npc
{
    protected readonly Dictionary<IMemoryModuleType, MemoryStatus> REQUIRED_MEMORIES;
    public abstract string name { get; }
    private BehaviorStatus status = BehaviorStatus.STOPPED;

    protected Behavior(Dictionary<IMemoryModuleType, MemoryStatus> requiredMemories)
    {
        REQUIRED_MEMORIES = requiredMemories;
    }

    #region Top Level Methods

    public BehaviorStatus getStatus()
    {
        return this.status;
    }

    public bool tryStart(E agent, double currentTime, double deltaTime)
    {
        if (this.hasRequiredMemories(agent) && this.checkExtraConditions(agent))
        {
            this.status = BehaviorStatus.RUNNING;
            this.doStart(agent, currentTime, deltaTime);
            return true;
        }

        return false;
    }

    public void updateOrStop(E agent, double currentTime, double deltaTime)
    {
        if (this.canStillUse(agent))
        {
            this.doUpdate(agent, currentTime, deltaTime);
        }
        else
        {
            this.stop(agent);
        }
    }

    public void stop(E agent)
    {
        this.status = BehaviorStatus.STOPPED;
        this.doStop(agent);
    }

    #region Private Utility Methods

    private bool hasRequiredMemories(E agent)
    {
        foreach (var (memoryType, memoryStatus) in this.REQUIRED_MEMORIES)
        {
            if (!agent.getBrain().checkMemory(memoryType, memoryStatus))
            {
                return false;
            }
        }

        return true;
    }

    #endregion

    #endregion

    #region Behavior Specific Methods

    protected virtual bool checkExtraConditions(E agent)
    {
        return true;
    }

    protected virtual bool canStillUse(E agent)
    {
        return false;
    }

    protected virtual void doStart(E agent, double currentTime, double deltaTime)
    {
    }

    protected virtual void doUpdate(E agent, double currentTime, double deltaTime)
    {
    }

    protected virtual void doStop(E agent)
    {
    }

    #endregion
}