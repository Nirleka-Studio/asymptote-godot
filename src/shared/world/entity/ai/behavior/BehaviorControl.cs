namespace Asymptote.Shared.World.Entity.AI;

public interface BehaviorControl<T> where T : IEntity
{
	public string name { get; }
	public Status getStatus();
	public bool tryStart(T agent, float currentTime, float deltaTime);
	public void updateOrStop(T agent, float currentTime, float deltaTime);
	public void stop(T agent);
}

public enum Status
{
	STOPPED,
	RUNNING
}