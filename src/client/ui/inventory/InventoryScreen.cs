using Asymptote.Shared.World.Entity.Component;
using Asymptote.Shared.World.Entity.Component.Inventory;
using Godot;

namespace Asymptote.Client.UI.Inventory;

public partial class InventoryScreen : CanvasLayer
{
	[Export] public NodePath rootPath = "Root";
	[Export] public NodePath mainGridPath = "Root/Backdrop/Margin/Sections/MainGrid";
	[Export] public NodePath backpackGridPath = "Root/Backdrop/Margin/Sections/BackpackGrid";

	private Control root;
	private InventoryGridUI mainGrid;
	private InventoryGridUI backpackGrid;

	public override void _Ready()
	{
		root = GetNode<Control>(rootPath);
		mainGrid = GetNode<InventoryGridUI>(mainGridPath);
		backpackGrid = GetNode<InventoryGridUI>(backpackGridPath);
	}

	public void open(InventoryComponent inventory)
	{
		mainGrid.setContainer(inventory.main);
		backpackGrid.setContainer(inventory.backpack);
		root.Visible = true;
	}

	public void close()
	{
		root.Visible = false;
	}

	public void toggle(InventoryComponent inventory)
	{
		if (root.Visible)
		{
			close();
		}
		else
		{
			open(inventory);
		}
	}
}
