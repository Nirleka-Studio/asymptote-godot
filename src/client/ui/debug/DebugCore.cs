using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Godot;

namespace Asymptote.Client.UI.Debug;

public enum DebugSide
{
    Left,
    Right
}

/// <summary>Put on a class implementing IDebugPanel. Auto-discovered at startup.</summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class DebugPanelAttribute(string title, DebugSide side = DebugSide.Left) : Attribute
{
    public string Title { get; } = title;
    public DebugSide Side { get; } = side;
    public int Order { get; init; }

    /// <summary>Updates per second. 0 = every frame (real time). Label only re-renders when text changes.</summary>
    public float RefreshHz { get; init; } = 10f;
}

/// <summary>Put on a static property / parameterless static method. Grouped by Panel name.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
public sealed class DebugStatAttribute(string panel, string label) : Attribute
{
    public string Panel { get; } = panel;
    public string Label { get; } = label;
    public DebugSide Side { get; init; } = DebugSide.Left;
    public int Order { get; init; }
    public float RefreshHz { get; init; } = 10f;

    /// <summary>Static value color, e.g. "#55ff55". For dynamic colors return DebugText instead.</summary>
    public string? ColorHex { get; init; }
}

public interface IDebugPanel
{
    void Update(DebugContext ctx, PanelWriter w);
}

/// <summary>A value with its own (dynamic) color. Return it from a [DebugStat] member or pass to Line().</summary>
public readonly record struct DebugText(string Text, Color Color);

public readonly record struct RayHit(GodotObject Collider, Vector3 Position, Vector3 Normal, float Distance);

public sealed class DebugContext
{
    public Node3D? Target;
    public Camera3D? Camera; // falls back to the viewport's current camera
    public float ReachDistance = 6f;
    public double Delta;

    bool _rayDone;
    RayHit? _ray;
    internal void BeginFrame() => _rayDone = false;

    /// <summary>What the camera is looking at. Raycast runs at most once per frame, shared by all panels.</summary>
    public RayHit? LookedAt
    {
        get
        {
            if (!_rayDone)
            {
                _ray = Cast();
                _rayDone = true;
            }

            return _ray;
        }
    }

    RayHit? Cast()
    {
        var cam = Camera ?? Target?.GetViewport()?.GetCamera3D();
        if (cam is null || !GodotObject.IsInstanceValid(cam)) return null;

        var from = cam.GlobalPosition;
        var to = from - cam.GlobalTransform.Basis.Z * ReachDistance;
        var hit = cam.GetWorld3D().DirectSpaceState.IntersectRay(PhysicsRayQueryParameters3D.Create(from, to));
        if (hit.Count == 0) return null;

        var pos = hit["position"].AsVector3();
        return new RayHit(hit["collider"].AsGodotObject(), pos, hit["normal"].AsVector3(), from.DistanceTo(pos));
    }
}

public static class DebugColors
{
    public static readonly Color Good = new(0.33f, 1f, 0.33f);
    public static readonly Color Warn = new(1f, 1f, 0.33f);
    public static readonly Color Bad = new(1f, 0.33f, 0.33f);
    public static readonly Color Accent = new(0.33f, 1f, 1f);
    public static readonly Color Dim = new(0.7f, 0.7f, 0.7f);

    /// <summary>Green at 'good', red at 'bad', yellow between. Works in either direction (fps: 60,20 / ms: 8,33).</summary>
    public static Color Scale(double v, double good, double bad)
    {
        float t = (float)Mathf.Clamp((v - good) / (bad - good), 0.0, 1.0);
        return t < 0.5f ? Good.Lerp(Warn, t * 2f) : Warn.Lerp(Bad, (t - 0.5f) * 2f);
    }
}

/// <summary>
/// Builds a panel's text (BBCode for colors). Lines are filtered by the user's settings:
/// the label is the stable id, so keep labels constant.
/// </summary>
public sealed class PanelWriter
{
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public readonly StringBuilder Sb = new();
    internal HashSet<string>? Hidden;
    internal bool IgnoreFilter;
    internal readonly List<string> Seen = new();

    private struct LineEntry
    {
        public string? Label; // null for unlabelled Text() lines
        public string Value;
        public Color? Color;
    }

    private readonly List<LineEntry> _entries = new();

    public bool Wants(string label)
    {
        if (!Seen.Contains(label)) Seen.Add(label);
        return IgnoreFilter || Hidden is null || !Hidden.Contains(label);
    }

    public PanelWriter Line(string label, object value, Color? color = null)
    {
        if (Hidden is not null && Hidden.Contains(label) && !IgnoreFilter) return this;
        if (!Seen.Contains(label)) Seen.Add(label);

        _entries.Add(new LineEntry { Label = label, Value = value?.ToString() ?? "", Color = color });
        return this;
    }

    public PanelWriter Text(string text, Color? color = null)
    {
        _entries.Add(new LineEntry { Label = null, Value = text, Color = color });
        return this;
    }

    public void Flush()
    {
        Sb.Clear();
        if (_entries.Count == 0) return;

        // Find longest label in this panel to align all colons vertically
        int maxLen = 0;
        foreach (var e in _entries)
        {
            if (e.Label is not null && e.Label.Length > maxLen)
                maxLen = e.Label.Length;
        }

        for (int i = 0; i < _entries.Count; i++)
        {
            if (i > 0) Sb.Append('\n');
            var e = _entries[i];

            if (e.Label is not null)
            {
                // Pad left with spaces so colons align perfectly in monospaced font
                Sb.Append(e.Label.PadLeft(maxLen)).Append(": ");
            }

            if (e.Color.HasValue)
            {
                Sb.Append("[color=#").Append(e.Color.Value.ToHtml(false)).Append(']')
                    .Append(e.Value)
                    .Append("[/color]");
            }
            else
            {
                Sb.Append(e.Value);
            }
        }

        _entries.Clear();
    }
}