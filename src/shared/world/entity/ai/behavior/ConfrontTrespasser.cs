using System.Collections.Generic;
using Asymptote.Shared.World.Entity.AI.Memory;

namespace Asymptote.Shared.World.Entity.AI.Behavior;

public class ConfrontTrespasser : Behavior<Npc>
{
    public override string name { get; } = "ConfrontTrespasser";

    public ConfrontTrespasser() : base(new Dictionary<IMemoryModuleType, MemoryStatus>
    {
        [MemoryModuleTypes.IS_CURIOUS] = MemoryStatus.REGISTERED // For demo only
    })
    {
        // Constructor body
    }

    protected override bool checkExtraConditions(Npc agent)
    {
        return true;
    }

    protected override bool canStillUse(Npc agent)
    {
        return false;
    }

    protected override void doStart(Npc agent, double currentTime, double deltaTime)
    {
    }

    protected override void doUpdate(Npc agent, double currentTime, double deltaTime)
    {
    }

    protected override void doStop(Npc agent)
    {
    }
}