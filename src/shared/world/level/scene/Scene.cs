using Asymptote.Shared.World.Entity;
using Asymptote.Shared.World.Level.Entity;

namespace Asymptote.Shared.World.Level.Scene;

public class Scene
{
    public EntityTickList entityTickList { get; private set; }
    public EntitySectionManager entityManager { get; private set; }

    public Scene()
    {
        this.entityTickList = new EntityTickList();

        LevelCallbackProxy callbackProxy = new(
            onTickingStart: (entity) => this.entityTickList.add(entity),
            onTickingStop: (entity) => this.entityTickList.remove(entity)
        );

        this.entityManager = new EntitySectionManager(callbackProxy);
    }

    public IEntity getEntityByUuid(string uuid)
    {
        return this.entityManager.getEntityByUuid(uuid);
    }
}