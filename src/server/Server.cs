#nullable enable

using System;
using Asymptote.Shared.World.Entity.Player;
using Asymptote.Shared.World.Level.Map;
using Asymptote.Shared.World.Level.Scene;
using Asymptote.Shared.World.Player;
using Asymptote.Util;
using Godot;

namespace Asymptote.Server;

public partial class Server : Node
{
    private const float TICK_RATE = (float)1 / 20;
    private const int MAX_TICKS_PER_SECOND = 5;

    public Scene scene { get; }
    private double timeAccumulator = 0;
    private ulong currentTick = 0;

    private EntitySectionDebugRenderer sectionsDebugger;

    private Node playersNode;

    public Server()
    {
        this.scene = new Scene();

        if (OS.IsDebugBuild())
        {
            this.sectionsDebugger = new EntitySectionDebugRenderer();
            AddChild(sectionsDebugger);

            sectionsDebugger.Initialize(this.scene.entityManager);
        }
    }

    #region Level

    private void updateLevel(double delta)
    {
        this.timeAccumulator += delta;

        double maxAllowedTime = TICK_RATE * MAX_TICKS_PER_SECOND;
        if (this.timeAccumulator > maxAllowedTime)
        {
            this.timeAccumulator = maxAllowedTime + (this.timeAccumulator % TICK_RATE);
        }

        while (this.timeAccumulator >= TICK_RATE)
        {
            double currentTime = this.currentTick * TICK_RATE;
            this.scene.update(TICK_RATE, currentTime);

            this.currentTick++;
            this.timeAccumulator -= TICK_RATE;
        }
    }

    #endregion

    #region Life Cycle and Players

    private Node3D? getDefaultSpawnNode()
    {
        var map = GetNode<Node3D>("Map").GetChildOrNull<Map>(0);
        if (map == null)
        {
            return null;
        }

        return map.defaultSpawnNode;
    }

    public override void _Ready()
    {
        if (!Multiplayer.IsServer())
        {
            SetPhysicsProcess(false);
            SetProcess(false);
            return;
        }

        this.playersNode = GetNode("Players");
        this.scene.entitiesNode = GetNode<Node3D>("Entities");
        this.scene.defaultSpawnNode = this.getDefaultSpawnNode();

        Multiplayer.PeerConnected += onPlayerJoined;
        Multiplayer.PeerDisconnected += onPlayerLeft;

        foreach (var id in Multiplayer.GetPeers())
        {
            this.onPlayerJoined(id);
        }

        // If this is run on a server, this check prevents spawning the host player.
        if (!OS.HasFeature("dedicated_server"))
        {
            this.onPlayerJoined(1); // Won't this certainly lead to persistence error?
        }
    }

    public override void _ExitTree()
    {
        if (Multiplayer.IsServer())
        {
            Multiplayer.PeerConnected -= onPlayerJoined;
            Multiplayer.PeerDisconnected -= onPlayerLeft;
        }

        base._ExitTree();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Multiplayer.IsServer())
        {
            this.updateLevel(delta);
        }

        base._PhysicsProcess(delta);
    }

    private void onPlayerJoined(long id)
    {
        string strId = id.ToString();

        if (this.playersNode.HasNode(strId))
        {
            GD.PushWarning($"Player {id} is already spawned. Skipping.");
            return;
        }

        GD.Print($"Adding player {id}");

        PackedScene characterScene = GD.Load<PackedScene>("res://scenes/player.tscn");
        var character = (PlayerEntity)characterScene.Instantiate();

        character.Name = strId;

        // Using .GlobalPosition causes
        // Condition "!is_inside_tree()" is true. Returning: Transform3D()
        // For some fucking reason.
        character.Position = this.scene.getPlayerSpawnPosition();
        this.playersNode.AddChild(character, true);

        // Add it to the actual Scene
        character.uuid = strId;
        character.setScene(this.scene);
        this.scene.entityManager.addEntity(character);
    }

    private void onPlayerLeft(long id)
    {
        GD.Print($"Removing player {id}");

        Node playerNode = playersNode.GetNodeOrNull(id.ToString());

        if (playerNode != null)
        {
            playerNode.QueueFree();
        }
        // EntityPlayer itself already handles deletion from the entityManager as
        // it listens to the removal event.
    }

    #endregion
}