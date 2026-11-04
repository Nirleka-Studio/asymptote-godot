namespace Asymptote.Shared.Physics.Collision;

public static class CollisionMasks
{
    public const uint WORLD = 1u << 0; // layer 1
    public const uint PLAYERS = 1u << 1; // layer 2
    public const uint NPCS = 1u << 2; // layer 3
    public const uint TRIGGERS = 1u << 3; // layer 4

    public const uint BULLET = WORLD | PLAYERS | NPCS;
    public const uint LOS = WORLD;
}