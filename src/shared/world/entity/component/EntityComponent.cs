namespace Asymptote.Shared.World.Entity.Component;

public interface EntityComponent<T> : IEntityComponent where T : IEntity
{
    void update(T agent, double deltaTime, double currentTime)
    {
    }
}

public interface IEntityComponent
{
}