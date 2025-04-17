using Godot;
using System;
using System.Collections.Generic;

public partial class EdgeNode : Node3D
{
    public PointNode PointA { get; private set; }
    public PointNode PointB { get; private set; }
    
    public Vector3 GetStartPoint() => PointA?.GlobalPosition ?? Vector3.Zero;
    public Vector3 GetEndPoint() => PointB?.GlobalPosition ?? Vector3.Zero;


    public List<FaceNode> ConnectedFaces { get; private set; } = new();

    public LineBuilder Line { get; private set; } = new LineBuilder();
    private MeshInstance3D _lineMesh;

    public override void _Ready()
    {
        // Create mesh once
        _lineMesh = Line.CreateMeshInstance();
        AddChild(_lineMesh);
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
        
        foreach (var face in ConnectedFaces)
            face.UpdateFace();
    }


    public void RegisterFace(FaceNode face)
    {
        if (!ConnectedFaces.Contains(face))
            ConnectedFaces.Add(face);
    }
}