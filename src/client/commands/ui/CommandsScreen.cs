using Godot;

namespace Asymptote.Client.Commands.UI;

public partial class CommandsScreen : Control
{
    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("command_open"))
        {
            GD.Print("Action pressed!");
            GetViewport().SetInputAsHandled();
        }
    }
}