using Asymptote.Client.UI.Inventory;
using Asymptote.Shared.World.Entity.Component;
using Asymptote.Shared.World.Entity.Component.Inventory;
using Asymptote.Shared.World.Entity.Player;
using Godot;

namespace Asymptote.Client.UI.Inventory;

// I fucking hate this
public partial class PlayerInventoryController : Node
{
    [Export] public NodePath playerEntityPath = "..";

    private PlayerEntity player;
    private InventoryScreen screen;

    public override void _Ready()
    {
        player = GetNode<PlayerEntity>(playerEntityPath);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!player.IsMultiplayerAuthority()) return;

        if (@event.IsActionPressed("inventory_toggle"))
        {
            onInventoryToggle();
        }

        if (@event.IsActionPressed("pick_up_item"))
        {
            tryPickup();
        }
    }

    private void onInventoryToggle()
    {
        // Toggle UI
        screen ??= GetNode<InventoryScreen>("/root/InventoryScene");
        screen.toggle(player.getComponent<InventoryComponent>());

        // TODO: There should be a central mouse manager for this
        if (Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
        else if (Input.MouseMode == Input.MouseModeEnum.Visible)
        {
            Input.MouseMode = Input.MouseModeEnum.Captured;
        }
    }

    private void tryPickup()
    {
        
    }
}