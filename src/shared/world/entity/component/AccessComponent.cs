using System.Collections.Generic;
using Asymptote.Shared.World.Entity.AI.Detection.Object;
using Asymptote.Shared.World.Entity.Player;
using Asymptote.Shared.World.Level.Zone;
using Godot;

namespace Asymptote.Shared.World.Entity.Component;

public class AccessComponent : EntityComponent<PlayerEntity>
{
    public PlayerEntity entity { get; }
    public HashSet<CrimeType> activeCrimes = new();

    public void onEnterZone(Zone zone)
    {
        if (!zone.isAuthorized(this.entity))
        {
            activeCrimes.Add(CrimeType.TRESPASSING);
            GD.Print($"{this.entity.Name} Is trespassing!");
        }
    }

    public void onLeaveZone(Zone zone)
    {
        // What the fuck?
        if (!zone.isAuthorized(this.entity))
        {
            activeCrimes.Remove(CrimeType.TRESPASSING);
            GD.Print($"{this.entity.Name} Is not trespassing!");
        }
    }
}