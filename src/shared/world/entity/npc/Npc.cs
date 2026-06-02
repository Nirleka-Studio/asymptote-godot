using Godot;

namespace Asymptote.Shared.World.Entity;

public partial class Npc : CharacterBody3D, IEntity
{
	public string uuid { get; set; }
	public int instId { get; set; }

	public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

	public override void _Ready()
	{
		GD.Print("NPC is ready!");
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector3 velocity = Velocity;

		if (!IsOnFloor())
		{
			velocity.Y -= gravity * (float)delta;
		}
		else
		{
			velocity.Y = 0;
		}

		Velocity = velocity;

		MoveAndSlide();
	}

	public Vector3 getPosition()
	{
		return GlobalPosition;
	}
}