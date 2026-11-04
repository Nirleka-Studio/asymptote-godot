using Asymptote.Client.Interaction;
using Godot;

namespace Asymptote.Shared.World.Interaction;

public partial class Prompt : Marker3D
{
    [Export] public int activationDistance { get; set; } = 3;
    [Export] public StringName action { get; set; } = "interact";
    [Export] public string labelKey { get; set; } = "PROMPT_INTERACT";

    public override void _EnterTree()
    {
        base._EnterTree();

        PromptRegistry.Instance.registerPrompt(this);

        GD.Print("prompt registered");
    }

    public override void _ExitTree()
    {
        base._ExitTree();
    }
}