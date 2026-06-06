using System;
using System.Collections.Generic;
using Asymptote.Shared.World.Entity.AI;
using Asymptote.Shared.World.Entity.AI.Memory;
using Asymptote.Shared.World.Entity.AI.Sensing;
using Asymptote.Shared.World.Level.Scene;
using Asymptote.Util;
using Godot;

namespace Asymptote.Shared.World.Entity;

public partial class Npc : CharacterBody3D, IEntity
{
    public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
    private Scene scene { get; set; }

    public Vector3 position { get; set; }
    public string uuid { get; set; }
    public int instId { get; set; }
    public event Action<IEntity> onPositionChanged;
    public event Action<IEntity> onRemovedFromWorld;

    private Node3D eyeNode;

    private Brain<Npc> brain;

    public Npc()
    {
        this.brain = new Brain<Npc>(this, new List<IMemoryModuleType>(), new List<ISensorFactory>()
        {
            new SensorFactory<Npc>(() => new VisibleEntitiesSensor())
        });
    }

    #region Navigation

    private NavigationAgent3D navAgent;
    private RandomNumberGenerator randomNumberGenerator = new();
    private bool hasValidTarget;

    [Export] public float MovementSpeed { get; set; } = 4.0f;
    [Export] public float WanderRadius { get; set; } = 15.0f;
    [Export] public float TargetThreshold { get; set; } = 1.0f;

    #endregion

    public string getUuid()
    {
        return uuid;
    }

    public Vector3 getPosition()
    {
        return GlobalPosition;
    }

    public void setScene(Scene scene)
    {
        this.scene = scene;
    }

    public bool isInScene()
    {
        return scene != null;
    }

    public Brain<Npc> getBrain()
    {
        return this.brain;
    }

    public override void _Ready()
    {
        this.eyeNode = GetNode<Node3D>("EyeVector");

        if (!Multiplayer.IsServer())
        {
            return;
        }

        navAgent = new NavigationAgent3D();
        AddChild(navAgent);

        // How close the agent should be before stopping
        navAgent.TargetDesiredDistance = TargetThreshold;
        navAgent.TargetReached += onTargetReached;
    }

    // TODO: Should replace _PhysicsProcess
    public void update(double deltaTime, double currentTime)
    {
        this.brain.update(deltaTime, currentTime);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!Multiplayer.IsServer())
        {
            return;
        }

        DebugDrawUtils.DebugDrawNpcEye(this);
        var mapRid = navAgent.GetNavigationMap();

        // Wait for the map to be valid before causing a major fuckup
        if (!mapRid.IsValid || NavigationServer3D.MapGetIterationId(mapRid) == 0) return;

        if (!hasValidTarget)
        {
            targetRandomPosition();
            return;
        }

        var velocity = Velocity;

        // Isaac Newton would be mindblown
        if (!IsOnFloor())
            velocity.Y -= gravity * (float)delta;
        else
            velocity.Y = 0;

        // Only try to move if the agent has a path and also didn't reach it yet
        if (!navAgent.IsTargetReached())
        {
            var currentPosition = GlobalPosition;
            var nextPathPosition = navAgent.GetNextPathPosition();

            var direction = nextPathPosition - currentPosition;
            direction.Y = 0; // Keep movement on a flat plane

            // Prevent zero-vector normalization bullshit
            if (direction.LengthSquared() > 0.001f)
            {
                direction = direction.Normalized();

                velocity.X = direction.X * MovementSpeed;
                velocity.Z = direction.Z * MovementSpeed;

                LookAt(currentPosition + direction, Vector3.Up);
            }
        }
        else
        {
            velocity.X = 0;
            velocity.Z = 0;
        }

        Velocity = velocity;
        MoveAndSlide();

        if (!Position.IsEqualApprox(position))
        {
            position = Position;
            onPositionChanged?.Invoke(this);
        }
    }

    private void targetRandomPosition()
    {
        var mapRid = navAgent.GetNavigationMap();

        var randomX = randomNumberGenerator.RandfRange(-WanderRadius, WanderRadius);
        var randomZ = randomNumberGenerator.RandfRange(-WanderRadius, WanderRadius);
        var randomTarget = GlobalPosition + new Vector3(randomX, 0, randomZ);

        // Holy shit we have more control over our navmesh
        // Fuck you Roblox PathfindingService
        var closestValidPoint = NavigationServer3D.MapGetClosestPoint(mapRid, randomTarget);

        // If the map returns (0,0,0) and our NPC isn't actually AT (0,0,0) then retry next frame
        if (closestValidPoint.IsEqualApprox(Vector3.Zero) && !GlobalPosition.IsEqualApprox(Vector3.Zero))
        {
            hasValidTarget = false;
            return;
        }

        // Prevents immediate re-triggering if we're still too close somehow
        if (GlobalPosition.DistanceTo(closestValidPoint) < TargetThreshold * 2.0f)
        {
            hasValidTarget = false;
            return;
        }

        navAgent.TargetPosition = closestValidPoint;
        hasValidTarget = true;
        GD.Print($"Heading to: {closestValidPoint}");
    }

    private void onTargetReached()
    {
        GD.Print("Target reached! Finding next destination...");
        hasValidTarget = false;
    }

    public Scene getScene()
    {
        return scene;
    }

    public Vector3 getEyeVector()
    {
        return -eyeNode.GlobalTransform.Basis.Z;
    }

    public Vector3 getEyePosition()
    {
        return eyeNode.GlobalPosition;
    }

    public Rid getRid()
    {
        return GetRid();
    }
}