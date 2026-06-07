namespace Asymptote.Shared.World.Entity.AI.Behavior;

public interface BehaviorControl<T> where T : IEntity
{
    public string name { get; }
    public BehaviorStatus getStatus();
    public bool tryStart(T agent, double currentTime, double deltaTime);
    public void updateOrStop(T agent, double currentTime, double deltaTime);
    public void stop(T agent);
}

public enum BehaviorStatus
{
    STOPPED,
    RUNNING
}