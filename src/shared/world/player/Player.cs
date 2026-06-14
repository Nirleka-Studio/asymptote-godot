using Godot;

namespace Asymptote.Shared.World.Player;

public partial class Player : Node3D
{
    [Export] public long peerId { get; set; }
    [Export] public string name { get; set; }
}