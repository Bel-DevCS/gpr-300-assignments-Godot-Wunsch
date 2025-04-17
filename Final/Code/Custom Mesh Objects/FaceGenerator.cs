using Godot;
using System;
using System.Collections.Generic;

public partial class FaceGenerator : Node
{
    private ImmediateMesh _mesh = new ImmediateMesh();

    public void GenerateFacesFromPoints(List<PointGenerator.ControlPoint> points)
    {
        _mesh.ClearSurfaces();
        _mesh.SurfaceBegin(Mesh.PrimitiveType.Triangles);

        int count = points.Count;

        if (count >= 3)
        {
            Vector3 center = Vector3.Zero;
            foreach (var pt in points)
                center += pt.Position;
            center /= count;

            for (int i = 0; i < count; i++)
            {
                Vector3 a = points[i].Position;
                Vector3 b = points[(i + 1) % count].Position;

                _mesh.SurfaceAddVertex(center);
                _mesh.SurfaceAddVertex(a);
                _mesh.SurfaceAddVertex(b);
            }
        }

        _mesh.SurfaceEnd();
    }

    public ImmediateMesh GetMesh() => _mesh;
}