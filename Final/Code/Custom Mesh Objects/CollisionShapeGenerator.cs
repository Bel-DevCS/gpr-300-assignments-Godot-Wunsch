using Godot;
using System;
using System.Collections.Generic;

public partial class CollisionShapeGenerator : Node
{
    private List<MeshInstance3D> _collisionShapes = new();

    public void GeneratePointCollisions(List<PointGenerator.ControlPoint> points)
    {
        foreach (var point in points)
        {
            var sphere = new MeshInstance3D
            {
                Mesh = new SphereMesh { Radius = 0.1f },
                Position = point.Position
            };

            var body = new StaticBody3D();
            var collider = new CollisionShape3D { Shape = new SphereShape3D { Radius = 0.1f } };
            body.AddChild(collider);
            body.Position = Vector3.Zero;
            sphere.AddChild(body);

            _collisionShapes.Add(sphere);
            AddChild(sphere);
        }
    }

    public void GenerateEdgeCollisions(List<LineBuilder> edges)
    {
        foreach (var line in edges)
        {
            // Implement collision logic for the edges (e.g., line-based colliders)
        }
    }

    public List<MeshInstance3D> GetCollisionShapes() => _collisionShapes;
}