using System;
using Asymptote.Shared.World.Level.Scene;
using Godot;

namespace Asymptote.Shared.World.Entity;

public partial class Npc : CharacterBody3D, IEntity
{
	public string uuid { get; set; }
	public int instId { get; set; }
	private Scene scene { get; set; }
	public event Action<IEntity> onPositionChanged;
	public event Action<IEntity> onRemovedFromWorld;

	public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

	public Godot.Vector3 position { get; set; }

	public string getUuid()
	{
		return this.uuid;
	}

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

		if (!Position.IsEqualApprox(this.position))
		{
			this.position = Position;
			this.onPositionChanged?.Invoke(this);
		}
	}

	public Vector3 getPosition()
	{
		return GlobalPosition;
	}

	public Scene getScene()
	{
		return this.scene;
	}

	public void setScene(Scene scene)
	{
		this.scene = scene;
	}

	public bool isInScene()
	{
		return this.scene != null;
	}
}