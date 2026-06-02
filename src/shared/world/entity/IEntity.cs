using Godot;

namespace Asymptote.Shared.World.Entity;

public interface IEntity
{
	string uuid { get; }
	int instId { get; }

	Godot.Vector3 getPosition();
}