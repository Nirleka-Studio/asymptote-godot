using System;
using System.Collections;
using System.Collections.Generic;
using Asymptote.Shared.World.Entity.AI.Memory;
using Asymptote.Shared.World.Entity.AI.Sensing;
using Asymptote.Util;
using Godot;

namespace Asymptote.Shared.World.Entity.AI;

public class Brain<E> where E : Npc
{
    private E agent;
    private Activity defaultActivity = Activity.IDLE;
    private Dictionary<IMemoryModuleType, Optional<IExpireableValue>> memories = new();
    private Dictionary<ISensorFactory, SensorControl<E>> sensors = new();
    private HashSet<Activity> activeActivities = new();
    private Dictionary<Activity, Dictionary<IMemoryModuleType, MemoryStatus>> activityRequirements = new();
    private Dictionary<Activity, HashSet<IMemoryModuleType>> activityMemoriesToEraseWhenStopped = new();

    private readonly SortedDictionary<int, Dictionary<Activity, HashSet<BehaviorControl<E>>>>
        availableBehaviorsByPriority = new();

    private HashSet<Activity> coreActivities = new();

    public Brain(
        E agent,
        List<IMemoryModuleType> memories,
        IEnumerable<ISensorFactory> sensors
    )
    {
        this.agent = agent;

        foreach (var memoryModuleType in memories)
        {
            this.memories[memoryModuleType] = Optional<IExpireableValue>.Empty;
        }

        foreach (var sensorFactory in sensors)
        {
            this.sensors[sensorFactory] = (SensorControl<E>)sensorFactory.create();
        }

        foreach (var kvp in this.sensors)
        {
            var sensor = kvp.Value;
            foreach (var memoryModuleType in sensor.getRequiredMemories())
            {
                this.memories.Add(memoryModuleType, Optional<IExpireableValue>.Empty);
            }
        }
    }

    // Porting from Minecraft Java's Brain system to C#...
    // Do you know how much black magic I need to do here?

    #region Memory Getters

    public Optional<U> getMemory<U>(MemoryModuleType<U> memoryType) where U : class
    {
        var optional = this.memories[(IMemoryModuleType)memoryType];
        if (optional == null)
        {
            throw new Exception($"Attempt to fetch unregistered '{memoryType.name}' memory");
        }
        else
        {
            return optional.Map((expireableValue) => (U)expireableValue.getValue());
        }
    }

    public bool hasMemoryValue<U>(MemoryModuleType<U> memoryType) where U : class
    {
        return this.checkMemory(memoryType, MemoryStatus.VALUE_PRESENT);
    }

    public bool checkMemory<U>(MemoryModuleType<U> memoryType, MemoryStatus memoryStatus) where U : class
    {
        var optional = this.memories[(IMemoryModuleType)memoryType];
        if (optional == null)
        {
            return false;
        }

        return memoryStatus == MemoryStatus.REGISTERED
               || (memoryStatus == MemoryStatus.VALUE_PRESENT && optional.IsPresent())
               || (memoryStatus == MemoryStatus.VALUE_ABSENT && !optional.IsPresent());
    }

    // For FUCK'S SAKE
    public bool checkMemory(IMemoryModuleType memoryType, MemoryStatus memoryStatus)
    {
        if (!this.memories.ContainsKey(memoryType))
        {
            return false;
        }

        var optional = this.memories[memoryType];

        if (optional == null)
        {
            return false;
        }

        return memoryStatus == MemoryStatus.REGISTERED
               || (memoryStatus == MemoryStatus.VALUE_PRESENT && optional.IsPresent())
               || (memoryStatus == MemoryStatus.VALUE_ABSENT && !optional.IsPresent());
    }

    #endregion

    #region Memory Setters

    public void setMemory<U>(MemoryModuleType<U> memoryType, U value) where U : class
    {
        this.setMemoryInternal(
            memoryType,
            Optional<U>.OfNullable(value).Map(v => ExpireableValue<U>.nonExpiring(v))
        );
    }

    public void setMemoryWithExpiry<U>(MemoryModuleType<U> memoryType, U value, float ttl) where U : class
    {
        this.setMemoryInternal(
            memoryType,
            Optional<ExpireableValue<U>>.Of(new ExpireableValue<U>(value, ttl))
        );
    }

    public void eraseMemory<U>(MemoryModuleType<U> memoryType) where U : class
    {
        this.setMemoryInternal(memoryType, Optional<ExpireableValue<U>>.Empty);
    }

    // This overload doesn't ask for <U>, so you can call it from your foreach loop safely.
    // Java to C# bullshit.
    public void eraseMemory(IMemoryModuleType memoryType)
    {
        if (this.memories.ContainsKey(memoryType))
        {
            this.memories[memoryType] = Optional<IExpireableValue>.Empty;
        }
    }

    internal void setMemoryInternal<U>(MemoryModuleType<U> memoryType, Optional<ExpireableValue<U>> optional)
        where U : class
    {
        if (this.memories.ContainsKey((IMemoryModuleType)memoryType))
        {
            if (optional.IsPresent() && isEmptyContainer(optional.Value.getValue()))
            {
                this.eraseMemory(memoryType);
            }
            else
            {
                // Type black magic, map the inner ExpireableValue<U> to the IExpireableValue interface.
                this.memories[(IMemoryModuleType)memoryType] = optional.Map(val => (IExpireableValue)val);
            }
        }
    }

    private bool isEmptyContainer<U>(U value)
    {
        return value is IEnumerable && value is not string;
    }

    #endregion

    #region Activities

    public bool isActivityActive(Activity activity)
    {
        return this.activeActivities.Contains(activity);
    }

    public void setCoreActivities(IEnumerable<Activity> activities)
    {
        HashSet<Activity> coreActivitiesSet = new();

        foreach (var activity in activities)
        {
            coreActivitiesSet.Add(activity);
        }

        this.coreActivities = coreActivitiesSet;
    }

    public void setDefaultActivity(Activity activity)
    {
        this.defaultActivity = activity;
    }

    private void setActiveActivity(Activity activity)
    {
        if (!this.isActivityActive(activity))
        {
            this.eraseMemoriesForOtherActivitiesThan(activity);
            this.activeActivities.Clear();

            foreach (var subActivity in this.coreActivities)
            {
                this.activeActivities.Add(subActivity);
            }

            this.activeActivities.Add(activity);
        }
    }

    public void setActiveActivityToFirstValid(IEnumerable<Activity> activities)
    {
        foreach (var activity in activities)
        {
            if (this.activityRequirementsAreMet(activity))
            {
                this.setActiveActivity(activity);
                break;
            }
        }
    }

    private bool activityRequirementsAreMet(Activity activity)
    {
        if (this.activityRequirements[activity] == null)
        {
            return false;
        }
        else
        {
            foreach (var kvp in this.activityRequirements[activity])
            {
                var memoryType = kvp.Key;
                var memoryStatus = kvp.Value;

                if (!this.checkMemory(memoryType, memoryStatus))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private void eraseMemoriesForOtherActivitiesThan(Activity activity)
    {
        foreach (var activeActivity in this.activeActivities)
        {
            if (activeActivity == activity)
            {
                continue;
            }

            var set = this.activityMemoriesToEraseWhenStopped[activeActivity];
            if (set == null)
            {
                continue;
            }

            foreach (var memoryType in set)
            {
                this.eraseMemory(memoryType);
            }
        }
    }

    public void useDefaultActivity()
    {
        this.setActiveActivity(this.defaultActivity);
    }

    public void addActivity(
        Activity activity,
        int priority,
        IEnumerable<BehaviorControl<E>> behaviorControls
    )
    {
        this.addActivityAndRemoveMemoriesWhenStopped(
            activity,
            this.createPriorityPairs(priority, behaviorControls),
            new Dictionary<IMemoryModuleType, MemoryStatus>(),
            new HashSet<IMemoryModuleType>()
        );
    }

    public void addActivityWithConditions(
        Activity activity,
        int priority,
        IEnumerable<BehaviorControl<E>> behaviorControls,
        Dictionary<IMemoryModuleType, MemoryStatus> entryConditions
    )
    {
        this.addActivityAndRemoveMemoriesWhenStopped(
            activity,
            this.createPriorityPairs(priority, behaviorControls),
            entryConditions,
            new HashSet<IMemoryModuleType>()
        );
    }

    public void addActivityAndRemoveMemoriesWhenStopped(
        Activity activity,
        IEnumerable<(int priority, BehaviorControl<E> behavior)> behaviorPairs,
        Dictionary<IMemoryModuleType, MemoryStatus> memoryRequirementsSet,
        HashSet<IMemoryModuleType> memoriesToEraseSet
    )
    {
        this.activityRequirements[activity] = memoryRequirementsSet;

        if (memoriesToEraseSet.Count > 0)
        {
            this.activityMemoriesToEraseWhenStopped[activity] = memoriesToEraseSet;
        }

        foreach (var (priority, behaviorControl) in behaviorPairs)
        {
            if (!this.availableBehaviorsByPriority.ContainsKey(priority))
            {
                this.availableBehaviorsByPriority[priority] = new Dictionary<Activity, HashSet<BehaviorControl<E>>>();
            }

            var behaviorsByActivity = this.availableBehaviorsByPriority[priority];

            if (!behaviorsByActivity.ContainsKey(activity))
            {
                behaviorsByActivity[activity] = new HashSet<BehaviorControl<E>>();
            }

            behaviorsByActivity[activity].Add(behaviorControl);
        }
    }

    internal IEnumerable<(int priority, BehaviorControl<E> behavior)> createPriorityPairs(
        int startPriority,
        IEnumerable<BehaviorControl<E>> behaviors)
    {
        var priorityPairs = new List<(int priority, BehaviorControl<E> behavior)>();
        int currentPriority = startPriority;

        foreach (var behaviorControl in behaviors)
        {
            priorityPairs.Add((currentPriority, behaviorControl));
            currentPriority++;
        }

        return priorityPairs;
    }

    #endregion

    #region Life Cycle

    public List<BehaviorControl<E>> getRunningBehaviors()
    {
        List<BehaviorControl<E>> behaviorControlsArray = new();

        foreach (var kvp in this.availableBehaviorsByPriority)
        {
            var activity = kvp.Value;
            foreach (var kvp1 in activity)
            {
                var behaviorControls = kvp1.Value;
                foreach (var behaviorControl in behaviorControls)
                {
                    if (behaviorControl.getStatus() != Status.RUNNING)
                    {
                        continue;
                    }

                    behaviorControlsArray.Add(behaviorControl);
                }
            }
        }

        return behaviorControlsArray;
    }

    public void update(double deltaTime, double currentTime)
    {
        this.forgetExpiredMemories(deltaTime);
        this.updateSensors(deltaTime);
        this.startEachNonRunningBehavior(deltaTime, currentTime);
        this.updateEachRunningBehavior(deltaTime, currentTime);
    }

    private void updateSensors(double deltaTime)
    {
        foreach (var kvp in this.sensors)
        {
            var sensor = kvp.Value;
            sensor.update(this.agent, deltaTime);
        }
    }

    private void forgetExpiredMemories(double deltaTime)
    {
        foreach (var kvp in this.memories)
        {
            var optional = kvp.Value;

            if (!optional.IsPresent())
            {
                continue;
            }

            var expireableValue = optional.Value;

            if (expireableValue.isExpired())
            {
                var memoryType = kvp.Key;
                this.eraseMemory(memoryType);
            }

            expireableValue.update(deltaTime);
        }
    }

    private void startEachNonRunningBehavior(double deltaTime, double currentTime)
    {
        foreach (var priorityKvp in this.availableBehaviorsByPriority)
        {
            var activities = priorityKvp.Value;

            foreach (var activityKvp in activities)
            {
                var activity = activityKvp.Key;
                var behaviorControls = activityKvp.Value;

                if (!this.activeActivities.Contains(activity))
                {
                    continue;
                }

                foreach (var behaviorControl in behaviorControls)
                {
                    if (behaviorControl.getStatus() != Status.STOPPED)
                    {
                        continue;
                    }

                    behaviorControl.tryStart(this.agent, currentTime, deltaTime);
                }
            }
        }
    }

    private void updateEachRunningBehavior(double deltaTime, double currentTime)
    {
        foreach (var behaviorControl in this.getRunningBehaviors())
        {
            behaviorControl.updateOrStop(this.agent, currentTime, deltaTime);
        }
    }

    #endregion
}