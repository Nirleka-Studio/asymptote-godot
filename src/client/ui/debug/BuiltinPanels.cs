using System;
using System.Runtime.InteropServices;
using Godot;

namespace Asymptote.Client.UI.Debug;

[DebugPanel("Position", DebugSide.Left, Order = 0, RefreshHz = 0)] // 0 = every frame
public sealed class PositionPanel : IDebugPanel
{
    public void Update(DebugContext ctx, PanelWriter w)
    {
        if (ctx.Target is not { } t)
        {
            w.Text("No target", DebugColors.Dim);
            return;
        }

        var p = t.GlobalPosition;
        var f = -t.GlobalTransform.Basis.Z; // Godot forward = -Z
        string facing = Mathf.Abs(f.X) > Mathf.Abs(f.Z)
            ? (f.X > 0 ? "east (+X)" : "west (-X)")
            : (f.Z > 0 ? "south (+Z)" : "north (-Z)");

        w.Line("XYZ", p)
            .Line("Block", $"{Mathf.FloorToInt(p.X)} {Mathf.FloorToInt(p.Y)} {Mathf.FloorToInt(p.Z)}")
            .Line("Facing", facing, DebugColors.Accent)
            .Line("Yaw", t.GlobalRotationDegrees.Y);
    }
}

[DebugPanel("Performance", DebugSide.Left, Order = 10, RefreshHz = 4)]
public sealed class PerformancePanel : IDebugPanel
{
    public void Update(DebugContext ctx, PanelWriter w)
    {
        double fps = Engine.GetFramesPerSecond();
        double ms = Performance.GetMonitor(Performance.Monitor.TimeProcess) * 1000.0;

        w.Line("FPS", (int)fps, DebugColors.Scale(fps, 60, 20)) // green >= 60, red <= 20
            .Line("Frame ms", ms, DebugColors.Scale(ms, 8, 33))
            .Line("Draw calls", (int)Performance.GetMonitor(Performance.Monitor.RenderTotalDrawCallsInFrame))
            .Line("Objects", (int)Performance.GetMonitor(Performance.Monitor.ObjectCount))
            .Line("Nodes", (int)Performance.GetMonitor(Performance.Monitor.ObjectNodeCount));
    }
}

[DebugPanel("Targeted Object", DebugSide.Right, Order = 5, RefreshHz = 0)]
public sealed class LookedAtPanel : IDebugPanel
{
    public void Update(DebugContext ctx, PanelWriter w)
    {
        if (ctx.LookedAt is not { } hit)
        {
            w.Text("Nothing in reach", DebugColors.Dim);
            return;
        }

        w.Line("Type", hit.Collider.GetType().Name, DebugColors.Accent)
            .Line("Name", hit.Collider is Node n ? n.Name.ToString() : "-")
            .Line("Hit", hit.Position)
            .Line("Normal", hit.Normal)
            .Line("Distance", hit.Distance, DebugColors.Scale(hit.Distance, 0, ctx.ReachDistance));
    }
}

[DebugPanel("Memory", DebugSide.Right, Order = 10, RefreshHz = 2)]
public sealed class MemoryPanel : IDebugPanel
{
    const double MiB = 1024.0 * 1024.0;

    public void Update(DebugContext ctx, PanelWriter w)
    {
        w.Line("Godot static MiB", OS.GetStaticMemoryUsage() / MiB)
            .Line(".NET heap MiB", GC.GetTotalMemory(false) / MiB)
            .Line("GC gen0/1/2", $"{GC.CollectionCount(0)}/{GC.CollectionCount(1)}/{GC.CollectionCount(2)}");
    }
}

[DebugPanel("System Specs", DebugSide.Right, Order = 20, RefreshHz = 0.2f)]
public sealed class SystemPanel : IDebugPanel
{
    public void Update(DebugContext ctx, PanelWriter w)
    {
        w.Line("Godot", Engine.GetVersionInfo()["string"].AsString())
            .Line(".NET", RuntimeInformation.FrameworkDescription)
            .Line("CPU", $"{OS.GetProcessorCount()}x {OS.GetProcessorName()}")
            .Line("GPU", RenderingServer.GetVideoAdapterName())
            .Line("API", RenderingServer.GetVideoAdapterApiVersion())
            .Line("Display", DisplayServer.ScreenGetSize().ToString())
            .Line("Window", DisplayServer.WindowGetSize().ToString());
    }
}