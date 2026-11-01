using Godot;

namespace Asymptote.Shared.World.Compound;

/// <summary>
/// Editor-placed marker. Child of a CompoundDefinition.
/// </summary>
public partial class NpcSpawnPoint : Marker3D
{
    // Unique within its compound, e.g. "guard_01". missions refer to this
    [Export] public string definitionId { get; set; } = "";

    [Export(PropertyHint.File, "*.tscn")] public string scenePath { get; set; } = "res://scenes/npc.tscn";
}