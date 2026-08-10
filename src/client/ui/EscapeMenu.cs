using Godot;

namespace Asymptote.Client.UI;

public partial class EscapeMenu : Node2D
{
    public override void _UnhandledInput(InputEvent @event)
    {
        base._UnhandledInput(@event);

        if (@event.IsActionPressed("ui_cancel"))
        {
            if (Input.MouseMode == Input.MouseModeEnum.Captured)
            {
                Input.MouseMode = Input.MouseModeEnum.Visible;
            }
            else
            {
                Input.MouseMode = Input.MouseModeEnum.Captured;
            }

            GetViewport().SetInputAsHandled();
        }
    }
}