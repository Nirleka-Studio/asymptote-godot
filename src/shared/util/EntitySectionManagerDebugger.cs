using Godot;
using System;
using Asymptote.Shared.World.Level.Entity;

namespace Asymptote.Util;

public partial class EntitySectionDebugRenderer : MeshInstance3D
{
    private EntitySectionManager _manager;
    private OrmMaterial3D _material;

    [Export] public Color SectionBoxColor { get; set; } = new(0, 1, 0, 0.4f); // Green wireframe
    [Export] public Color EntityMarkerColor { get; set; } = new(1, 0, 0, 1.0f); // Red dots/lines
    [Export] public bool DrawEntities { get; set; } = true;

    public void Initialize(EntitySectionManager manager)
    {
        _manager = manager;

        // Set up an unshaded material so it's always visible regardless of lighting
        _material = new OrmMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            VertexColorUseAsAlbedo = true,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            NoDepthTest = true,
            RenderPriority = 10
        };

        this.MaterialOverride = _material;
        this.Mesh = new ImmediateMesh();
    }

    public override void _Process(double delta)
    {
        if (_manager == null || Mesh is not ImmediateMesh immediateMesh) return;

        immediateMesh.ClearSurfaces();

        var activeSections = _manager.getActiveSections();
        if (activeSections.Count == 0) return;

        immediateMesh.SurfaceBegin(Mesh.PrimitiveType.Lines);

        float size = EntitySectionManager.SECTION_SIZE;

        foreach (var (sectionKey, entities) in activeSections)
        {
            // Calculate the minimum corner of the section box in world space
            Vector3 minBounds = new Vector3(sectionKey.X, sectionKey.Y, sectionKey.Z) * size;
            Vector3 maxBounds = minBounds + new Vector3(size, size, size);

            // Draw the Section Box (Wireframe Cube)
            DrawWireframeCube(immediateMesh, minBounds, maxBounds, SectionBoxColor);

            // Draw entity position markers
            if (DrawEntities)
            {
                foreach (var entity in entities)
                {
                    DrawEntityMarker(immediateMesh, entity.getPosition(), EntityMarkerColor);
                }
            }
        }

        immediateMesh.SurfaceEnd();
    }

    private void DrawWireframeCube(ImmediateMesh mesh, Vector3 min, Vector3 max, Color color)
    {
        // Bottom 4 vertices
        Vector3 b000 = new(min.X, min.Y, min.Z);
        Vector3 b100 = new(max.X, min.Y, min.Z);
        Vector3 b101 = new(max.X, min.Y, max.Z);
        Vector3 b001 = new(min.X, min.Y, max.Z);

        // Top 4 vertices
        Vector3 t000 = new(min.X, max.Y, min.Z);
        Vector3 t100 = new(max.X, max.Y, min.Z);
        Vector3 t101 = new(max.X, max.Y, max.Z);
        Vector3 t001 = new(min.X, max.Y, max.Z);

        mesh.SurfaceSetColor(color);

        // Bottom ring
        DrawLine(mesh, b000, b100);
        DrawLine(mesh, b100, b101);
        DrawLine(mesh, b101, b001);
        DrawLine(mesh, b001, b000);

        // Top ring
        DrawLine(mesh, t000, t100);
        DrawLine(mesh, t100, t101);
        DrawLine(mesh, t101, t001);
        DrawLine(mesh, t001, t000);

        // Vertical pillars
        DrawLine(mesh, b000, t000);
        DrawLine(mesh, b100, t100);
        DrawLine(mesh, b101, t101);
        DrawLine(mesh, b001, t001);
    }

    private void DrawEntityMarker(ImmediateMesh mesh, Vector3 position, Color color)
    {
        mesh.SurfaceSetColor(color);
        float markerSize = 0.3f;

        // Entity position crosshair
        DrawLine(mesh, position + Vector3.Left * markerSize, position + Vector3.Right * markerSize);
        DrawLine(mesh, position + Vector3.Up * markerSize, position + Vector3.Down * markerSize);
        DrawLine(mesh, position + Vector3.Forward * markerSize, position + Vector3.Back * markerSize);
    }

    private void DrawLine(ImmediateMesh mesh, Vector3 start, Vector3 end)
    {
        mesh.SurfaceAddVertex(start);
        mesh.SurfaceAddVertex(end);
    }
}