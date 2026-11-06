#nullable enable

using System;
using System.Collections.Generic;
using Asymptote.Shared.World.Entity.Player;
using Godot;

namespace Asymptote.Shared.World.Interaction;

public class InteractionManager
{
    private const float RANGE_SLACK = 1.5f; // server is more forgiving than the client (lag)

    private readonly Dictionary<string, Func<PlayerEntity, Prompt, bool>> handlers = new();

    public void register(string interactionType, Func<PlayerEntity, Prompt, bool> handler)
    {
        this.handlers[interactionType] = handler;
    }

    public bool handle(PlayerEntity player, Prompt? prompt)
    {
        if (prompt == null || !GodotObject.IsInstanceValid(prompt) || !prompt.enabled)
        {
            return false;
        }

        if (!this.handlers.TryGetValue(prompt.interactionType, out var handler))
        {
            return false;
        }

        if (!prompt.isReachableFrom(player.getEyePosition(), player.getRid(), RANGE_SLACK))
        {
            return false;
        }

        return handler(player, prompt);
    }
}