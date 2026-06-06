using Asymptote.Server;
using Godot;

public partial class DebugDraw : MeshInstance3D
{
    private static DebugDraw _instance;
    private ImmediateMesh _immMesh;
    private OrmMaterial3D _material;
    private Server server;

    public override async void _Ready()
    {
        _instance = this;
        _immMesh = new ImmediateMesh();
        Mesh = _immMesh;

        _material = new OrmMaterial3D();
        _material.ShadingMode = OrmMaterial3D.ShadingModeEnum.Unshaded;
        _material.VertexColorUseAsAlbedo = true;
        MaterialOverride = _material;
    }

    private int HOWMANYFUCKINGTICKSBEFORECLEARINGTHISRETARDEDFUCKER = 0;

    public override async void _PhysicsProcess(double delta)
    {
        HOWMANYFUCKINGTICKSBEFORECLEARINGTHISRETARDEDFUCKER++;

        if (HOWMANYFUCKINGTICKSBEFORECLEARINGTHISRETARDEDFUCKER > 1)
        {
            HOWMANYFUCKINGTICKSBEFORECLEARINGTHISRETARDEDFUCKER = 0;
            _instance._immMesh.ClearSurfaces();
        }
    }

    public static void DrawLine(Vector3 start, Vector3 end, Color color)
    {
        if (_instance == null || !_instance.IsInsideTree()) return;

        _instance._immMesh.SurfaceBegin(Mesh.PrimitiveType.Lines);
        _instance._immMesh.SurfaceSetColor(color);
        _instance._immMesh.SurfaceAddVertex(start);
        _instance._immMesh.SurfaceAddVertex(end);
        _instance._immMesh.SurfaceEnd();
    }

    public static void DrawRing(Vector3 center, Vector3 normal, float radius, int segments, Color color)
    {
        if (_instance == null || !_instance.IsInsideTree()) return;

        _instance._immMesh.SurfaceBegin(Mesh.PrimitiveType.Lines);
        _instance._immMesh.SurfaceSetColor(color);

        Vector3 up = Mathf.Abs(normal.Y) < 0.99f ? Vector3.Up : Vector3.Forward;
        Vector3 right = normal.Cross(up).Normalized();
        Vector3 forward = right.Cross(normal).Normalized();

        Vector3 lastPoint = center + right * radius;

        for (int i = 1; i <= segments; i++)
        {
            float theta = (i / (float)segments) * Mathf.Tau;
            Vector3 nextPoint = center + (right * Mathf.Cos(theta) + forward * Mathf.Sin(theta)) * radius;

            _instance._immMesh.SurfaceAddVertex(lastPoint);
            _instance._immMesh.SurfaceAddVertex(nextPoint);

            lastPoint = nextPoint;
        }

        _instance._immMesh.SurfaceEnd();
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