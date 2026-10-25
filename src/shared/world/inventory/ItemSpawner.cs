using Asymptote.Shared.World.Level.Scene;
using Godot;

namespace Asymptote.Shared.World.Inventory;

public partial class ItemSpawner : Node3D
{
	public override void _Ready()
	{
		if (!Multiplayer.IsServer())
		{
			return;
		}

		Server.Server server = Owner as Server.Server;
		if (server == null)
		{
			GD.PrintErr("ItemSpawner: Owner is not a Server — items won't be spawnable");
			return;
		}

		server.scene.itemSpawnRoot = this;
	}
}
