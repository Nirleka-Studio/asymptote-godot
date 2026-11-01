#nullable enable

using System.Collections.Generic;
using Asymptote.Shared.World.Level.Scene;

namespace Asymptote.Shared.World.Compound;

public class CompoundManager
{
    private const double PROXIMITY_CHECK_INTERVAL = 1;

    private readonly Scene scene;
    private readonly Dictionary<string, Compound> compounds = new();
    private double sinceLastCheck;

    public CompoundManager(Scene scene)
    {
        this.scene = scene;
    }

    public void register(Compound compound)
    {
        this.compounds[compound.id] = compound;
    }

    public Compound? get(string id)
    {
        return this.compounds.GetValueOrDefault(id);
    }

    public void update(double deltaTime)
    {
        this.sinceLastCheck += deltaTime;
        if (this.sinceLastCheck >= PROXIMITY_CHECK_INTERVAL)
        {
            foreach (var compound in this.compounds.Values)
            {
                compound.updateProximity(this.scene.players, this.sinceLastCheck);
            }

            this.sinceLastCheck = 0;
        }

        foreach (var compound in this.compounds.Values)
        {
            compound.tick(this.scene);
        }
    }
}