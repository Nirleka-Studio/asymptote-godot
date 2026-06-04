using Asymptote.Shared.World.Entity.player;
using Asymptote.Shared.World.Level.Scene;
using Godot;

namespace Asymptote.Server;

public partial class Server : Node
{
    private const float TICK_RATE = (float)1 / 20;
    private const int MAX_TICKS_PER_SECOND = 5;

    private Scene scene = new();
    private double timeAccumulator = 0;
    private ulong currentTick = 0;

    #region Level

    private void updateLevel(double delta)
    {
        // NOTES: Maybe this will get fucked if we decide to change the tick rate,
        // pause tick, speed up time, pause time, etc.
        this.timeAccumulator += delta;

        int ticks = 0;

        while (this.timeAccumulator >= TICK_RATE && ticks < MAX_TICKS_PER_SECOND)
        {
            // Oh this would absolutely get fucked if the game keeps running for a long time.
            double currentTime = this.currentTick * TICK_RATE;
            this.scene.update(TICK_RATE, currentTime);
            ticks++;
            this.currentTick++;
        }

        if (this.timeAccumulator > TICK_RATE * MAX_TICKS_PER_SECOND)
        {
            this.timeAccumulator -= 0;
        }
    }

    #endregion

    #region Life Cycle and Players

    public override void _Ready()
    {
        if (!Multiplayer.IsServer())
        {
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
        this.updateLevel(delta);
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