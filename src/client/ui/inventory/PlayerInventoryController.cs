using Asymptote.Shared.Physics.Structs;
using Asymptote.Shared.World.Entity.Component.Inventory;
using Asymptote.Shared.World.Entity.Player;
using Asymptote.Shared.World.Inventory;
using Godot;

namespace Asymptote.Client.UI.Inventory;

// I fucking hate this
public partial class PlayerInventoryController : Node3D
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
        if (!IsMultiplayerAuthority()) return;

        Camera3D cam = GetViewport().GetCamera3D();
        if (cam == null) return;

        Vector2 mousePos = GetViewport().GetMousePosition();
        Vector3 from = cam.ProjectRayOrigin(mousePos);

        const float maxReach = 5.0f; // TODO: This should be somewhere on the server
        Vector3 to = from + cam.ProjectRayNormal(mousePos) * maxReach;

        if (this.Raycast(from, to) is { } hit)
        {
            if (hit.Collider is WorldItem worldItem)
            {
                GD.Print("Got item");
                // Send the pickup request to the Server (Peer ID 1)
                worldItem.RpcId(1, WorldItem.MethodName.RequestPickup);
            }
        }
    }
}