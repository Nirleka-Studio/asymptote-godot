using System.Collections.Generic;
using Asymptote.Shared.World.Inventory;
using Godot;

namespace Asymptote.Client.UI.Inventory;

// Renders one InventoryContainer as a grid of cells and lets the player drag
// placed items around within it. Local-only for now — see tryHandleDrop() for
// where the "request move" RPC plugs in once the server is the truth-holder,
// and where combine/stack-merge will get consulted before falling back to a
// plain positional move.
public partial class InventoryGridUI : Control
{
    [Export] public int cellSize = 64;
    [Export] public Color gridLineColor = new Color(1, 1, 1, 0.15f);
    [Export] public Color invalidDropColor = new Color(1, 0, 0, 0.35f);
    [Export] public Color validDropColor = new Color(0, 1, 0, 0.25f);

    private InventoryContainer container;
    private readonly Dictionary<PlacedItem, Control> itemVisuals = new();

    private PlacedItem draggingItem;
    private Vector2 dragGrabOffset; // where inside the item's rect the mouse grabbed it
    private Vector2I dragPreviewCell;
    private bool dragPreviewValid;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;
    }

    public void setContainer(InventoryContainer container)
    {
        this.container = container;
        rebuild();
    }

    private void rebuild()
    {
        foreach (var child in itemVisuals.Values)
        {
            child.QueueFree();
        }

        itemVisuals.Clear();

        if (container == null) return;

        CustomMinimumSize = new Vector2(
            container.grid.width * cellSize,
            container.grid.height * cellSize
        );

        foreach (var placed in container.grid.placedItems)
        {
            addVisualFor(placed);
        }

        QueueRedraw();
    }

    private void addVisualFor(PlacedItem placed)
    {
        var visual = buildItemVisual(placed.stack);
        AddChild(visual);
        layoutVisual(visual, placed.position, placed.getSize());
        itemVisuals[placed] = visual;
    }

    private Control buildItemVisual(ItemStack stack)
    {
        var panel = new Panel { MouseFilter = MouseFilterEnum.Ignore };

        var icon = new TextureRect
        {
            Texture = stack.definition.icon,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = MouseFilterEnum.Ignore,
            AnchorRight = 1,
            AnchorBottom = 1
        };
        panel.AddChild(icon);

        if (stack.count > 1)
        {
            var countLabel = new Label
            {
                Text = $"x{stack.count}",
                MouseFilter = MouseFilterEnum.Ignore,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                AnchorRight = 1,
                AnchorBottom = 1
            };
            panel.AddChild(countLabel);
        }

        return panel;
    }

    private void layoutVisual(Control visual, Vector2I cellPosition, Vector2I cellSpan)
    {
        visual.Position = new Vector2(cellPosition.X * cellSize, cellPosition.Y * cellSize);
        visual.Size = new Vector2(cellSpan.X * cellSize, cellSpan.Y * cellSize);
    }

    public override void _Draw()
    {
        if (container == null) return;

        var grid = container.grid;

        for (int x = 0; x <= grid.width; x++)
        {
            DrawLine(new Vector2(x * cellSize, 0), new Vector2(x * cellSize, grid.height * cellSize), gridLineColor);
        }

        for (int y = 0; y <= grid.height; y++)
        {
            DrawLine(new Vector2(0, y * cellSize), new Vector2(grid.width * cellSize, y * cellSize), gridLineColor);
        }

        if (draggingItem != null)
        {
            var size = draggingItem.getSize();
            var rect = new Rect2(
                dragPreviewCell.X * cellSize, dragPreviewCell.Y * cellSize,
                size.X * cellSize, size.Y * cellSize
            );
            DrawRect(rect, dragPreviewValid ? validDropColor : invalidDropColor);
        }
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (container == null) return;

        if (@event is InputEventMouseButton mouseButton && mouseButton.ButtonIndex == MouseButton.Left)
        {
            if (mouseButton.Pressed)
            {
                beginDrag(mouseButton.Position);
            }
            else if (draggingItem != null)
            {
                endDrag();
            }
        }
        else if (@event is InputEventMouseMotion mouseMotion && draggingItem != null)
        {
            updateDrag(mouseMotion.Position);
        }
    }

    private void beginDrag(Vector2 mousePosition)
    {
        var cell = new Vector2I((int)(mousePosition.X / cellSize), (int)(mousePosition.Y / cellSize));

        foreach (var placed in container.grid.placedItems)
        {
            var rect = new Rect2I(placed.position, placed.getSize());
            if (rect.HasPoint(cell))
            {
                draggingItem = placed;
                dragGrabOffset =
                    mousePosition - new Vector2(placed.position.X * cellSize, placed.position.Y * cellSize);

                itemVisuals[placed].Modulate = new Color(1, 1, 1, 0.5f);
                itemVisuals[placed].ZIndex = 10;

                updateDrag(mousePosition);
                return;
            }
        }
    }

    private void updateDrag(Vector2 mousePosition)
    {
        var topLeftPixel = mousePosition - dragGrabOffset;
        dragPreviewCell = new Vector2I(
            Mathf.RoundToInt(topLeftPixel.X / cellSize),
            Mathf.RoundToInt(topLeftPixel.Y / cellSize)
        );

        dragPreviewValid = container.grid.isFree(dragPreviewCell, draggingItem.getSize(), draggingItem);

        layoutVisual(itemVisuals[draggingItem], dragPreviewCell, draggingItem.getSize());

        QueueRedraw();
    }

    private void endDrag()
    {
        var item = draggingItem;
        var visual = itemVisuals[item];

        visual.Modulate = Colors.White;
        visual.ZIndex = 0;

        if (!tryHandleDrop(item, dragPreviewCell))
        {
            // rejected — snap back to wherever it's actually still recorded as being
            layoutVisual(visual, item.position, item.getSize());
        }

        draggingItem = null;
        QueueRedraw();
    }

    // The seam: local-only positional move today. Later this becomes
    // "ask ItemCombineService / stack-merge first, then send RequestDragDrop
    // to the server and reconcile" — this function's signature doesn't change.
    private bool tryHandleDrop(PlacedItem item, Vector2I targetCell)
    {
        if (!container.grid.tryMove(item, targetCell))
        {
            return false;
        }

        layoutVisual(itemVisuals[item], item.position, item.getSize());
        return true;
    }
}