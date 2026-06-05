using Asymptote.Shared.World.Level.Scene;
using Godot;

namespace Asymptote.Shared.World.Entity;

public partial class NpcSpawner : Node3D
{
    private Scene scene;

    [Export] public bool Enabled { get; set; } = true;
    private Node3D entitiesNode;

    public NpcSpawner()
    {
        this.entitiesNode = GetNode(new NodePath(".")) as Node3D;
    }

    public override async void _Ready()
    {
        if (!Multiplayer.IsServer())
        {
            return;
        }

        GD.Print($"NpcSpawner Ready, Enabled = {Enabled}");
        if (!Enabled)
        {
            GD.Print("Npc not spawned");
            return;
        }

        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        Server.Server server = Owner as Server.Server;
        if (server != null)
        {
            GD.Print("Spawning Npc...");
            this.scene = server.scene;
            this.spawn();
        }
        else
        {
            GD.PrintErr("Npc not spawned: Server is null");
        }
    }

    private void spawn()
    {
        PackedScene playerScene = GD.Load<PackedScene>("res://scenes/npc.tscn");
        Npc character = (Npc)playerScene.Instantiate();
        this.entitiesNode.AddChild(character);
        character.setScene(this.scene);
        character.GlobalPosition = GlobalPosition;
        character.GlobalRotation = GlobalRotation;
    }
}