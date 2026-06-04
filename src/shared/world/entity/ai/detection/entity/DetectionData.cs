using Godot;

namespace Asymptote.Shared.World.Entity.AI.Detection;

public struct DetectionData
{
    public string uuid { get; set; }
    public Node3D character { get; set; } // Since Roblox has models, not sure what we do here...
    public float detectionValue { get; set; }

    public DetectionData(string uuid, Node3D character, float detectionValue)
    {
        this.uuid = uuid;
        this.character = character;
        this.detectionValue = detectionValue;
    }
}