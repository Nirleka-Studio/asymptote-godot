using System.Collections.Generic;
using Asymptote.Shared.World.Interaction;
using Godot;

namespace Asymptote.Client.Interaction;

public partial class PromptRegistry : Node
{
    public HashSet<Prompt> prompts = new();

    public static PromptRegistry Instance { get; private set; }

    public override void _EnterTree()
    {
        Instance = this;
    }

    public void registerPrompt(Prompt prompt)
    {
        prompts.Add(prompt);
    }

    public void unregisterPrompt(Prompt prompt)
    {
        prompts.Remove(prompt);
    }
}