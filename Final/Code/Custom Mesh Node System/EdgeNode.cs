using Godot;
using System;
using System.Collections.Generic;

public partial class EdgeNode : Node3D
{
    public PointNode PointA { get; private set; }
    public PointNode PointB { get; private set; }

    public List<FaceNode> ConnectedFaces { get; private set; } = new();

    public LineBuilder Line { get; private set; } = new LineBuilder();
    private MeshInstance3D _lineMesh;

    public override void _Ready()
    {
        _lineMesh = Line.CreateMeshInstance();
        AddChild(_lineMesh);
        UpdateEdge();
    }

    public void SetPoints(PointNode a, PointNode b)
    {
        PointA = a;
        PointB = b;
        UpdateEdge();
    }

    public void UpdateEdge()
    {
        if (PointA == null || PointB == null)
            return;

        Line.SetEndpoints(PointA.GlobalPosition, PointB.GlobalPosition);
        Line.DrawLine();

        if (_lineMesh != null && _lineMesh.IsInsideTree())
        {
            RemoveChild(_lineMesh);
            _lineMesh.QueueFree();
        }

        _lineMesh = Line.CreateMeshInstance();
        AddChild(_lineMesh);
    }

    public void RegisterFace(FaceNode face)
    {
        if (!ConnectedFaces.Contains(face))
            ConnectedFaces.Add(face);
    }
}
