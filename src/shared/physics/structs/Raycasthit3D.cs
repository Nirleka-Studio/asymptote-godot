using Godot;

namespace Asymptote.Shared.Physics.Structs;

public readonly record struct RaycastHit3D(
    Vector3 Position,
    Vector3 Normal,
    GodotObject Collider,
    ulong ColliderId,
    Rid Rid,
    int Shape
);

public static class PhysicsExtensions
{
    /// <summary>
    /// Performs a 3D raycast and returns a strongly-typed hit result or null.
    /// </summary>
    public static RaycastHit3D? Raycast(
        this Node3D node,
        Vector3 from,
        Vector3 to,
        uint collisionMask = uint.MaxValue,
        Godot.Collections.Array<Rid>? exclude = null,
        bool collideWithAreas = false,
        bool collideWithBodies = true)
    {
        var spaceState = node.GetWorld3D().DirectSpaceState;

        var query = PhysicsRayQueryParameters3D.Create(from, to);
        query.CollisionMask = collisionMask;
        query.CollideWithAreas = collideWithAreas;
        query.CollideWithBodies = collideWithBodies;

        if (exclude != null)
        {
            query.Exclude = exclude;
        }

        var result = spaceState.IntersectRay(query);

        if (result.Count == 0)
            return null;

        return new RaycastHit3D(
            Position: (Vector3)result["position"],
            Normal: (Vector3)result["normal"],
            Collider: (GodotObject)result["collider"],
            ColliderId: (ulong)result["collider_id"],
            Rid: (Rid)result["rid"],
            Shape: (int)result["shape"]
        );
    }

    /// <summary>
    /// Performs a forward raycast relative to the Node3D's global position and direction.
    /// </summary>
    public static RaycastHit3D? RaycastForward(
        this Node3D node,
        float distance,
        uint collisionMask = uint.MaxValue)
    {
        Vector3 origin = node.GlobalPosition;
        Vector3 target = origin + (-node.GlobalTransform.Basis.Z * distance);
        return node.Raycast(origin, target, collisionMask);
    }
}