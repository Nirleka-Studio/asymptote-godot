using System;
using Asymptote.Shared.World.Entity.Player;
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

    [Signal]
    public delegate void LevelUpdateFinishedEventHandler();

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
            EmitSignal(SignalName.LevelUpdateFinished);

            this.currentTick++;
            this.timeAccumulator -= TICK_RATE;
        }
    }

    #endregion

    #region Life Cycle and Players

    public override void _Ready()
    {
        if (!Multiplayer.IsServer())
        {
            SetPhysicsProcess(false);
            SetProcess(false);
            return;
        }

        Multiplayer.PeerConnected += onPlayerJoined;
        Multiplayer.PeerDisconnected += onPlayerLeft;

        foreach (var id in Multiplayer.GetPeers())
        {
            this.onPlayerJoined(id);
        }

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
        // TODO: Also on `onPlayerLeft`, this should be more cleaner

        GD.Print($"Adding player {id}");

        // To decouple the player CONNECTION instance and the player CHARACTER (entity),
        // `player_instance` will represent the connection and `player` is the actual character entity which exists
        // in the world.
        PackedScene playerScene = GD.Load<PackedScene>("res://scenes/player_instance.tscn");
        var player = (Player)playerScene.Instantiate();

        PackedScene characterScene = GD.Load<PackedScene>("res://scenes/player.tscn");
        var character = (PlayerEntity)characterScene.Instantiate();

        player.peerId = id;
        player.name = id.ToString(); // for now
        character.Name = id.ToString(); // to get the peer_id
        GetNode("Players").AddChild(character, true);

        // Add it to the actual Scene
        character.uuid = id.ToString();
        character.setScene(this.scene);
        this.scene.entityManager.addEntity(character);
    }

    private void onPlayerLeft(long id)
    {
        GD.Print($"Removing player {id}");

        if (!GetNode("Players").HasNode(id.ToString())) return;

        GetNode("Players").GetNode(id.ToString()).QueueFree();
        // EntityPlayer itself already handles deletion from the entityManager as
        // it listens to the removal event.
    }

    #endregion
}