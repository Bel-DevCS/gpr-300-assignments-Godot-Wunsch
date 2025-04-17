using Godot;
using System;

public partial class EdgeNode : Node3D
{
    public PointNode PointA { get; set; }
    public PointNode PointB { get; set; }

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

    public Vector3[] GetSegmentPoints()
    {
        return Line.Points.ToArray(); // Useful for face mesh generation
    }
}