using System.Collections.Generic;
using Asymptote.Server;
using Godot;

public partial class DebugDraw : MeshInstance3D
{
    private static DebugDraw _instance;
    private ImmediateMesh _immMesh;
    private OrmMaterial3D _material;

    private static readonly List<(Vector3 Start, Vector3 End, Color Color)> _lineBuffer = new();

    public override void _Ready()
    {
        _instance = this;
        _immMesh = new ImmediateMesh();
        Mesh = _immMesh;

        _material = new OrmMaterial3D
        {
            ShadingMode = OrmMaterial3D.ShadingModeEnum.Unshaded,
            VertexColorUseAsAlbedo = true
        };
        MaterialOverride = _material;
    }

    public override void _PhysicsProcess(double delta)
    {
        RenderBufferedLines();
    }

    private void RenderBufferedLines()
    {
        _immMesh.ClearSurfaces();

        if (_lineBuffer.Count == 0) return;

        _immMesh.SurfaceBegin(Mesh.PrimitiveType.Lines);

        foreach (var line in _lineBuffer)
        {
            _immMesh.SurfaceSetColor(line.Color);
            _immMesh.SurfaceAddVertex(line.Start);
            _immMesh.SurfaceAddVertex(line.End);
        }

        _immMesh.SurfaceEnd();

        _lineBuffer.Clear();
    }

    public static void DrawLine(Vector3 start, Vector3 end, Color color)
    {
        if (_instance == null || !_instance.IsInsideTree()) return;
        _lineBuffer.Add((start, end, color));
    }

    public static void DrawRing(Vector3 center, Vector3 normal, float radius, int segments, Color color)
    {
        if (_instance == null || !_instance.IsInsideTree()) return;

        Vector3 up = Mathf.Abs(normal.Y) < 0.99f ? Vector3.Up : Vector3.Forward;
        Vector3 right = normal.Cross(up).Normalized();
        Vector3 forward = right.Cross(normal).Normalized();

        Vector3 lastPoint = center + right * radius;

        for (int i = 1; i <= segments; i++)
        {
            float theta = (i / (float)segments) * Mathf.Tau;
            Vector3 nextPoint = center + (right * Mathf.Cos(theta) + forward * Mathf.Sin(theta)) * radius;

            DrawLine(lastPoint, nextPoint, color);
            lastPoint = nextPoint;
        }
    }

    public static void DrawArrow(Vector3 start, Vector3 end, float arrowHeadSize, Color color)
    {
        if (_instance == null || !_instance.IsInsideTree()) return;

        DrawLine(start, end, color);

        Vector3 dir = (end - start).Normalized();
        Vector3 up = Mathf.Abs(dir.Y) < 0.99f ? Vector3.Up : Vector3.Forward;
        Vector3 right = dir.Cross(up).Normalized();
        Vector3 top = dir.Cross(right).Normalized();

        Vector3 arrowBase = end - dir * arrowHeadSize;

        DrawLine(end, arrowBase + right * (arrowHeadSize * 0.5f), color);
        DrawLine(end, arrowBase - right * (arrowHeadSize * 0.5f), color);
        DrawLine(end, arrowBase + top * (arrowHeadSize * 0.5f), color);
        DrawLine(end, arrowBase - top * (arrowHeadSize * 0.5f), color);
    }
}