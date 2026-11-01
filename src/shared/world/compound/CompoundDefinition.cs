using System.Collections.Generic;
using Godot;

namespace Asymptote.Shared.World.Compound;

public partial class CompoundDefinition : Node3D
{
    public const string GROUP = "compound_definitions";

    [Export] public string compoundId { get; set; } = "";
    [Export] public float activateRadius { get; set; } = 150f;
    [Export] public float deactivateRadius { get; set; } = 200f; // must be > activateRadius (hysteresis)
    [Export] public float despawnDelay { get; set; } = 10f; // seconds outside before reset

    public override void _EnterTree()
    {
        AddToGroup(GROUP);
    }

    public override void _Ready()
    {
        if (!Multiplayer.IsServer())
        {
            QueueFree();
        }
    }

    public Compound build()
    {
        var spawns = new List<NpcSpawnDefinition>();
        collect(this, spawns);

        return new Compound(
            compoundId, GlobalPosition, activateRadius, deactivateRadius, despawnDelay, spawns);
    }

    private static void collect(Node node, List<NpcSpawnDefinition> into)
    {
        foreach (Node child in node.GetChildren())
        {
            if (child is NpcSpawnPoint sp)
            {
                into.Add(new NpcSpawnDefinition(
                    sp.definitionId, sp.scenePath, sp.GlobalPosition, sp.GlobalRotation));
            }

            collect(child, into);
        }
    }
}