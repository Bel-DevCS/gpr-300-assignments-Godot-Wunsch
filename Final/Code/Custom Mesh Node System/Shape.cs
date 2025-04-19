using Godot;
using System;
using System.Collections.Generic;

public class Shape
{
    public string Name { get; set; } = "New Shape";

    public List<PointNode> Points { get; private set; } = new();
    public List<EdgeNode> Edges { get; private set; } = new();
    public List<FaceNode> Faces { get; private set; } = new();

    public Shape(string name = "New Shape")
    {
        Name = name;
    }

    public void AddPoint(PointNode point)
    {
        if (!Points.Contains(point))
        {
            Points.Add(point);
            point.ParentShape = this;
        }
    }

    public void AddEdge(EdgeNode edge)
    {
        if (!Edges.Contains(edge))
        {
            Edges.Add(edge);
            // No back-reference needed for EdgeNode unless for future logic
        }
    }

    public void AddFace(FaceNode face)
    {
        if (!Faces.Contains(face))
        {
            Faces.Add(face);
            // No back-reference needed for FaceNode unless for future logic
        }
    }

    public void ApplyTransform(Transform3D transform)
    {
        foreach (var point in Points)
        {
            var global = point.GlobalTransform;
            global = transform * global;
            point.GlobalTransform = global;
        }

        foreach (var edge in Edges)
            edge.UpdateEdge();

        foreach (var face in Faces)
            face.UpdateFace();
    }
}