using System;
using Asymptote.Shared.World.Entity.Player;
using Asymptote.Shared.World.Level.Scene;
using Godot;

namespace Asymptote.Server;

public partial class Server : Node
{
    private const float TICK_RATE = (float)1 / 20;
    private const int MAX_TICKS_PER_SECOND = 5;

    public Scene scene { get; }
    private double timeAccumulator = 0;
    private ulong currentTick = 0;

    [Signal]
    public delegate void LevelUpdateFinishedEventHandler();

    public Server()
    {
        this.scene = new Scene();
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

        base._PhysicsProcess(delta); // Which one should go first bruh?
    }

    private void onPlayerJoined(long id)
    {
        // TODO: Also on `onPlayerLeft`, this should be more cleaner

        GD.Print($"Adding player {id}");

        PackedScene playerScene = GD.Load<PackedScene>("res://scenes/player.tscn");
        Node character = playerScene.Instantiate();

        character.Set("player", id);
        character.Name = id.ToString();
        GetNode("Players").AddChild(character, true);

        // Add it to the actual Scene
        PlayerEntity playerEntity = new PlayerEntity((CharacterBody3D)character);
        playerEntity.uuid = id.ToString();
        playerEntity.setScene(this.scene);
        this.scene.entityManager.addEntity(playerEntity);
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